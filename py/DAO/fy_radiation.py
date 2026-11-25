from enum import Enum
from matplotlib.axes import Axes
import matplotlib.pyplot as plt
import cartopy.crs as ccrs
import numpy as np
import netCDF4 as nc
from cartopy.mpl.gridliner import LONGITUDE_FORMATTER, LATITUDE_FORMATTER
import matplotlib as mpl

file_path = "C:\\Users\\fyq\\Documents\\data\\fy-radiation\\FY4B-_DISK_1050E_GEO_NOM_LUT_20240227000000_4000M_V0001.raw"


def read_lut_file(file_path, width=2748, height=2748):
    # 打开文件并读取为字节流
    with open(file_path, "rb") as f:
        # 读取整个文件内容
        data = np.fromfile(f, dtype="<f8", count=width * height * 2)  # 小端double类型

    # 转换为二维数组，纬度和经度交替存储
    lat = data[0::2].reshape((height, width))
    lon = data[1::2].reshape((height, width))
    latitudes: np.ndarray = np.ma.masked_where(lat == 999999.9999, lat)
    longitudes: np.ndarray = np.ma.masked_where(lon == 999999.9999, lon)

    return latitudes, longitudes


latitudes, longitudes = read_lut_file(file_path)
LatLonAbsence = 999999.9999


def calculate_distance(lat1, lon1, lat2, lon2):
    """计算两点之间的平方距离"""
    return (lat1 - lat2) ** 2 + (lon1 - lon2) ** 2


def find_nearest_index(plat, plon, x=None, y=None):
    """
    在给定的纬度和经度数组中，查找最接近p点的坐标索引。

    参数:
    - p: 包含 'X' 和 'Y' 键的对象或字典，代表目标点的纬度和经度。
    - latitudes: 二维numpy数组，表示纬度数据。
    - longitudes: 二维numpy数组，表示经度数据。

    返回:
    - (x, y) 元组，代表最近点的索引。
    """
    height, width = latitudes.shape

    # 从中心点开始搜索
    if x is None:
        x = height // 2
        y = width // 2

    while True:
        current_distance = calculate_distance(
            plat, plon, latitudes[x, y], longitudes[x, y]
        )

        # 初始化下一个位置为当前位置
        next_x, next_y = x, y

        # 探索四周
        for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
            nx, ny = x + dx, y + dy

            # 检查是否在边界内
            if 0 <= nx < height and 0 <= ny < width:
                new_distance = calculate_distance(
                    plat, plon, latitudes[nx, ny], longitudes[nx, ny]
                )

                if new_distance < current_distance:
                    current_distance = new_distance
                    next_x, next_y = nx, ny

        # 如果没有找到更近的点，则停止循环
        if next_x == x and next_y == y:
            break
        else:
            x, y = next_x, next_y

    return x, y


class RadiationType(Enum):
    OLR = "OLR"
    RSR = "RSR"
    CFR = "CFR"
    Default = "Default"


varMap = {
    RadiationType.OLR: "OLR (W/m$^2$)",
    RadiationType.RSR: "RSR (W/m$^2$)",
    RadiationType.CFR: "Cloud fraction",
}


def draw_cartopy(ax: Axes):

    # 添加海岸线
    ax.coastlines()

    gl = ax.gridlines(
        # crs=ccrs.PlateCarree(),
        draw_labels=True,
        # linewidth=2,
        color="k",
        alpha=0.5,
        # linestyle="--",
    )

    # 设置经度和纬度的格式化器
    gl.xformatter = LONGITUDE_FORMATTER
    gl.yformatter = LATITUDE_FORMATTER

    # 显示经度和纬度标签
    gl.top_labels = False  # 不在顶部显示经度标签
    gl.right_labels = False  # 不在右侧显示纬度标签
    gl.xlabel_style = {"size": 10, "color": "black"}  # 设置经度标签样式
    gl.ylabel_style = {"size": 10, "color": "black"}  # 设置纬度标签样式
    return gl


def draw_radiation(
    fileName,
    variable: RadiationType,
    N=20,
    cmap=None,
    vmax1=None,
    extents=None,
    usePlate=False,
    shrink=1,
    figsize=(10, 5),
):
    # 加载数据
    ncfile = nc.Dataset(fileName)
    proj_geo = ccrs.Geostationary(central_longitude=105)
    proj_ortho = ccrs.Orthographic(central_longitude=105)
    plate = ccrs.PlateCarree(180)
    projection = plate if usePlate else proj_geo

    if cmap is None:
        match variable:
            case RadiationType.OLR:
                cmap = "Spectral"
            case RadiationType.RSR:
                cmap = "coolwarm"
            case RadiationType.CFR:
                cmap = "Blues_r"

    # 创建图形和坐标轴
    fig, ax = plt.subplots(
        subplot_kw={"projection": projection},
        figsize=figsize,
    )
    ax: Axes = ax

    draw_cartopy(ax)
    if extents:
        ax.set_extent(extents, ccrs.PlateCarree())

    # 处理数据
    data = np.array(ncfile.variables[variable.name])
    match variable:
        case RadiationType.OLR:
            # data[data == 32766] = np.nan
            data = np.ma.masked_where(data == 32766, data)
            data = np.ma.masked_where(data == 0, data)
        case RadiationType.RSR:
            # 65532:night,65535:space
            # data[data == 65532] = 0
            # data[data == 65535] = np.nan
            data = np.ma.masked_where(data == 65535, data)
            data = np.ma.masked_where(data == 65532, data)
        case RadiationType.CFR:
            data = np.ma.masked_where(data == 65535, data)
            data = np.ma.masked_where(data == -1, data)

    cmap = mpl.colormaps.get_cmap(cmap).copy()
    # cmap.set_bad(color="white")
    # 绘制二维图
    vmin = np.nanmin(data)
    vmax = np.nanmax(data)
    print(vmin, vmax)

    if vmax1 is not None:
        vmax = vmax1
    cp = ax.pcolormesh(
        longitudes[::N, ::N].T,
        latitudes[::N, ::N].T,
        data[::N, ::N].T,
        cmap=cmap,
        vmin=vmin,
        vmax=vmax,
        transform=ccrs.PlateCarree(),
    )

    # 添加颜色条
    if vmax1 is not None:
        plt.colorbar(cp, ax=ax, label=varMap[variable], extend="max", shrink=shrink)
    else:
        plt.colorbar(cp, ax=ax, label=varMap[variable], shrink=shrink)

    # 设置标题
    # ax.set_title("OLR Distribution")
    return fig, ax


def draw_fy_data(
    ax: Axes, data, N, vmin, vmax, cmap="viridis", mask=None, label=None, shrink=1
):
    cmap = mpl.colormaps.get_cmap(cmap).copy()
    # cmap.set_bad(color="white")
    latitudes1 = latitudes
    longitudes1 = longitudes
    if mask is not None:
        latitudes1 = np.ma.masked_where(mask, latitudes)
        longitudes1 = np.ma.masked_where(mask, longitudes)
    # 绘制二维图
    cp = ax.pcolormesh(
        longitudes1[::N, ::N].T,
        latitudes1[::N, ::N].T,
        data[::N, ::N].T,
        cmap=cmap,
        vmin=vmin,
        vmax=vmax,
        transform=ccrs.PlateCarree(),
    )
    # 添加颜色条
    if shrink != 0:
        plt.colorbar(cp, ax=ax, label=label, shrink=shrink)

    return cp


if __name__ == "__main__":
    OLRFile = r"C:\Users\fyq\Documents\data\fy-radiation\20250324\FY4B-_AGRI--_N_DISK_1050E_L2-_OLR-_MULT_NOM_20250324090000_20250324091459_4000M_V0001.NC"
    RSRFile = r"C:\Users\fyq\Documents\data\fy-radiation\20250324\FY4B-_AGRI--_N_DISK_1050E_L2-_RSR-_MULT_NOM_20250324090000_20250324091459_4000M_V0001.NC"
    path = "C:\\Users\\fyq\\Documents\\data\\fy-radiation\\202502\\"
    dataType = RadiationType.OLR
    date = "20250220"
    hours = ["00", "06", "12", "18"]
    # hours = ["03"]
    OLRFiles = []
    for hour in hours:
        OLRFiles.append(
            f"FY4B-_AGRI--_N_DISK_1050E_L2-_{dataType.name}-_MULT_NOM_{date}{hour}0000_{date}{hour}1459_4000M_V0001.NC"
        )
    # f = draw_radiation(OLRFile, "OLR")
    # f = draw_radiation(RSRFile, RadiationType.RSR)

    for file in OLRFiles:
        draw_radiation(path + file, dataType, 4)
    plt.show()

    # 画网格的分布图
    # N = 50
    # plt.scatter(longitudes[::N], latitudes[::N])
    # plt.show()
