from matplotlib import pyplot as plt
from matplotlib.axes import Axes
import numpy as np

from typing import TypedDict, List


class DAOWindFieldJSON(TypedDict):
    latitudes: List[float]
    longitudes: List[float]
    field: List[List[List[List[float]]]]


class DAOWindFieldV2(TypedDict):
    latitudes: List[float]
    longitudes: List[float]
    levels: List[float]
    field: List[List[List[List[List[float]]]]]


def set_bound(ax: Axes, windfield: DAOWindFieldJSON):
    latitudes = windfield["latitudes"]
    longitudes = windfield["longitudes"]
    ax.set_xlim(longitudes[0], longitudes[-1])
    ax.set_ylim(latitudes[-1], latitudes[0])


def set_xylabel(ax: Axes):
    ax.set_xlabel("Longitude (°)")
    ax.set_ylabel("Latitude (°)")


def draw_windfield(ax: Axes, windfield: DAOWindFieldJSON, i=0, step=1):
    field = np.array(windfield["field"][i])
    longitudes = windfield["longitudes"]
    latitudes = windfield["latitudes"]
    contour = ax.contourf(
        longitudes,
        latitudes,
        np.linalg.norm(field, 2, 0),
        cmap="BuPu",  # "magma"
    )
    plt.colorbar(contour, ax=ax, label="wind speed (m/s)")
    ax.quiver(
        longitudes[::step],
        latitudes[::step],
        field[0, ::step, ::step],
        field[1, ::step, ::step],
        scale=200,
        headwidth=1.5,
    )
    ax.set_xlabel("Latitude (°)")
    ax.set_ylabel("Longitude (°)")
    # ax.barbs(longitudes, latitudes, field[0], field[1])


def draw_windfield_barbs(ax: Axes, windfield: DAOWindFieldJSON, i=0, step=1):
    field = np.array(windfield["field"][i])
    longitudes = windfield["longitudes"]
    latitudes = windfield["latitudes"]
    contour = ax.contourf(
        longitudes,
        latitudes,
        np.linalg.norm(field, 2, 0),
        cmap="BuPu",  # "magma"
    )
    plt.colorbar(contour)
    ax.barbs(
        longitudes[::step],
        latitudes[::step],
        field[0, ::step, ::step],
        field[1, ::step, ::step],
    )


def draw_windfieldV2_barbs(
    ax: Axes, windfield: DAOWindFieldV2, i=0, level_index=0, step=1
):
    field = np.array(windfield["field"][i])
    field = field[:, level_index]
    longitudes = windfield["longitudes"]
    latitudes = windfield["latitudes"]
    contour = ax.contourf(
        longitudes,
        latitudes,
        np.linalg.norm(field, 2, 0),
        cmap="BuPu",  # "magma"
    )
    plt.colorbar(contour)
    ax.barbs(
        longitudes[::step],
        latitudes[::step],
        field[0, ::step, ::step],
        field[1, ::step, ::step],
    )


def draw_windfieldV2_stream(
    ax: Axes, windfield: DAOWindFieldV2, i=0, level_index=0, step=1
):
    field = np.array(windfield["field"][i])
    field = field[:, level_index]
    longitudes = windfield["longitudes"]
    latitudes = windfield["latitudes"]
    contour = ax.contourf(
        longitudes,
        latitudes,
        np.linalg.norm(field, 2, 0),
        cmap="BuPu",  # "magma"
    )
    # plt.colorbar(contour)

    lat = latitudes[::step]
    f1 = field[0, ::step, ::step]
    f2 = field[1, ::step, ::step]
    if len(lat) > 1 and lat[0] > lat[-1]:
        lat = lat[::-1]
        f1 = np.flip(f1, 0)
        f2 = np.flip(f2, 0)

    strm = ax.streamplot(
        longitudes[::step],
        lat,
        f1,
        f2,
        color="black",  # 流线用纯黑色，与背景色区分
        linewidth=0.8,
        # density=2.0,  # 密度
    )
    set_bound(ax, windfield)
    return contour


def draw_wind_speed_spread(
    ax: Axes,
    windfield: DAOWindFieldV2,
    spread: DAOWindFieldV2,
    i=0,
    level_index=0,
    step=1,
    speed_cmap="BuPu",
    speed_vmin=None,
    speed_vmax=None,
    spread_levels=None,
    sigma_linewidth=1.0,
    sigma_alpha=0.7,
    sigma_labels=True,
    density=1.5,
):
    field = np.array(windfield["field"][i])[:, level_index]
    longitudes = np.asarray(windfield["longitudes"])
    latitudes = np.asarray(windfield["latitudes"])

    speed = np.linalg.norm(field, 2, 0)
    contour = ax.contourf(
        longitudes,
        latitudes,
        speed,
        cmap=speed_cmap,
        vmin=speed_vmin,
        vmax=speed_vmax,
    )

    ax.barbs(
        longitudes[::step],
        latitudes[::step],
        field[0, ::step, ::step],
        field[1, ::step, ::step],
        length=6,
        linewidth=0.8,
        color="0.15",
        barbcolor="0.15",
        pivot="tip",
    )

    spread1 = np.array(spread["field"][i])[:, level_index]
    sigma = np.linalg.norm(spread1, 2, 0)
    longitudes1 = np.asarray(spread["longitudes"])
    latitudes1 = np.asarray(spread["latitudes"])
    if spread_levels is None:
        vmax = np.nanmax(sigma)
        spread_levels = np.linspace(vmax * 0.2, vmax * 0.8, 4)
    cs = ax.contour(
        longitudes1,
        latitudes1,
        sigma,
        levels=spread_levels,
        colors="black",
        linewidths=sigma_linewidth,
        alpha=sigma_alpha,
    )
    if sigma_labels:
        ax.clabel(cs, fmt=r"$\sigma$=%.1f", fontsize=8, colors="black")

    set_bound(ax, windfield)
    return contour, sigma


def draw_wind_spread(
    ax: Axes,
    windfield: DAOWindFieldV2,
    spread: DAOWindFieldV2,
    i=0,
    level_index=0,
    step=1,
    colorbar="BuPr",
):
    field = np.array(windfield["field"][i])
    field = field[:, level_index]
    spread1 = np.array(spread["field"][i])
    spread1 = spread1[:, level_index]

    longitudes = windfield["longitudes"]
    latitudes = windfield["latitudes"]
    longitudes1 = spread["longitudes"]
    latitudes1 = spread["latitudes"]

    contour = ax.contourf(
        longitudes1,
        latitudes1,
        np.linalg.norm(spread1, 2, 0),
        cmap=colorbar,  # "magma"
    )
    # plt.colorbar(contour)
    ax.barbs(
        longitudes[::step],
        latitudes[::step],
        field[0, ::step, ::step],
        field[1, ::step, ::step],
    )
    set_bound(ax, windfield)
    return contour
