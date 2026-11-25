from os import path

from draw_deployment_v3 import DrawOptimalMode, plot_optimal_strategies_from_file

basePath = "data/SSG"
# 不同数量1，6，12
# 不同疏密
files = [
    "SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
    "SSG_HullArea420260515_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
    "SSG_HullArea420260515_NSO3_NSI3_NAI12_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
    # diff
    "SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json",
]

mode = DrawOptimalMode(
    False, merge_Best_Top_Best=False, best_N_mode=5, has_legend=False, best_N_multi=5
)

for file in files:
    plot_optimal_strategies_from_file(
        path.join(basePath, file), mode, prefix="02b-", eps=0.001, eps_attacker=0.005
    )
