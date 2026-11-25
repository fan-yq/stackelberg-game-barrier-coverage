import matplotlib.pyplot as plt
import numpy as np
import pandas as pd

# 1. 准备数据
data = {
    "Method": [
        "ACDE", "ACDE", "ACDE",
        "SGCA-DE", "SGCA-DE", "SGCA-DE",
        "SGCA-H", "SGCA-H", "SGCA-H",
    ],
    "nAirship": [3, 6, 12, 3, 6, 12, 3, 6, 12],
    "Mean_ms": [
        1310.6,        # ACDE, 3
        6713.2,        # ACDE, 6
        35171.0,       # ACDE, 12
        2779.6,        # SGCA-DE, 3
        9788.0,        # SGCA-DE, 6
        44262.6,       # SGCA-DE, 12
        2907.4,        # SGCA-H, 3
        11768.4,       # SGCA-H, 6
        54379.4,       # SGCA-H, 12
    ],
    "Error_ms": [
        131.52,        # ACDE, 3
        1069.10,       # ACDE, 6
        428.78,        # ACDE, 12
        29.08,         # SGCA-DE, 3
        410.52,        # SGCA-DE, 6
        225.40,        # SGCA-DE, 12
        27.22,         # SGCA-H, 3
        606.20,        # SGCA-H, 6
        131.60,        # SGCA-H, 12
    ],
}
df = pd.DataFrame(data)

# 转换单位为秒
df["Mean_s"] = df["Mean_ms"] / 1_000
df["Error_s"] = df["Error_ms"] / 1_000

# 2. 绘图配置
methods = sorted(df["Method"].unique())          # ['ACDE', 'SGCA-DE', 'SGCA-H']
n_airships = sorted(df["nAirship"].unique())     # [3, 6, 12]

num_methods = len(methods)
num_groups = len(n_airships)

bar_width = 0.26                                  # 3 组柱子，调窄
x = np.arange(num_methods)

palette = ["#4C72B0", "#55A868", "#C44E52"]       # 3 种颜色对应 L=3/6/12

plt.style.use("seaborn-v0_8")
custom_style = {
    "axes.facecolor": "#F5F5F7",
}
plt.rcParams.update(custom_style)
fig, ax = plt.subplots(figsize=(5.5, 4))

# 3. 绘制柱状图和误差棒
for i, method in enumerate(methods):
    subset = df[df["Method"] == method]

    for j, na in enumerate(n_airships):
        row = subset[subset["nAirship"] == na].iloc[0]
        mean_val = row["Mean_s"]
        err_val = row["Error_s"]

        # 计算 x 轴位置（以方法为中心，左右分布）
        x_pos = x[i] + (j - (num_groups - 1) / 2) * bar_width

        label_text = f"L={na}" if (i == 0) else None

        ax.bar(
            x_pos,
            mean_val,
            width=bar_width,
            yerr=err_val,
            capsize=4,
            color=palette[j],
            edgecolor="black",
            linewidth=1.2,
            label=label_text,
        )

        # --- 在柱子顶部（误差棒之上）添加数值标签 ---
        gap = max(mean_val * 0.02, 0.02)          # 相对间隙，最小 0.02 s
        label_y = mean_val + err_val + gap

        # 数值范围 0.65~27 s，用自适应格式避免小值丢精度
        if mean_val >= 10:
            txt = f"{mean_val:.1f}"
        else:
            txt = f"{mean_val:.2f}"

        ax.text(
            x_pos,
            label_y,
            txt,
            ha="center",
            va="bottom",
            fontsize=8,
        )

# 4. 设置坐标轴和标签
ax.set_ylabel("Time (seconds)", fontsize=12)

ax.set_xticks(x)
ax.set_xticklabels(methods, fontsize=11)

ax.yaxis.set_major_formatter(plt.FuncFormatter(lambda y, _: f"{y:.2f}"))
ax.yaxis.grid(True, linestyle="--", alpha=0.6)
ax.set_axisbelow(True)

# 5. 图例
ax.legend(loc="upper left")

fig.tight_layout()
fig.savefig("data/SSG/figures/05-bench-time.pdf")
plt.show()