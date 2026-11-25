import json
import matplotlib.pyplot as plt
from datetime import datetime
from typing import List, Dict, Any, Optional

plt.style.use("seaborn-v0_8")
custom_style = {
    "axes.facecolor": "#F5F5F7",
}
plt.rcParams.update(custom_style)
Diverging = [
    "#045275",
    "#089099",
    "#7ccb52",
    "#E99471",
    "#DC3977",
    "#7C1D6F",
    "#FCDE9C",
]

xlim_max = 3000


# 假设 CAInternalInfo 类定义保持不变
class CAInternalInfo:
    def __init__(self, data: Dict[str, Any]):
        self.Note = data.get("Note", "default")
        self.PopSizeDef = data.get("PopSizeDef", 0)
        self.PopSizeAttForEachDef = data.get("PopSizeAttForEachDef", 0)
        self.PopSizeAtt = data.get("PopSizeAtt", 0)
        # 处理 DateTime 字符串
        create_at_str = data.get("CreateAt", "")
        if isinstance(create_at_str, str):
            try:
                self.CreateAt = datetime.fromisoformat(
                    create_at_str.replace("Z", "+00:00")
                )
            except ValueError:
                self.CreateAt = datetime.now()
        else:
            self.CreateAt = create_at_str

        self.MMU = data.get("MMU", [])
        self.ChangeAttCount = data.get("ChangeAttCount", [])
        self.ChangeDefCount = data.get("ChangeDefCount", [])


def calculate_change_proportions(info_objects: List[CAInternalInfo]) -> tuple:
    """
    计算 Change Att 和 Change Def 的比例列表
    返回: (att_ratios_list, def_ratios_list)
    """
    att_ratios_list = []
    def_ratios_list = []

    for info in info_objects:
        # 1. 计算 Change Att 比例
        # 规则：通常除以 PopSizeAtt，如果为0则避免除零错误

        # 2. 计算 Change Def 比例
        # 规则：如果 PopSizeAttForEachDef 为 0，则除以 PopSizeDef
        #      否则除以 PopSizeDef * PopSizeAttForEachDef
        if info.PopSizeAttForEachDef == 0:
            divisor = info.PopSizeAtt
        else:
            divisor = info.PopSizeDef * info.PopSizeAttForEachDef
        current_att_ratio = [c / divisor for c in info.ChangeAttCount]
        att_ratios_list.append(current_att_ratio)

        current_def_ratio = [c / info.PopSizeDef for c in info.ChangeDefCount]
        def_ratios_list.append(current_def_ratio)

    return att_ratios_list, def_ratios_list


def moving_average(data: List[float], window_size: int) -> List[float]:
    """
    计算移动平均线
    """
    if len(data) < window_size:
        return data

    result = []
    # 使用简单移动平均
    for i in range(len(data)):
        start_idx = max(0, i - window_size + 1)
        window = data[start_idx : i + 1]
        avg = sum(window) / len(window)
        result.append(avg)
    return result


def plot_mmu(info_objects: List[CAInternalInfo], window_size: int = 1):
    """
    独立绘制 MMU 曲线
    :param info_objects: 数据对象列表
    :param window_size: 滑动窗口大小，1表示无平滑
    """
    if not info_objects:
        print("No data to plot MMU.")
        return

    fig, ax = plt.subplots(figsize=(5, 3))

    # colors = ["#e6b9aa", "#a4c2f4", "#d9ead3", "#ffcccb", "#ffe5cc"]
    colors = Diverging
    zorder = range(1, len(info_objects) + 1)
    zorder = list(reversed(zorder))

    for i, info in enumerate(info_objects):
        raw_mmu = info.MMU
        label = f"{info.Note}"

        # 应用滑动窗口平均
        smoothed_mmu = moving_average(raw_mmu, window_size)

        x_axis = list(range(len(smoothed_mmu)))

        # 绘制平滑线
        ax.plot(
            x_axis,
            smoothed_mmu,
            label=f"{label}",
            # linestyle="-" if window_size == 1 else "--",
            color=colors[i % len(colors)],
            zorder=zorder[i],
            alpha=0.8,
        )

        # 如果开启了平滑，可以选择性绘制原始数据作为背景参考（可选，这里为了清晰暂不画）
        # if window_size > 1:
        #     ax.plot(x_axis, raw_mmu[:len(x_axis)], 'o', markersize=2, color=colors[i % len(colors)], alpha=0.2)

    # ax.set_title(f"MMU Curve Visualization (Window Size: {window_size})")
    ax.set_xlabel("Step Index")
    ax.set_ylabel("Max Min $U_d$")
    ax.legend(loc="lower right")
    # ax.legend(loc="best")
    ax.set_xlim(0, min(xlim_max, len(smoothed_mmu)))
    # ax.grid(True, linestyle="--", alpha=0.6)

    plt.tight_layout()
    return fig, ax


def plot_att_ratio(info_objects: List[CAInternalInfo], window_size):
    """
    独立绘制 Att Ratio 曲线
    """
    if not info_objects:
        print("No data to plot Att Ratio.")
        return

    att_ratios_list, _ = calculate_change_proportions(info_objects)

    fig, ax = plt.subplots(figsize=(5, 3))
    colors = Diverging

    for i, ratios in enumerate(att_ratios_list):
        if len(ratios) == 0:
            continue
        print(f"att ave {i}:", sum(ratios) / len(ratios))

        x_axis = list(range(len(ratios)))
        ratios = moving_average(ratios, window_size)
        label = f"{info_objects[i].Note}"

        ax.plot(
            x_axis,
            ratios,
            label=label,
            linestyle="-",
            color=colors[i % len(colors)],
            alpha=0.8,
        )

    ax.set_xlabel("Step Index")
    ax.set_ylabel("Proportion")
    # ax.legend(loc="best")
    ax.legend(loc="upper right")
    ax.set_xlim(0, min(xlim_max, len(ratios)))

    plt.tight_layout()
    return fig, ax


def plot_def_ratio(info_objects: List[CAInternalInfo], window_size):
    """
    独立绘制 Def Ratio 曲线
    包含特殊逻辑：根据 PopSizeAttForEachDef 决定分母
    """
    if not info_objects:
        print("No data to plot Def Ratio.")
        return

    _, def_ratios_list = calculate_change_proportions(info_objects)

    fig, ax = plt.subplots(figsize=(5, 3))
    colors = Diverging

    for i, ratios in enumerate(def_ratios_list):
        if len(ratios) == 0:
            continue
        print(f"def ave {i}:", sum(ratios) / len(ratios))

        x_axis = list(range(len(ratios)))
        ratios = moving_average(ratios, window_size)
        info = info_objects[i]
        # 在标题或标签中注明计算逻辑，方便调试
        logic_note = (
            "Div by PopSizeDef"
            if info.PopSizeAttForEachDef == 0
            else f"Div by PopSizeDef*{info.PopSizeAttForEachDef}"
        )
        label = f"Run {i+1}: {info.Note} ({logic_note})"
        label = info.Note

        ax.plot(
            x_axis,
            ratios,
            label=label,
            color=colors[i % len(colors)],
            alpha=0.8,
        )

    # ax.set_title("Operator Change Proportion: Def Ratio")
    ax.set_xlabel("Step Index")
    ax.set_ylabel("Proportion")
    ax.legend(loc="upper right")
    ax.set_xlim(0, min(xlim_max, len(ratios)))

    plt.tight_layout()
    return fig, ax


def plot_ca_internal_info(
    json_data_path: str = None, data_list: List[Dict] = None, window_size: int = 3
):
    """
    主入口函数：加载数据并调用三个独立的绘图函数
    :param json_data_path: JSON 文件路径
    :param data_list: 直接传入的字典列表
    :param window_size: MMU 滑动窗口大小
    """
    # 1. 准备数据
    if data_list is None and json_data_path:
        with open(json_data_path, "r", encoding="utf-8") as f:
            raw_data = json.load(f)

        if isinstance(raw_data, list):
            records = raw_data
        elif isinstance(raw_data, dict) and "data" in raw_data:
            records = raw_data["data"]
        else:
            records = [raw_data]

        info_objects = [CAInternalInfo(r) for r in records]
    elif data_list is not None:
        info_objects = [CAInternalInfo(d) for d in data_list]
    else:
        print("未提供数据")

    if not info_objects:
        print("没有可处理的数据。")
        return

    print(f"开始绘制 {len(info_objects)} 条记录的数据...")

    # 2. 分别绘制三个图
    print("正在绘制 MMU 曲线...")
    f, _ = plot_mmu(info_objects, window_size=window_size)
    f.savefig("data/SSG/figures/06-mmu.pdf")

    print("正在绘制 Att Ratio 曲线...")
    f, _ = plot_att_ratio(info_objects, window_size)
    f.savefig("data/SSG/figures/06-att-ratio.pdf")

    print("正在绘制 Def Ratio 曲线...")
    f, _ = plot_def_ratio(info_objects, window_size)
    f.savefig("data/SSG/figures/06-def-ratio.pdf")


# 执行绘图
if __name__ == "__main__":
    # 如果你有真实文件，取消下面注释并替换路径
    # plot_ca_internal_info(json_data_path="your_file.json")

    # 否则运行模拟数据
    json_data_path = "data/SSG/internal"

    files = [
        "SGDE-6fc17ff1-d39f-4800-8a4f-365655a6523f.json",
        "SGDE-ab95123f-512b-4234-b753-e08bb3898892.json",
        "SGDE-07050df5-57a7-4874-9f24-7cd3f83390f9.json",
        "ACGA-7041a496-ad26-44df-b656-af762963dd29.json",
    ]

    labels = ["SGCA-H", "SGCA-H-W", "SGCA-DE", "ACDE"]
    files1 = [json.load(open(f"{json_data_path}/{file}")) for file in files]
    for f, label in zip(files1, labels):
        f["Note"] = label
    plot_ca_internal_info(data_list=files1, window_size=10)
    plt.show()
