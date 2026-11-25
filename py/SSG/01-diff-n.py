from os import path

from draw_deployment_v3 import DrawOptimalMode, plot_optimal_strategies_from_file
from draw_deployment_each import plot_each_strategies_from_file, DrawEachMode

basePath = "data/SSG"
files = [
    "SSG_Case320260609_NSO3_NSI3_NAI2_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json",  #
    # "PredefinedCase_defIndex3_SSG_Case320260609_NSO3_NSI3_NAI2_NC3_IT1000_EPS20.001_SSGDEDefH_crowdingFalse_fixProj_s20260609.json",
    # "PredefinedCase_defIndex43_SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDEDefH_crowdingFalse_fixProj_s20260609.json",
]

# mode = DrawOptimalMode(
#     False, merge_Best_Top_Best=True, best_N_mode=10, text_path_index=True
# )
# mode1 = DrawEachMode(False, True)
# for file in files:
#     plot_optimal_strategies_from_file(
#         path.join(basePath, file), mode, (5, 6), prefix="01-"
#     )
#     plot_each_strategies_from_file(path.join(basePath, file), mode1)

from draw_deployment_select import plot_select_strategies_from_file

files = [
    "SSG_Case320260609_NSO3_NSI3_NAI2_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json",  #
]
plot_select_strategies_from_file(
    path.join(basePath, files[0]),
    {13: [16, 241, 96, 56]},
    0.001,
    (3, 4.5),
    prefix="01-",
)
plot_select_strategies_from_file(
    path.join(basePath, files[1]),
    {48: [901, 748, 854, 1200, 1730]},
    0.001,
    (3, 4.5),
    prefix="01-",
)
