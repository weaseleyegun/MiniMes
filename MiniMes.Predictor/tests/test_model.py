"""예측 모델 검증.

수집기(CycleGenerator)와 같은 이론식으로 가상 품목을 만들고,
학습에 쓰지 않은 신규 품목을 얼마나 잘 맞히는지 확인한다.
"""
import numpy as np
import pytest

from app.model import CtModel, NotTrainedError

MATERIAL_FACTOR = {"Steel": 1.0, "Stainless": 1.25, "Aluminum": 0.8, "Carbide": 1.4}


def true_ct(material: str, d: float, length: float, flutes: int) -> float:
    return (40 + 4.0 * d + 0.6 * length + 5.0 * flutes) * MATERIAL_FACTOR[material]


# (코드, 재질, 직경, 절삭길이, 날수) — API의 시드 품목과 같다.
TRAIN_SPECS = [
    ("EM-S-06", "Steel", 6, 15, 4), ("EM-S-10", "Steel", 10, 22, 4),
    ("EM-S-12", "Steel", 12, 26, 4), ("EM-S-16", "Steel", 16, 32, 4),
    ("EM-SS-08", "Stainless", 8, 19, 3), ("EM-SS-12", "Stainless", 12, 26, 3),
    ("EM-AL-10", "Aluminum", 10, 25, 2), ("EM-AL-16", "Aluminum", 16, 38, 2),
    ("EM-AL-20", "Aluminum", 20, 45, 3), ("EM-CB-06", "Carbide", 6, 13, 4),
    ("EM-CB-10", "Carbide", 10, 22, 6), ("EM-CB-12", "Carbide", 12, 26, 6),
]
NEW_SPECS = [("EM-S-14", "Steel", 14, 28, 4), ("EM-AL-12", "Aluminum", 12, 30, 2)]


def make_items(seed: int = 0) -> list[dict]:
    rng = np.random.default_rng(seed)
    return [
        {
            "item_code": code, "material": mat, "diameter": d, "cut_length": length, "flute_count": f,
            # 표준 CT는 이상치를 제거한 중앙값이라 오차가 작다(약 0.3%).
            "standard_ct": true_ct(mat, d, length, f) * (1 + rng.normal(0, 0.003)),
        }
        for code, mat, d, length, f in TRAIN_SPECS
    ]


@pytest.fixture(scope="module")
def trained() -> CtModel:
    model = CtModel()
    model.train(make_items())
    return model


def test_leave_one_out_error_is_small(trained: CtModel):
    assert trained.info["loo_mape_pct"] < 3.0


@pytest.mark.parametrize("code,mat,d,length,flutes", NEW_SPECS)
def test_new_items_are_predicted_within_5_percent(trained: CtModel, code, mat, d, length, flutes):
    pred = trained.predict(mat, d, length, flutes)["predicted_ct_sec"]
    truth = true_ct(mat, d, length, flutes)
    assert abs(pred - truth) / truth < 0.05, f"{code}: 예측 {pred}, 정답 {truth:.1f}"


def test_warns_when_outside_training_range(trained: CtModel):
    result = trained.predict("Steel", 40, 80, 4)
    assert result["warning"] is not None


def test_unknown_material_is_rejected(trained: CtModel):
    with pytest.raises(ValueError):
        trained.predict("Titanium", 10, 20, 4)


def test_predict_before_training_fails():
    with pytest.raises(NotTrainedError):
        CtModel().predict("Steel", 10, 20, 4)


def test_too_few_items_is_rejected():
    with pytest.raises(ValueError):
        CtModel().train(make_items()[:3])
