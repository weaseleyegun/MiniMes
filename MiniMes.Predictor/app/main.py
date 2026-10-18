"""MiniMes Cycle Time 예측 서버 (FastAPI).

.NET MES API가 이 서버를 호출한다. 학습 데이터는 MES API의 /api/items에서 가져온다.
실행: uvicorn app.main:app --port 8000
"""
from __future__ import annotations

import logging
import os
from contextlib import asynccontextmanager

import httpx
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel

from .model import CtModel, NotTrainedError

MES_API_URL = os.getenv("MES_API_URL", "http://localhost:5168")
log = logging.getLogger("predictor")
model = CtModel()


class CamelModel(BaseModel):
    """JSON은 camelCase(.NET과 동일), 파이썬 코드는 snake_case로 쓴다."""

    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class PredictRequest(CamelModel):
    material: str
    diameter: float = Field(gt=0, le=500)
    cut_length: float = Field(gt=0, le=1000)
    flute_count: int = Field(ge=1, le=12)


class PredictResponse(CamelModel):
    predicted_ct_sec: float
    expected_error_pct: float
    low_ct_sec: float
    high_ct_sec: float
    warning: str | None = None


class LooRow(CamelModel):
    item_code: str
    standard_ct: float
    predicted_ct: float
    error_pct: float


class ModelInfo(CamelModel):
    n_samples: int
    alpha: float
    loo_mape_pct: float
    materials: list[str]
    rows: list[LooRow]


def fetch_training_items() -> list[dict]:
    """MES API에서 표준 CT가 계산된 품목만 가져온다."""
    response = httpx.get(f"{MES_API_URL}/api/items", timeout=10)
    response.raise_for_status()
    return [
        {
            "item_code": i["itemCode"],
            "material": i["material"],
            "diameter": i["diameter"],
            "cut_length": i["cutLength"],
            "flute_count": i["fluteCount"],
            "standard_ct": i["standardCycleTimeSec"],
        }
        for i in response.json()
        if i.get("standardCycleTimeSec")
    ]


def train_from_api() -> dict:
    try:
        items = fetch_training_items()
    except httpx.HTTPError as ex:
        raise HTTPException(502, f"MES API({MES_API_URL})에 연결할 수 없습니다: {ex}") from ex

    try:
        return model.train(items)
    except ValueError as ex:
        raise HTTPException(409, str(ex)) from ex


@asynccontextmanager
async def lifespan(_: FastAPI):
    # 시작할 때 학습을 시도한다. MES API가 꺼져 있거나 표준 CT가 없어도 서버는 뜬다.
    try:
        info = train_from_api()
        log.info("시작 시 학습 완료: %s개 품목, LOO 오차 %s%%", info["n_samples"], info["loo_mape_pct"])
    except HTTPException as ex:
        log.warning("시작 시 학습 건너뜀: %s", ex.detail)
    yield


app = FastAPI(title="MiniMes Predictor", lifespan=lifespan)


@app.get("/health")
def health() -> dict:
    return {"status": "ok", "trained": model.trained}


@app.post("/train", response_model=ModelInfo)
def train() -> dict:
    """MES API의 품목 데이터로 모델을 다시 학습한다."""
    return train_from_api()


@app.get("/model", response_model=ModelInfo)
def get_model() -> dict:
    if model.info is None:
        raise HTTPException(409, "모델이 학습되지 않았습니다. 표준 CT를 재계산한 뒤 학습하세요.")
    return model.info


@app.post("/predict", response_model=PredictResponse)
def predict(req: PredictRequest) -> dict:
    try:
        return model.predict(req.material, req.diameter, req.cut_length, req.flute_count)
    except NotTrainedError as ex:
        raise HTTPException(409, str(ex)) from ex
    except ValueError as ex:
        raise HTTPException(422, str(ex)) from ex
