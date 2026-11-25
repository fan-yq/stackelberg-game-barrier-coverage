from os import path

from draw_deployment_each import plot_each_strategies_from_file, DrawEachMode
from draw_deployment_select import plot_select_strategies_from_file, DrawSelectMode

basePath = "data/SSG"


files = [
    # "SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
    "SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20261112.json"
]
mode = DrawEachMode(False, True)
# for file in files:
#     plot_each_strategies_from_file(path.join(basePath, file), mode)

mode = DrawSelectMode(True, False, best_N_mode=10)
plot_select_strategies_from_file(
    path.join(basePath, files[0]), {37: []}, 0.001, prefix="02-", mode=mode
)
