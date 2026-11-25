# 2025/1/3

import json
from matplotlib import pyplot as plt
from matplotlib.axes import Axes
from matplotlib.patches import Polygon
import numpy as np
import cartopy.crs as ccrs

obstaclefile = "data/obstacles/radiation-ECfraction.json"

# Earth radius in meters
EARTH_RADIUS = 6371000


def great_circle_distance(lat1, lon1, lat2, lon2):
    """
    计算两个经纬度点之间的大圆距离（单位：千米）
    使用 Haversine 公式
    """
    R = 6371.0  # 地球半径（单位：km）

    return great_circle_distance0(lat1, lon1, lat2, lon2) * R


def great_circle_distance0(lat1, lon1, lat2, lon2):
    """
    计算两个经纬度点之间的大圆距离（单位：千米）
    使用 Haversine 公式
    """

    lat1, lon1, lat2, lon2 = map(np.radians, [lat1, lon1, lat2, lon2])

    dlat = lat2 - lat1
    dlon = lon2 - lon1

    a = np.sin(dlat / 2) ** 2 + np.cos(lat1) * np.cos(lat2) * np.sin(dlon / 2) ** 2
    c = 2 * np.arctan2(np.sqrt(a), np.sqrt(1 - a))

    return c


def HullPointsToLatLon(hull: list[list], lonOffset=0, max_dist_km=100.0):
    """
    将 hull 点转为 lat/lon 列表，并在距离过远的相邻点之间插值加密

    :param hull: 输入点列表，每个点是 [lat, lon]
    :param lonOffset: 经度偏移量
    :param max_dist_km: 最大允许距离（km），超过则插值
    :return: (lats, lons) 加密后的纬度和经度列表
    """
    if len(hull) < 2:
        # 如果只有一个点或没有点，直接返回
        lats = [hull[-1][0]] if hull else []
        lons = [hull[-1][1] + lonOffset] if hull else []
        return lats, lons

    # 添加最后一个点作为起点（根据原逻辑）
    prev_lat = hull[-1][0]
    prev_lon = hull[-1][1] + lonOffset
    lats = [prev_lat]
    lons = [prev_lon]

    for point in hull:
        curr_lat = point[0]
        curr_lon = point[1] + lonOffset

        # 计算两点间距离
        dist = great_circle_distance(prev_lat, prev_lon, curr_lat, curr_lon)

        # 插值数量：根据距离决定插入多少个中间点
        if dist > max_dist_km:
            num_steps = int(np.ceil(dist / max_dist_km))  # 每段不超过 max_dist_km
            for step in range(1, num_steps + 1):
                ratio = step / num_steps
                # 线性插值（适用于小范围，精度可接受）
                lat_interp = prev_lat + ratio * (curr_lat - prev_lat)
                lon_interp = prev_lon + ratio * (curr_lon - prev_lon)
                lats.append(lat_interp)
                lons.append(lon_interp)
        else:
            lats.append(curr_lat)
            lons.append(curr_lon)

        # 更新前一个点
        prev_lat, prev_lon = curr_lat, curr_lon

    return lats, lons


def draw_cloud_area(ax: Axes, obstacle: dict, cmap="viridis"):
    """Draw cloud area contour on given axes"""

    contour = ax.contourf(
        obstacle["Longitudes"], obstacle["Latitudes"], obstacle["BitmapFlat"], cmap=cmap
    )
    plt.colorbar(contour)

    ax.set_xlabel("Longitude (degrees)")
    ax.set_ylabel("Latitude (degrees)")
    ax.grid(True)
    return contour


def draw_convex_hulls(
    ax: Axes,
    convex_hulls: list,
    color,
    linestyle,
    LonOffset=0,
    label=False,
    pruneLessThan=0,
):
    """Draw convex hulls on given axes"""
    for i, hull in enumerate(convex_hulls):
        if len(hull) < pruneLessThan:
            continue
        lats, lons = HullPointsToLatLon(hull, LonOffset)
        ax.plot(lons, lats, color=color, linestyle=linestyle)
        if label:
            ax.text(lons[0], lats[0], i)


def draw_convex_hulls_fill(
    ax: Axes, convex_hulls: list, color, alpha=0.15, pruneLessThan=0, hasTransform=False
):
    """Draw convex hulls on given axes"""
    polygons = []
    for i, hull in enumerate(convex_hulls):
        if len(hull) < pruneLessThan:
            continue
        lats, lons = HullPointsToLatLon(hull, 0)
        if hasTransform:
            polygon1 = Polygon(
                list(zip(lons, lats)),
                color=color,
                alpha=alpha,
                transform=ccrs.PlateCarree(),
            )
        else:
            polygon1 = Polygon(list(zip(lons, lats)), color=color, alpha=alpha)
        ax.add_patch(polygon1)
        polygons.append(polygon1)
    return polygons


def draw_convex_hulls2(
    ax: Axes,
    convex_hulls: list,
    color,
    linestyle,
    alpha=1,
    pruneLessThan=0,
):
    """Draw convex hulls on given axes"""
    for i, hull in enumerate(convex_hulls):
        if len(hull) < pruneLessThan:
            continue
        lats, lons = HullPointsToLatLon(hull)
        if any(map(lambda lat: lat > 70 or lat < -70, lats)):
            N = len(lats)
            light = "#E4F1F7"
            for i in range(N):
                ax.plot(
                    [lons[i], lons[(i + 1) % N]],
                    [lats[i], lats[(i + 1) % N]],
                    color=light,
                    linestyle=linestyle,
                    transform=ccrs.PlateCarree(),
                    alpha=alpha,
                    linewidth=1.2,
                )
        else:
            ax.plot(
                lons,
                lats,
                color=color,
                linestyle=linestyle,
                transform=ccrs.PlateCarree(),
                alpha=alpha,
                linewidth=1.2,
            )


def draw_paths(ax, paths: dict, plot_tree=False, LonOffset=0):
    # path: head tail Path[][2] Edges[][4]
    path = np.array(paths["Path"])
    if plot_tree:
        for edge in paths["Edges"]:
            ax.plot(
                np.array(edge[1::2]) + LonOffset, edge[::2], color="gray", alpha=0.5
            )
    ax.plot(path[:, 1] + LonOffset, path[:, 0], color="orange", marker=".")


def draw_paths2(
    ax,
    paths: dict,
    plot_tree=False,
    color="orange",
    linestyle="-",
    label=None,
    alpha=0.05,
):
    # path: head tail Path[][2] Edges[][4]
    path = np.array(paths["Path"])
    if plot_tree:
        for edge in paths["Edges"]:
            ax.plot(
                np.array(edge[1::2]),
                edge[::2],
                color="silver",
                alpha=alpha,
                transform=ccrs.PlateCarree(),
            )
        print("tree nodes:", len(paths["Edges"]))
    ax.plot(
        path[:, 1],
        path[:, 0],
        color=color,
        marker=".",
        transform=ccrs.PlateCarree(),
        linestyle=linestyle,
        label=label,
    )


if __name__ == "__main__":
    # Load obstacle data and create plot
    obstacleData: dict = json.load(open(obstaclefile))
    obstacle = {}
    for k, v in obstacleData["cloudArea"].items():
        obstacle[k] = np.array(v)
    f, ax = plt.subplots()

    draw_cloud_area(ax, obstacle)
    draw_convex_hulls(ax, obstacleData["convexHulls"], "orange", ":")
    draw_convex_hulls(ax, obstacleData["mergedHulls"], "red", "-")

    plt.show()
