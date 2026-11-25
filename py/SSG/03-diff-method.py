from os import path

from draw_deployment_v3 import DrawOptimalMode, plot_optimal_strategies_from_file
from draw_deployment_select import plot_optimal_strategies_from_file_v4, DrawSelectMode

basePath = "data/SSG"
files = [
    "SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_ACDE_crowdingFalse_fixProj_s20260515.json",
    # "SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDEDefH_crowdingFalse_fixProj_s20260515.json",
    # "SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDEPureDE_crowdingFalse_fixProj_s20260515.json",
    "SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT200_EPS20.001_AttackerOracle2_crowdingFalse_fixProj_s20260515.json",
    "PredefinedLine_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_fixProj_s20260515.json",
]

mode = DrawSelectMode(False, best_N_mode=10, has_legend=False, best_N_multi=5)

for file in files:
    plot_optimal_strategies_from_file_v4(
        path.join(basePath, file), mode, prefix="03-", eps=0.001, eps_attacker=0.001
    )
