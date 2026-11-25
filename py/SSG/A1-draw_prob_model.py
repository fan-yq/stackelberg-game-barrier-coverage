# 画探测模型，确定参数
from matplotlib import pyplot as plt
import numpy as np

k_detect = 2000
k_anti = 50
d_anti = 0.03
k_anti_max = 0.9


def detectModel(d_rad):
    return np.exp(-k_detect * d_rad**2)


def antiModel(d_rad):
    return k_anti_max / (1 + np.exp(-k_anti * (d_rad - d_anti)))


plt.style.use("seaborn-v0_8")
# plt.style.use("fivethirtyeight")
# plt.style.use("bmh")
custom_style = {
    "axes.facecolor": "#F5F5F7",
}
plt.rcParams.update(custom_style)

d1 = np.linspace(0, 5)
detects = detectModel(np.radians(d1))
d2 = np.linspace(0, 5)
antis = antiModel(np.radians(d2))
f, ax = plt.subplots(figsize=(4, 3))
ax.plot(d1, detects, label="$p_d$")
ax.plot(d2, antis, label=r"$p_{\text{anti}}$")
ax.set_xlabel("Angular distance (°)")  # 角距英文
ax.set_ylabel("Probability")
ax.legend()
ax.set_xlim([0, 5])
f.tight_layout()
f.savefig("data/SSG/figures/A02-prob-model.pdf")
plt.show()
