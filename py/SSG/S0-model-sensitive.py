import os
import sys
from concurrent.futures import ProcessPoolExecutor, as_completed
from os import path

# 并行绘制时使用无界面后端，避免每个子进程争抢同一个 GUI/显示
os.environ.setdefault("MPLBACKEND", "Agg")

from draw_deployment_select import plot_optimal_strategies_from_file_v4, DrawSelectMode

basePath = "data/SSG"
files = [
    # K_detect 2x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_detect_2x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_detect_2x.json",
    # K_detect 0.5x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_detect_0.5x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_detect_0.5x.json",
    # K_anti 2x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_anti_2x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_anti_2x.json",
    # K_anti 0.5x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_anti_0.5x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_anti_0.5x.json",
    # K_anti_max 2x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_anti_max_2x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_anti_max_2x.json",
    # K_anti_max 0.5x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_anti_max_0.5x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_K_anti_max_0.5x.json",
    # d_anti 2x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_d_anti_2x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_d_anti_2x.json",
    # d_anti 0.5x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_d_anti_0.5x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_d_anti_0.5x.json",
    # dMaxAttacker 2x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_dMaxAttacker_2x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_dMaxAttacker_2x.json",
    # dMaxAttacker 0.5x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_dMaxAttacker_0.5x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_dMaxAttacker_0.5x.json",
    # LambdaPath 2x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_LambdaPath_2x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_LambdaPath_2x.json",
    # LambdaPath 0.5x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_LambdaPath_0.5x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_LambdaPath_0.5x.json",
    # Kappa 2x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_Kappa_2x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_Kappa_2x.json",
    # Kappa 0.5x
    "SSG_Case320260609_NSO3_NSI3_NAI1_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_Kappa_0.5x.json",
    "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_Kappa_0.5x.json",
]

# files = [
#     "SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609_baseline.json"
# ]


def render(file: str) -> str:
    """绘制单个结果文件（在子进程中执行）。"""
    mode = DrawSelectMode(False, best_N_mode=10, has_legend=False, best_N_multi=10)
    plot_optimal_strategies_from_file_v4(
        path.join(basePath, file), mode, prefix="S0-", eps=0, eps_attacker=0
    )
    return file


def main() -> int:
    # 默认并行度：CPU 核数，最多 8（每个任务内部还会画多张图，太多进程抢 IO 反而慢）
    workers = int(sys.argv[1]) if len(sys.argv) > 1 else min(8, os.cpu_count() or 1)
    print(f"Rendering {len(files)} files with {workers} processes ...", flush=True)

    failed = []
    with ProcessPoolExecutor(max_workers=workers) as pool:
        futures = {pool.submit(render, f): f for f in files}
        for n, fut in enumerate(as_completed(futures), 1):
            f = futures[fut]
            try:
                fut.result()
                print(f"[{n}/{len(files)}] done  {f}", flush=True)
            except Exception as exc:  # 单个文件失败不影响其它任务
                failed.append((f, exc))
                print(f"[{n}/{len(files)}] FAIL  {f}: {exc!r}", flush=True)

    if failed:
        print(f"\n{len(failed)} file(s) failed:", flush=True)
        for f, exc in failed:
            print(f"  - {f}: {exc!r}", flush=True)
        return 1

    print("\nAll plots saved.", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
