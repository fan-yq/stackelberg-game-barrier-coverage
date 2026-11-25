import json
import os
import sys
import matplotlib as mpl
from matplotlib.axes import Axes
import numpy as np
import matplotlib.pyplot as plt
from typing import TypedDict, Any, Dict
from data_structure import SSGResultV2, load_SSGResult, PointGeo2D

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from DAO.draw_obstacles import great_circle_distance0


def draw_allocation_hex(ax: Axes, userRates: list[float], users: list[PointGeo2D]):
    lats = [pos["Lat"] for pos in users]
    lons = [pos["Lon"] for pos in users]
    hexbin = ax.hexbin(
        lons,
        lats,
        C=np.array(userRates),
        gridsize=36,  # 分箱数量
        cmap="YlOrRd",
        reduce_C_function=np.mean,
    )  # 每个bin内取平均速率
    plt.colorbar(hexbin, label="allocation")


def draw_single(
    ax,
    positions: list[PointGeo2D],
    color,
    marker=".",
    text_number=True,
    alpha=1,
    s=None,
):
    if positions == []:
        return

    lats = [pos["Lat"] for pos in positions]
    lons = [pos["Lon"] for pos in positions]
    scatter = ax.scatter(lons, lats, color=color, marker=marker, alpha=alpha, s=s)
    if text_number:
        width = max(map(lambda p: p["Lon"], positions)) - min(
            map(lambda p: p["Lon"], positions)
        )
        height = max(map(lambda p: p["Lat"], positions)) - min(
            map(lambda p: p["Lat"], positions)
        )
        for i in range(len(positions)):
            ax.text(lons[i] + 0.01 * width, lats[i] + 0.01 * height, i)
    return scatter


def draw_single_c(
    ax,
    positions: list[PointGeo2D],
    color,
    marker=".",
    text_number=True,
    alpha=1,
    s=None,
):
    if positions == []:
        return

    lats = [pos["Lat"] for pos in positions]
    lons = [pos["Lon"] for pos in positions]
    scatter = ax.scatter(
        lons,
        lats,
        marker=marker,
        alpha=alpha,
        s=s,
        c="none",
        linewidths=1.5,
        edgecolors=color,
    )
    if text_number:
        width = max(map(lambda p: p["Lon"], positions)) - min(
            map(lambda p: p["Lon"], positions)
        )
        height = max(map(lambda p: p["Lat"], positions)) - min(
            map(lambda p: p["Lat"], positions)
        )
        for i in range(len(positions)):
            ax.text(lons[i] + 0.01 * width, lats[i] + 0.01 * height, i)
    return scatter


def draw_traj(
    ax: Axes, positions: list[PointGeo2D], color=None, alpha=1, linestyle="-"
):
    lats = [pos["Lat"] for pos in positions]
    lons = [pos["Lon"] for pos in positions]
    ax.plot(lons, lats, c=color, alpha=alpha, linestyle=linestyle)


def traj_to_vec(positions: list[PointGeo2D]):
    p = []
    for pos in positions:
        p.append(pos["Lat"])
        p.append(pos["Lon"])
    return np.array(p)


def draw_traj_and_deployment_v2(ax, result_data: SSGResultV2, eps=1e-2):

    umatrix = np.array(result_data["UMatrix"])
    cmap = mpl.colormaps["YlGnBu"]
    colors = cmap(np.linspace(0.2, 1, len(result_data["AirshipStrategies"])))
    draw_convex_hulls3(ax, [result_data["Env"]["DeployArea1"]], "grey", "-")
    draw_convex_hulls3(ax, [result_data["Env"]["MissleArea1"]], "grey", ":")

    draw_single(ax, result_data["Env"]["Sources"], "red", "^")
    draw_single(ax, result_data["Env"]["Sinks"], "red", "x")

    print(
        "max distance: ",
        result_data["Setting"]["dMaxAttacker"],
        "maxmin: ",
        result_data["MaxMinU"],
        "minmax: ",
        result_data["MinMaxU"],
    )

    # for j, st in enumerate(result_data["AttackerStrategies"]):
    #     d = path_distance(st["Paths"])
    #     colmax = umatrix[:, j].max()
    #     if (
    #         d < result_data["Setting"]["dMaxAttacker"]
    #         and colmax - result_data["MinMaxU"] < eps
    #     ):
    #         draw_traj(ax, st["Paths"])
    #         # print(d)
    #     else:
    #         draw_traj(ax, st["Paths"], "silver", 0.2)

    selectPath = set()

    for i, strategy in enumerate(result_data["AirshipStrategies"]):
        rowmin = umatrix[i, :].min()
        if result_data["MaxMinU"] - rowmin < eps:
            draw_single(ax, strategy["Deployments"], color=colors[i], text_number=False)
            for j in range(umatrix.shape[1]):
                if result_data["MaxMinU"] - umatrix[i, j] < eps:
                    selectPath.add(j)
        else:
            draw_single(
                ax,
                strategy["Deployments"],
                color="silver",
                text_number=False,
                alpha=0.1,
            )
            # print("skip, row min: ", rowmin)

    for j, st in enumerate(result_data["AttackerStrategies"]):
        d = path_distance(st["Paths"])
        if j in selectPath:
            draw_traj(ax, st["Paths"])
            # print(d)
        else:
            draw_traj(ax, st["Paths"], "silver", 0.2)


#
def draw_convex_hulls3(
    ax: Axes,
    convex_hulls: list,
    color,
    linestyle,
    label=False,
    pruneLessThan=0,
):
    """Draw convex hulls on given axes"""
    for i, hull in enumerate(convex_hulls):
        if len(hull) < pruneLessThan:
            continue
        lats = [v[0] for v in hull]
        lons = [v[1] for v in hull]
        lats.append(hull[0][0])
        lons.append(hull[0][1])
        ax.plot(lons, lats, color=color, linestyle=linestyle)
        if label:
            ax.text(lons[0], lats[0], i)


def path_distance(path: list[PointGeo2D]):
    prev_lat, prev_lon = path[0].values()
    dist = 0
    for p in path:
        cur_lat, cur_lon = p.values()
        dist += great_circle_distance0(prev_lat, prev_lon, cur_lat, cur_lon)
        prev_lat, prev_lon = cur_lat, cur_lon
    return dist


if __name__ == "__main__":
    result_data = load_SSGResult("data/SSG/SSG_NSO3_NSI3_NAI5_NC3_IT1000.json")
    # # draw_heatmap(result_data)
    f, ax = plt.subplots()
    # draw_allocation_hex(
    #     ax,
    #     result_data["AirshipMixedStrategy"]["Allocations"],
    #     result_data["AirshipMixedStrategy"]["Centers"],
    # )
    # draw_single(ax, result_data["Env"]["Sources"], "red", "^")
    # draw_single(ax, result_data["Env"]["Sinks"], "red", "x")

    # for st in result_data["AttackerStrategies"]:
    #     draw_traj(ax, st["Paths"])

    draw_traj_and_deployment_v2(ax, result_data)
    plt.show()
