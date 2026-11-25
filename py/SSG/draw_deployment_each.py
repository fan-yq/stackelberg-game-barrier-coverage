from dataclasses import dataclass
import json
import os
import shutil
from matplotlib.axes import Axes
import numpy as np
import matplotlib.pyplot as plt
import matplotlib as mpl
from data_structure import SSGResultV2
from draw_deployment import (
    draw_convex_hulls3,
    draw_single,
    draw_single_c,
    draw_traj,
)

def_optimal_color = "#F27A4D"  # "purple"
att_source_color = "#245B7D"
att_sink_color = "#5C80B5"  # "#A8CBDF"
att_path_color_op = "#AB1866"
att_path_color_op2 = "#C55490"
SourceLabel = "Launch sites"
SinkLabel = "Targets"


@dataclass
class DrawEachMode:
    draw_suboptimalAttacker: bool = True
    text_path_index: bool = False


def draw_each_strategy_separately(
    result_data: SSGResultV2,
    output_folder: str,
    mode: DrawEachMode,
    eps=1e-2,
):
    """
    将每个飞艇策略及其对应的最优攻击者策略单独画图并保存

    Args:
        result_data: SSG结果数据
        output_folder: 输出文件夹路径
        eps: 误差阈值
    """
    # 创建输出文件夹
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

    # 为每个飞艇策略单独画图
    for i, airship_strategy in enumerate(result_data["AirshipStrategies"]):
        # 创建新图
        fig, ax = plt.subplots(figsize=(12, 10))
        ax: Axes = ax

        # 绘制环境基础元素
        draw_convex_hulls3(ax, [result_data["Env"]["DeployArea1"]], "grey", "-")
        draw_convex_hulls3(ax, [result_data["Env"]["MissleArea1"]], "grey", ":")
        draw_single(ax, result_data["Env"]["Sources"], "red", "^")
        draw_single(ax, result_data["Env"]["Sinks"], "red", "x")

        # 获取当前飞艇策略的收益值
        rowmin = umatrix[i, :].min()
        rowmax = umatrix[i, :].max()

        # 判断是否为全局最优策略（leader最优）
        is_optimal = (result_data["MaxMinU"] - rowmin) < eps

        # 绘制当前飞艇部署
        if is_optimal:
            draw_single_c(
                ax,
                airship_strategy["Deployments"],
                color=def_optimal_color,
                marker="s",
                text_number=False,
                alpha=0.8,
                s=50,
            )
            title_prefix = "★ Optimal Leader Strategy"
        else:
            draw_single_c(
                ax,
                airship_strategy["Deployments"],
                color="green",
                marker="s",
                text_number=False,
                alpha=0.8,
                s=50,
            )
            title_prefix = f"Leader Strategy (Utility: {rowmin:.4f})"

        # 找出当前飞艇策略对应的最优攻击者策略
        # 对于给定的飞艇策略，攻击者会选择最小化飞艇收益的策略
        # 所以最优攻击者策略是那些使得umatrix[i, j]最小的j
        best_attacker_indices = []
        min_value = rowmin
        for j in range(umatrix.shape[1]):
            if umatrix[i, j] - min_value < eps:
                best_attacker_indices.append(j)

        # 绘制攻击者轨迹 - 区分最优和非最优
        for j, attacker_strategy in enumerate(result_data["AttackerStrategies"]):
            if j in best_attacker_indices:
                # 最优攻击者策略用实线突出显示
                draw_traj(
                    ax,
                    attacker_strategy["Paths"],
                    color="blue",
                    alpha=0.9,
                )
                if mode.text_path_index:
                    p1 = attacker_strategy["Paths"][1]
                    ax.text(p1["Lon"], p1["Lat"], j)

            elif mode.draw_suboptimalAttacker:
                # 非最优攻击者策略用淡灰色显示
                draw_traj(
                    ax,
                    attacker_strategy["Paths"],
                    color="silver",
                    alpha=0.3,
                )

        # 设置标题，包含详细信息
        title = f"{title_prefix} #{i+1}\n"
        title += f"Leader Utility: min={rowmin:.4f}, max={rowmax:.4f}\n"
        title += f"Best Attacker Responses: {len(best_attacker_indices)} strategy(ies)"
        ax.set_title(title, fontsize=12, fontweight="bold")
        ax.set_xlabel("X Coordinate", fontsize=11)
        ax.set_ylabel("Y Coordinate", fontsize=11)
        ax.grid(True, alpha=0.3)
        ax.set_aspect("equal")

        # 保存图片
        utility_str = f"{rowmin:.4f}".replace(".", "_")
        filename = f"leader_strategy_{i:03d}_utility_{utility_str}.png"
        filepath = os.path.join(output_folder, filename)
        plt.savefig(filepath, dpi=300, bbox_inches="tight")
        plt.close(fig)

        print(
            f"Saved: {filepath} (Leader utility: {rowmin:.4f}, Best attackers: {len(best_attacker_indices)})"
        )

    print(
        f"\n✓ All {len(result_data['AirshipStrategies'])} leader strategies saved to: {output_folder}"
    )


def plot_each_strategies_from_file(file_path: str, mode: DrawEachMode):
    # 加载数据
    with open(file_path, "r") as f:
        res = json.load(f)

    # 从文件路径提取文件夹名称（去掉扩展名）
    base_name = os.path.splitext(os.path.basename(file_path))[0]
    output_folder = os.path.join(os.path.dirname(file_path), base_name)
    if os.path.exists(output_folder):
        shutil.rmtree(output_folder)

    # 调用绘图函数
    draw_each_strategy_separately(res, output_folder, mode)
    print(f"Plots saved to: {output_folder}")
