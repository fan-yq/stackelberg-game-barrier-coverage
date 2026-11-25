"""
反复运行 SSGBarrierSamples2，使用不同的 seed2。

在仓库根目录之外运行也可以：脚本会自动把工作目录设为仓库根目录。
用法：
    python py/SSG/run-seeds.py
    python py/SSG/run-seeds.py --seed2s 20260609 20260610 20260611
    python py/SSG/run-seeds.py --num-runs 20 --seed2-start 20260609 --seed2-step 1
    python py/SSG/run-seeds.py --dry-run
"""

import argparse
import subprocess
import sys
from datetime import datetime
from pathlib import Path

# ==================== 可配置参数 ====================
PROJECT = "BuoyancyAircraft.Samples"
SUBCOMMAND = "SSGBarrierSamples2"

N = 3
D = 6
ITER = 1000
CASE = "Case3"
METHOD = "SSGDE"
CONFIGURATION = "Release"
DEFAULTSEED= 20260515

# 未在命令行指定 --seed2s 时，按下面规则生成 seed2
seed2_START = 20260609
seed2_STEP = 1
NUM_RUNS = 10

# 追加到命令末尾的其它参数，例如 ["--eps", "0.001"]
EXTRA_ARGS = []
# ===================================================

REPO_ROOT = Path(__file__).resolve().parents[2]
LOG_DIR = Path(__file__).resolve().parent / "logs"


def parse_args():
    parser = argparse.ArgumentParser(description="反复运行不同 seed2 的 SSG 实验")
    parser.add_argument("--seed2s", type=int, nargs="+", default=None, help="显式指定 seed2 列表")
    parser.add_argument("--num-runs", type=int, default=NUM_RUNS, help="seed2 数量")
    parser.add_argument("--seed2-start", type=int, default=seed2_START, help="起始 seed2")
    parser.add_argument("--seed2-step", type=int, default=seed2_STEP, help="seed2 步长")
    parser.add_argument("--n", type=int, default=N)
    parser.add_argument("--d", type=int, default=D)
    parser.add_argument("--seed1", type=int, default=DEFAULTSEED)
    parser.add_argument("--iter", type=int, default=ITER)
    parser.add_argument("--case", default=CASE)
    parser.add_argument("--method", default=METHOD)
    parser.add_argument("--configuration", default=CONFIGURATION)
    parser.add_argument("--extra", nargs=argparse.REMAINDER, default=EXTRA_ARGS, help="追加到命令末尾的参数")
    parser.add_argument("--dry-run", action="store_true", help="只打印命令，不执行")
    parser.add_argument("--keep-going", action="store_true", help="某个 seed2 失败后继续运行后续 seed2")
    return parser.parse_args()


def build_command(args, seed2):
    return [
        "dotnet", "run", "-c", args.configuration, "--project", PROJECT, SUBCOMMAND,
        "-n", str(args.n),
        "-d", str(args.d),
        "--iter", str(args.iter),
        "--case", args.case,
        "--method", args.method,
        "--seed2", str(seed2),
        "--seed",str(args.seed1),
        *args.extra,
    ]


def main():
    args = parse_args()

    if args.seed2s:
        seed2s = args.seed2s
    else:
        seed2s = [args.seed2_start + i * args.seed2_step for i in range(args.num_runs)]

    LOG_DIR.mkdir(parents=True, exist_ok=True)
    print(f"仓库根目录: {REPO_ROOT}")
    print(f"共 {len(seed2s)} 个 seed2: {seed2s}")

    failures = []
    for index, seed2 in enumerate(seed2s, start=1):
        command = build_command(args, seed2)
        printable = " ".join(command)
        print(f"\n[{index}/{len(seed2s)}] {printable}")

        if args.dry_run:
            continue

        log_file = LOG_DIR / f"{SUBCOMMAND}_{args.case}_{args.method}_n{args.n}_d{args.d}_iter{args.iter}_s{seed2}.log"
        started = datetime.now()
        with log_file.open("w", encoding="utf-8") as log:
            log.write(printable + "\n\n")
            log.flush()
            result = subprocess.run(
                command,
                cwd=REPO_ROOT,
                stdout=log,
                stderr=subprocess.STDOUT,
            )

        elapsed = (datetime.now() - started).total_seconds()
        if result.returncode != 0:
            failures.append((seed2, result.returncode))
            print(f"    失败 (exit={result.returncode}, {elapsed:.1f}s)，日志: {log_file}")
            if not args.keep_going:
                break
        else:
            print(f"    完成 ({elapsed:.1f}s)，日志: {log_file}")

    if failures:
        print("\n失败的 seed2:")
        for seed2, code in failures:
            print(f"  seed2={seed2}, exit={code}")
        return 1

    print("\n全部完成。" if not args.dry_run else "\ndry-run 结束。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
