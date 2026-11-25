from os import path

from draw_deployment_v3 import DrawOptimalMode, plot_optimal_strategies_from_file

basePath = "data/SSG"
files = [
    "SSG_HullArea420260515_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
    "SSG_HullArea520260515_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json",
]

mode = DrawOptimalMode(
    False, merge_Best_Top_Best=False, best_N_mode=10, has_legend=True, best_N_multi=10
)

for file in files:
    plot_optimal_strategies_from_file(
        path.join(basePath, file), mode, prefix="S1-", eps=0.0001, eps_attacker=0.01
    )
