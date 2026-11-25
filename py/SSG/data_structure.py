import json
from typing import List, TypedDict


class PointGeo2D(TypedDict):
    Lat: float
    Lon: float


class AttackerStrategy(TypedDict):
    Paths: List[PointGeo2D]


class Env(TypedDict):
    Sources: List[PointGeo2D]
    Sinks: List[PointGeo2D]
    AntiMissles: List[PointGeo2D]
    SinkValues1: List[int]
    DeployArea1: list[list[float]]
    MissleArea1: list[list[float]]


class Setting(TypedDict):
    Radius: float
    K_detect: int
    K_anti: int
    d_anti: float
    dMaxAttacker: float


class AirshipMixedStrategy(TypedDict):
    Centers: List[PointGeo2D]
    Allocations: List[float]
    TotalAllocation: float


class SSGResult(TypedDict):
    AirshipMixedStrategy: AirshipMixedStrategy
    Env: Env
    Setting: Setting
    Ua: float
    Iteration: int
    CreateAt: str


class AirshipStrategy2(TypedDict):
    Deployments: List[PointGeo2D]


class SSGResultV2(TypedDict):
    AirshipMixedStrategy: AirshipMixedStrategy
    AirshipStrategies: list[AirshipStrategy2]
    AttackerStrategies: list[AttackerStrategy]
    Env: Env
    Setting: Setting
    Iteration: int
    CreateAt: str
    UMatrix: list[list[float]]
    MaxMinU: float
    MinMaxU: float
    CaseName: str | None
    Method: str | None


def load_SSGResult(file) -> SSGResult:
    return json.load(open(file))
