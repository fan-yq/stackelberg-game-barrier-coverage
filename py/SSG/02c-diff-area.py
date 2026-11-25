from os import path

from draw_deployment_v3 import DrawOptimalMode, plot_optimal_strategies_from_file

basePath = "data/SSG"
files = [
    # "SSG_LargeArea20260515_NSO3_NSI3_NAI12_NC3_IT3000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json ",
    # "SSG_LargeArea20260515_NSO2_NSI3_NAI12_NC3_IT3000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json ",
    "SSG_LargeArea20260515_NSO3_NSI3_NAI16_NC3_IT3000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json ",
    # "SSG_LargeArea20260515_NSO3_NSI4_NAI12_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json ",
    # "SSG_LargeArea20260515_NSO3_NSI3_NAI12_NC2_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
]

mode = DrawOptimalMode(
    False, merge_Best_Top_Best=False, best_N_mode=20, has_legend=False, best_N_multi=100
)

for file in files:
    plot_optimal_strategies_from_file(
        path.join(basePath, file), mode, prefix="02c-", eps=0.03, eps_attacker=0.005
    )
