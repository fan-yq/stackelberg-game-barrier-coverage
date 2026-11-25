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

def_suboptimal_color = "green"
def_optimal_color = "#F27A4D"  # "purple"
att_source_color = "#245B7D"
att_sink_color = "#5C80B5"  # "#A8CBDF"
att_path_color_op = "#AB1866"
att_path_color_op2 = "#C55490"
predefined_att_path_color = "#007068"
SourceLabel = "Launch sites"
SinkLabel = "Targets"


@dataclass
class DrawSelectMode:
    is_suboptimal: bool = False
    has_legend: bool = True
    best_N_mode: int | None = None
    best_N_multi: int = 3
    Draw_target_value: bool = False


def find_best_attacker_indices(umatrix, i, eps, result_data: SSGResultV2):
    best_attacker_indices = []
    rowmin = umatrix[i, :].min()
    for j in range(umatrix.shape[1]):
        if abs(umatrix[i, j] - rowmin) <= eps:
            best_attacker_indices.append(j)

    # 去重
    b1 = len(best_attacker_indices)
    best_attacker_indices = remove_duplicated(result_data, best_attacker_indices, None)
    return best_attacker_indices


def find_top_best_attacker_indidces(
    umatrix, i, best_N_multi, best_N_mode, result_data: SSGResultV2
):
    indexs = sorted(range(umatrix.shape[1]), key=lambda k: umatrix[i, k])
    top_best_attacker_indices = list(indexs)[: best_N_multi * best_N_mode]
    top_best_attacker_indices = remove_duplicated(
        result_data, top_best_attacker_indices, best_N_mode
    )

    return top_best_attacker_indices


def find_eps_defender_indices(result_data: SSGResultV2, umatrix, eps):
    optimal_indices = []
    for i in range(len(result_data["AirshipStrategies"])):
        rowmin = umatrix[i, :].min()
        if abs(result_data["MaxMinU"] - rowmin) <= eps:
            optimal_indices.append(i)
    return optimal_indices


# optimal + select with utility
def draw_select_leader_strategies(
    result_data: SSGResultV2,
    output_folder: str,
    select: dict[int, list[int]],  # def-index: att-array
    mode: DrawSelectMode,
    eps=1e-2,
    figsize=(6, 4),
    prefix="",
    text_u=False,
    predefined_data: dict[int, SSGResultV2] | None = None,
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

    print(
        "max distance: ",
        result_data["Setting"]["dMaxAttacker"],
        "maxmin: ",
        result_data["MaxMinU"],
        "minmax: ",
        result_data["MinMaxU"],
    )

    # 找出所有最优Leader策略
    optimal_indices = list(select.keys())

    plt.style.use("seaborn-v0_8")
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

        if mode.Draw_target_value:
            for indexSink, s in enumerate(result_data["Env"]["SinkValues1"]):
                p1 = result_data["Env"]["Sinks"][indexSink]
                ax.text(p1["Lon"], p1["Lat"], f"{s:.3f}")

        # 绘制最优飞艇部署
        draw_single_c(
            ax,
            airship_strategy["Deployments"],
            color=def_suboptimal_color if mode.is_suboptimal else def_optimal_color,
            marker="s",
            text_number=False,
            alpha=0.8,
            s=50,
        )

        # 找出对应的最优攻击者策略
        best_attacker_indices = find_best_attacker_indices(umatrix, i, eps, result_data)

        top_best_attacker_indidces = []

        if mode.best_N_mode is not None:
            top_best_attacker_indidces = find_top_best_attacker_indidces(
                umatrix, i, mode.best_N_multi, mode.best_N_mode, result_data
            )

        # 绘制攻击者轨迹
        count = 0
        for j, attacker_strategy in enumerate(result_data["AttackerStrategies"]):
            if j in best_attacker_indices:
                draw_traj(
                    ax,
                    attacker_strategy["Paths"],
                    color=att_path_color_op if count & 1 == 0 else att_path_color_op2,
                    alpha=0.9,
                )
                count += 1

            elif j in select[i] or j in top_best_attacker_indidces:
                # 非最优攻击者策略（理论上不会发生）
                draw_traj(
                    ax,
                    attacker_strategy["Paths"],
                    color="lightgray",
                    alpha=0.8,
                )
            else:
                continue

            if text_u:
                p1 = attacker_strategy["Paths"][2]
                ax.text(p1["Lon"], p1["Lat"], f"{j}:{result_data['UMatrix'][i][j]:.3f}")

        # 绘制predefined data
        hasPrefinedData = (
            predefined_data is not None and predefined_data.get(i) is not None
        )
        if hasPrefinedData:
            predefined_result = predefined_data.get(i)
            umatrix1 = np.array(predefined_result["UMatrix"])
            best_attacker_indices_predefined = find_best_attacker_indices(
                umatrix1, 0, eps, predefined_result
            )
            for j, attacker_strategy in enumerate(
                predefined_result["AttackerStrategies"]
            ):
                if j in best_attacker_indices_predefined:
                    draw_traj(
                        ax,
                        attacker_strategy["Paths"],
                        color=predefined_att_path_color,
                        alpha=0.5,
                        linestyle="--",
                    )
                    if text_u:
                        p1 = attacker_strategy["Paths"][2]
                        ax.text(
                            p1["Lon"],
                            p1["Lat"],
                            f"{j}:{predefined_result['UMatrix'][0][j]:.3f}",
                        )

        # 设置标题
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
            Line2D([0], [0], color="grey", linestyle="-", label="Deploy area"),
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
            Line2D(
                [0],
                [0],
                color="silver",
                linewidth=2.5,
                label="Inferior Paths",
            ),
        ]
        if hasPrefinedData:
            legend_elements.append(
                Line2D(
                    [0],
                    [0],
                    color=predefined_att_path_color,
                    linewidth=2.5,
                    linestyle="--",
                    alpha=0.5,
                    label="Near-optimal path",
                ),
            )
        if mode.has_legend:
            fig.legend(handles=legend_elements, loc="outside upper center", ncols=2)

        # 保存图片
        tu = "_textu" if text_u else ""
        filename = f"{prefix}select_{result_data['CaseName']}_{result_data['Method']}_{idx+1:02d}_s{i+1:03d}_u{min(result_data['UMatrix'][i]):.4f}{tu}.png"
        filepath = os.path.join(output_folder, filename)
        plt.savefig(filepath, dpi=300, bbox_inches="tight")
        plt.close(fig)

        print(f"Saved select strategy {i+1}: {filepath}, {len(best_attacker_indices)}")

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


def plot_select_strategies_from_file(
    file_path: str | SSGResultV2,
    select: dict[int, list],
    eps=0.01,
    figsize=(6, 5),
    prefix="",
    mode=None,
):
    """
    从JSON文件读取SSG结果，并绘制最优Leader策略

    Args:
        file_path: JSON文件路径
    """
    # 加载数据
    if isinstance(file_path, dict):
        res = file_path
        file_path = res["filepath"]
    else:
        with open(file_path, "r") as f:
            res = json.load(f)

    # 从文件路径提取文件夹名称（去掉扩展名）
    base_name = prefix + os.path.splitext(os.path.basename(file_path))[0] + "_select"
    output_folder = os.path.join(os.path.dirname(file_path), base_name)
    if os.path.exists(output_folder):
        shutil.rmtree(output_folder)

    if mode is None:
        mode = DrawSelectMode()

    # 自动加载predefined
    basePath = os.path.dirname(file_path)
    predefined_res = {}
    for k in select.keys():
        predefinedFileName = os.path.join(
            basePath, f"PredefinedCase_defIndex{k}_{os.path.basename(file_path)}"
        )
        if os.path.exists(predefinedFileName):
            pre_res = json.load(open(predefinedFileName))
            predefined_res[k] = pre_res
            print("load predefined:", predefinedFileName)

    # 调用绘图函数
    draw_select_leader_strategies(
        res,
        output_folder,
        select,
        mode,
        eps,
        figsize=figsize,
        prefix=prefix,
        text_u=True,
        predefined_data=predefined_res,
    )
    draw_select_leader_strategies(
        res,
        output_folder,
        select,
        mode,
        eps,
        figsize=figsize,
        prefix=prefix,
        predefined_data=predefined_res,
    )
    print(f"Plots saved to: {output_folder}")


def plot_optimal_strategies_from_file_v4(
    file_path: str,
    mode: DrawSelectMode,
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
        res["filepath"] = file_path

    umatrix = np.array(res["UMatrix"])
    optimal_indices = find_eps_defender_indices(res, umatrix, eps)
    # 一次性绘制所有最优 leader：输出目录由 plot_select_strategies_from_file 统一清空一次，
    # 每个 leader 按文件名中的 s{i:03d} 区分，避免逐个调用时清空目录只剩最后一个。
    plot_select_strategies_from_file(
        res,
        {int(def_index): [] for def_index in optimal_indices},
        eps_attacker,
        figsize=figsize,
        prefix=prefix,
        mode=mode,
    )
