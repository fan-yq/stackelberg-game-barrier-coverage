from os import path

from draw_deployment_select import plot_optimal_strategies_from_file_v4, DrawSelectMode

basePath = "data/SSG"
files = [
    "SSG_LargeArea20260515_NSO3_NSI3_NAI12_NC3_IT3000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
    "SSG_LargeArea20260515_NSO3_NSI3_NAI16_NC3_IT3000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
    "SSG_LargeArea20260515_NSO3_NSI3_NAI20_NC3_IT3000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
]

mode = DrawSelectMode(False, best_N_mode=10, has_legend=True, best_N_multi=10)

for file in files:
    plot_optimal_strategies_from_file_v4(
        path.join(basePath, file), mode, prefix="S2-", eps=0.001, eps_attacker=0.01
    )
