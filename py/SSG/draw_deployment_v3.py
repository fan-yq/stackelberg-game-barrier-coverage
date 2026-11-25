from dataclasses import dataclass
import json
import os
import shutil
import numpy as np
import matplotlib.pyplot as plt
import matplotlib as mpl
from data_structure import SSGResultV2
from draw_deployment import (
    draw_convex_hulls3,
    draw_single,
    draw_single_c,
    draw_traj,
    traj_to_vec,
)

def_optimal_color = "#146282"
def_optimal_color = "#F27A4D"  # "purple"
att_source_color = "#245B7D"
att_sink_color = "#5C80B5"  # "#A8CBDF"
att_path_color_op = "#AB1866"
att_path_color_op2 = "#C55490"
SourceLabel = "Launch sites"
SinkLabel = "Targets"


@dataclass
class DrawOptimalMode:
    draw_suboptimalAttacker: bool = True
    # 控制是否额外绘制"Top-N 次优路径"，以及取多少个。
    best_N_mode: int | None = None
    # 控制 Top-N 路径与 ε-best 路径"合并"还是"分层"画。
    merge_Best_Top_Best: bool = False
    has_legend: bool = True
    # Top-N 的放大倍数，与 best_N_mode 配合
    best_N_multi: int = 3
    text_path_index: bool = False


def draw_optimal_leader_strategies(
    result_data: SSGResultV2,
    output_folder: str,
    mode: DrawOptimalMode,
    eps=1e-2,
    eps_attacker=1e-2,
    figsize=(6, 4),
    prefix="",
):
    """
    只绘制最优的Leader策略（MaxMin策略）及其对应的攻击者响应

    Args:
        result_data: SSG结果数据
        output_folder: 输出文件夹路径
        eps: 误差阈值
        best_N_mode: 如果不为None，选取N个Attacker
    """
    os.makedirs(output_folder, exist_ok=True)

    umatrix = np.array(result_data["UMatrix"])
    cmap = mpl.colormaps["YlGnBu"]
    colors = cmap(np.linspace(0.2, 1, len(result_data["AirshipStrategies"])))

    print(
        "max distance: ",
        result_data["Setting"]["dMaxAttacker"],
        "maxmin: ",
        result_data["MaxMinU"],
        "minmax: ",
        result_data["MinMaxU"],
    )

    # 找出所有最优Leader策略
    optimal_indices = []
    for i in range(len(result_data["AirshipStrategies"])):
        rowmin = umatrix[i, :].min()
        if abs(result_data["MaxMinU"] - rowmin) < eps:
            optimal_indices.append(i)

    print(
        f"Found {len(optimal_indices)} optimal strategies (MaxMinU = {result_data['MaxMinU']})"
    )

    # 为每个最优策略画图
    # plt.style.use("ggplot")
    # plt.style.use("seaborn-v0_8-paper")
    plt.style.use("seaborn-v0_8")
    # plt.style.use("fivethirtyeight")
    # plt.style.use("bmh")
    custom_style = {
        "axes.facecolor": "#F5F5F7",
    }
    plt.rcParams.update(custom_style)
    for idx, i in enumerate(optimal_indices):
        airship_strategy = result_data["AirshipStrategies"][i]
        fig, ax = plt.subplots(figsize=figsize)

        # 绘制环境基础元素
        draw_convex_hulls3(ax, [result_data["Env"]["DeployArea1"]], "grey", "-")
        draw_convex_hulls3(ax, [result_data["Env"]["MissleArea1"]], "grey", ":")
        source = draw_single(
            ax, result_data["Env"]["Sources"], att_source_color, "^", False, s=60
        )
        sink = draw_single(
            ax, result_data["Env"]["Sinks"], att_sink_color, "x", False, s=60
        )

        # 绘制最优飞艇部署
        draw_single_c(
            ax,
            airship_strategy["Deployments"],
            color=def_optimal_color,
            marker="s",
            text_number=False,
            alpha=0.8,
            s=50,
        )

        # 找出对应的最优攻击者策略
        best_attacker_indices = []
        top_best_attacker_indices = []
        rowmin = umatrix[i, :].min()
        for j in range(umatrix.shape[1]):
            if abs(umatrix[i, j] - rowmin) < eps_attacker:
                best_attacker_indices.append(j)

        best_N_mode = mode.best_N_mode
        if best_N_mode is not None:
            indexs = sorted(range(umatrix.shape[1]), key=lambda k: umatrix[i, k])
            top_best_attacker_indices = list(indexs)[: mode.best_N_multi * best_N_mode]

        # 去重
        b1 = len(best_attacker_indices)
        best_attacker_indices = remove_duplicated(
            result_data, best_attacker_indices, None
        )
        b2 = len(top_best_attacker_indices)
        top_best_attacker_indices = remove_duplicated(
            result_data, top_best_attacker_indices, best_N_mode
        )
        # to filter to closed

        # 绘制攻击者轨迹
        count = 0
        for j, attacker_strategy in enumerate(result_data["AttackerStrategies"]):
            if mode.merge_Best_Top_Best:
                f1 = j in best_attacker_indices or j in top_best_attacker_indices
                f2 = mode.draw_suboptimalAttacker
            else:
                f1 = j in best_attacker_indices
                f2 = mode.draw_suboptimalAttacker or j in top_best_attacker_indices
            if f1:
                draw_traj(
                    ax,
                    attacker_strategy["Paths"],
                    color=att_path_color_op if count & 1 == 0 else att_path_color_op2,
                    alpha=0.9,
                )
                if mode.text_path_index:
                    p1 = attacker_strategy["Paths"][1]
                    ax.text(p1["Lon"], p1["Lat"], j)
                count += 1

                # 标记路径上的重要点
                if len(attacker_strategy["Paths"]) > 0:
                    start_point = attacker_strategy["Paths"][0]
                    end_point = attacker_strategy["Paths"][-1]
                    # ax.plot(
                    #     start_point["Lon"],
                    #     start_point["Lat"],
                    #     # "go",
                    #     "^",
                    #     markersize=10,
                    #     # label="Attack Start",
                    # )
                    # ax.plot(
                    #     end_point["Lon"],
                    #     end_point["Lat"],
                    #     "x",
                    #     # "r*",
                    #     markersize=12,
                    #     # label="Attack Target",
                    # )
            elif f2:
                # 非最优攻击者策略（理论上不会发生）
                draw_traj(ax, attacker_strategy["Paths"], color="lightgray", alpha=0.4)

        # 设置标题
        # title = f"Optimal Leader Strategy #{i+1} (MaxMin Solution)\n"
        # title += f"Leader Guaranteed Utility: {result_data['MaxMinU']:.4f}\n"
        # title += f"Number of Best Responses: {len(best_attacker_indices)}"
        # ax.set_title(title, fontsize=12, fontweight="bold")
        ax.set_xlabel("Longitude (°)")
        ax.set_ylabel("Latitude (°)")
        # ax.grid(True, alpha=0.3)
        ax.set_aspect("equal")

        # 添加图例
        from matplotlib.lines import Line2D

        legend_elements = [
            Line2D(
                [0],
                [0],
                color=att_source_color,
                marker="^",
                linestyle="",
                label=SourceLabel,
            ),
            Line2D(
                [0],
                [0],
                color=att_sink_color,
                marker="x",
                linestyle="",
                markeredgewidth=1.5,
                label=SinkLabel,
            ),
            Line2D([0], [0], color="grey", linestyle="-", label="Deployment Area"),
            Line2D([0], [0], color="grey", linestyle=":", label="Missile area"),
            Line2D(
                [0],
                [0],
                c=def_optimal_color,
                markerfacecolor="none",
                marker="s",
                markeredgewidth=1.5,
                linestyle="None",
                # markersize=8,
                label="Deployment",
            ),
            Line2D(
                [0],
                [0],
                color=att_path_color_op,
                linewidth=2.5,
                label="$\epsilon$-best paths",
            ),
        ]
        if mode.best_N_mode:
            legend_elements.append(
                Line2D(
                    [0],
                    [0],
                    color="silver",
                    linewidth=2.5,
                    label=f"Top {mode.best_N_mode} paths",
                ),
            )
        if mode.has_legend:
            # ax.legend(handles=legend_elements, loc="best", fontsize=9)
            fig.legend(handles=legend_elements, loc="outside upper center", ncols=2)

        # 保存图片
        filename = f"{prefix}optimal_{result_data['CaseName']}_{result_data['Method']}_{idx+1:02d}_s{i:03d}_u{result_data['MaxMinU']:.4f}.png"
        filepath = os.path.join(output_folder, filename)
        plt.savefig(filepath, dpi=300, bbox_inches="tight")
        plt.close(fig)

        print(
            f"Saved optimal strategy {i}: {filepath}, {len(best_attacker_indices)}/{b1}, {len(top_best_attacker_indices)}/{b2}"
        )

    print(
        f"\n✓ All {len(optimal_indices)} optimal leader strategies saved to: {output_folder}"
    )


def remove_duplicated(result_data: SSGResultV2, best_attacker_indices, best_N_mode):
    # 去重
    tol = 1e-2
    best_attacker_indices1 = []
    strategies = result_data["AttackerStrategies"]
    len_best_att = len(best_attacker_indices)
    dist_matrix = np.zeros((len_best_att, len_best_att))
    for i in range(len_best_att):
        for j in range(i, len_best_att):
            dist_matrix[i, j] = np.linalg.norm(
                traj_to_vec(strategies[best_attacker_indices[i]]["Paths"])
                - traj_to_vec(strategies[best_attacker_indices[j]]["Paths"])
            )
            dist_matrix[j, i] = dist_matrix[i, j]
    duplicates = set()
    for i in range(len_best_att):
        if i in duplicates:
            continue
        # 查找下三角矩阵中距离小于容差的索引（避免对称重复比较）
        dup_indices = np.where(dist_matrix[i, :i] < tol)[0]
        if len(dup_indices) > 0:
            duplicates.add(i)

    keep_indices = [i for i in range(len_best_att) if i not in duplicates]
    best_attacker_indices1 = [best_attacker_indices[i] for i in keep_indices]
    if best_N_mode is not None:
        best_attacker_indices1 = best_attacker_indices1[:best_N_mode]
    return best_attacker_indices1


def plot_optimal_strategies_from_file(
    file_path: str,
    mode: DrawOptimalMode,
    figsize=(6, 5),
    prefix="",
    eps=0.01,
    eps_attacker=0.01,
):
    """
    从JSON文件读取SSG结果，并绘制最优Leader策略

    Args:
        file_path: JSON文件路径
    """
    # 加载数据
    with open(file_path, "r") as f:
        res = json.load(f)

    # 从文件路径提取文件夹名称（去掉扩展名）
    base_name = prefix + os.path.splitext(os.path.basename(file_path))[0] + "_optimal"
    output_folder = os.path.join(os.path.dirname(file_path), base_name)
    if os.path.exists(output_folder):
        shutil.rmtree(output_folder)
    # 调用绘图函数
    draw_optimal_leader_strategies(
        res,
        output_folder,
        mode,
        eps=eps,
        figsize=figsize,
        prefix=prefix,
        eps_attacker=eps_attacker,
    )
    print(f"Plots saved to: {output_folder}")
