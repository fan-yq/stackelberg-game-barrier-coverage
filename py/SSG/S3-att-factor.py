import os
import sys
from concurrent.futures import ProcessPoolExecutor, as_completed
from os import path

# 并行绘制时使用无界面后端，避免每个子进程争抢同一个 GUI/显示
os.environ.setdefault("MPLBACKEND", "Agg")

from draw_deployment_select import plot_optimal_strategies_from_file_v4, DrawSelectMode

basePath = "data/SSG"
files = [
    # attacker population factor: 0.5x (8 -> 4)
    "SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515_att-factor_0.5x.json",
    # attacker population factor: 1x (8 -> 8, baseline)
    "SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515_att-factor_1x.json",
    # attacker population factor: 2x (8 -> 16)
    "SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515_att-factor_2x.json",
]


def render(file: str) -> str:
    """绘制单个结果文件（在子进程中执行）。"""
    mode = DrawSelectMode(False, best_N_mode=10, has_legend=True, best_N_multi=10)
    plot_optimal_strategies_from_file_v4(
        path.join(basePath, file), mode, prefix="S3-", eps=0.001, eps_attacker=0.01
    )
    return file


def main() -> int:
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
