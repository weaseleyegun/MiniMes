"""신규 품목 Cycle Time 예측 모델.

품목 사양(재질, 직경, 절삭 길이, 날 수)으로 표준 Cycle Time을 예측한다.

- 학습 데이터는 "표준 CT가 계산된 품목"뿐이라 수십 건 이하로 작다.
  표본이 작을 때는 복잡한 모델보다 규제가 있는 선형 모델(Ridge)이 안정적이다.
- CT는 재질 계수가 곱해지는 구조라 로그를 취하면 선형에 가까워진다. 로그 CT를 예측한다.
- 성능은 Leave-One-Out 교차검증(품목 하나를 빼고 학습해 그 품목을 예측)으로 측정한다.
"""
from __future__ import annotations

import numpy as np
import pandas as pd
from sklearn.compose import ColumnTransformer
from sklearn.linear_model import Ridge
from sklearn.model_selection import LeaveOneOut, cross_val_predict
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import OneHotEncoder, StandardScaler

NUMERIC = ["diameter", "cut_length", "flute_count"]
CATEGORICAL = ["material"]
FEATURES = NUMERIC + CATEGORICAL
ALPHAS = [0.01, 0.03, 0.1, 0.3, 1.0, 3.0, 10.0]
MIN_SAMPLES = 6


class NotTrainedError(RuntimeError):
    """모델이 아직 학습되지 않았다."""


def _build_pipeline(alpha: float) -> Pipeline:
    preprocess = ColumnTransformer(
        [
            ("num", StandardScaler(), NUMERIC),
            ("cat", OneHotEncoder(handle_unknown="ignore"), CATEGORICAL),
        ]
    )
    return Pipeline([("pre", preprocess), ("reg", Ridge(alpha=alpha))])


class CtModel:
    def __init__(self) -> None:
        self._pipeline: Pipeline | None = None
        self._materials: set[str] = set()
        self._ranges: dict[str, tuple[float, float]] = {}
        self.info: dict | None = None

    @property
    def trained(self) -> bool:
        return self._pipeline is not None

    def train(self, items: list[dict]) -> dict:
        """items: [{item_code, material, diameter, cut_length, flute_count, standard_ct}, ...]"""
        df = pd.DataFrame(items)
        if len(df) < MIN_SAMPLES:
            raise ValueError(
                f"학습에 필요한 품목이 부족합니다 ({len(df)}개, 최소 {MIN_SAMPLES}개). "
                "먼저 표준 CT를 재계산하세요."
            )

        x = df[FEATURES]
        y = np.log(df["standard_ct"].to_numpy(dtype=float))
        actual = df["standard_ct"].to_numpy(dtype=float)

        # alpha(규제 강도)를 LOO 오차가 가장 작은 값으로 고른다.
        best_alpha, best_mape, best_pred = ALPHAS[0], float("inf"), None
        for alpha in ALPHAS:
            pred = np.exp(cross_val_predict(_build_pipeline(alpha), x, y, cv=LeaveOneOut()))
            mape = float(np.mean(np.abs(pred - actual) / actual) * 100)
            if mape < best_mape:
                best_alpha, best_mape, best_pred = alpha, mape, pred

        self._pipeline = _build_pipeline(best_alpha).fit(x, y)
        self._materials = set(df["material"])
        self._ranges = {col: (float(df[col].min()), float(df[col].max())) for col in NUMERIC}

        rows = [
            {
                "item_code": r.item_code,
                "standard_ct": round(float(r.standard_ct), 1),
                "predicted_ct": round(float(p), 1),
                "error_pct": round(float((p - r.standard_ct) / r.standard_ct * 100), 2),
            }
            for r, p in zip(df.itertuples(), best_pred)
        ]
        self.info = {
            "n_samples": int(len(df)),
            "alpha": best_alpha,
            "loo_mape_pct": round(best_mape, 2),
            "materials": sorted(self._materials),
            "rows": rows,
        }
        return self.info

    def predict(self, material: str, diameter: float, cut_length: float, flute_count: int) -> dict:
        if self._pipeline is None or self.info is None:
            raise NotTrainedError("모델이 학습되지 않았습니다. 먼저 학습(/train)을 실행하세요.")
        if material not in self._materials:
            raise ValueError(
                f"학습 데이터에 없는 재질입니다: {material} (사용 가능: {', '.join(sorted(self._materials))})"
            )

        row = pd.DataFrame(
            [{"diameter": diameter, "cut_length": cut_length, "flute_count": flute_count, "material": material}]
        )
        ct = float(np.exp(self._pipeline.predict(row[FEATURES])[0]))

        # 학습 범위 밖이면 외삽이라 오차가 커질 수 있다.
        given = {"diameter": diameter, "cut_length": cut_length, "flute_count": flute_count}
        outside = [c for c, v in given.items() if not self._ranges[c][0] <= v <= self._ranges[c][1]]
        warning = None
        if outside:
            warning = "학습 범위를 벗어난 값이 있어 오차가 커질 수 있습니다: " + ", ".join(outside)

        err = self.info["loo_mape_pct"]
        return {
            "predicted_ct_sec": round(ct, 1),
            "expected_error_pct": err,
            "low_ct_sec": round(ct * (1 - err / 100), 1),
            "high_ct_sec": round(ct * (1 + err / 100), 1),
            "warning": warning,
        }
