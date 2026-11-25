"""
统计同一配置下不同 seed 结果的 MaxMinU / MinMaxU 的 mean 和 std。

输入文件名遵循：
    SSG_{area.Name}_NSO{N_source}_NSI{N_sink}_NAI{N_airship}_NC{N_ControlPoints}_IT{iterations}_EPS2{eps2}_{method}_crowding{isCrowding}_fix{mode}_s{randomSeed}.json

脚本按除 seed 以外的字段分组，每个分组生成一个新的统计文件，文件名沿用同一模板，
其中 seed 部分替换为 seed 范围（如 s20260609-20260610）。

用法：
    python py/SSG/seed-stats.py
    python py/SSG/seed-stats.py --dir data/SSG --out-dir data/SSG/stats
    python py/SSG/seed-stats.py --summary-csv data/SSG/stats/summary.csv
    python py/SSG/seed-stats.py --dry-run
"""

import argparse
import csv
import json
import re
import statistics
import sys
from collections import defaultdict
from pathlib import Path

# ==================== 可配置参数 ====================
INPUT_DIR = "data/SSG"
OUTPUT_DIR = "data/SSG/stats"
PATTERN = "SSG_*.json"
RECURSIVE = False
DDof = 1  # std 的自由度：1=样本标准差，0=总体标准差

MAX_MIN_KEYS = ("MaxMinU", "MaxMin")
MIN_MAX_KEYS = ("MinMaxU", "MinMax")
# ===================================================

REPO_ROOT = Path(__file__).resolve().parents[2]

FILENAME_RE = re.compile(
    r"^SSG_(?P<area>.+?)"
    r"_NSO(?P<n_source>\d+)"
    r"_NSI(?P<n_sink>\d+)"
    r"_NAI(?P<n_airship>\d+)"
    r"_NC(?P<n_control>\d+)"
    r"_IT(?P<iterations>\d+)"
    r"_EPS2(?P<eps2>[^_]+)"
    r"_(?P<method>.+?)"
    r"_crowding(?P<crowding>True|False|true|false)"
    r"(?:_fix(?P<mode>[^_]+))?"
    r"_s(?P<seed>\d+)\.json$"
)

FIELD_ORDER = (
    "area",
    "n_source",
    "n_sink",
    "n_airship",
    "n_control",
    "iterations",
    "eps2",
    "method",
    "crowding",
    "mode",
)


def resolve(path):
    path = Path(path)
    return path if path.is_absolute() else REPO_ROOT / path


def parse_args():
    parser = argparse.ArgumentParser(description="统计不同 seed 结果的 MaxMinU/MinMaxU mean 和 std")
    parser.add_argument("--dir", default=INPUT_DIR, help="输入目录")
    parser.add_argument("--out-dir", default=OUTPUT_DIR, help="输出目录")
    parser.add_argument("--pattern", default=PATTERN, help="文件名 glob 模式")
    parser.add_argument("--recursive", action="store_true", default=RECURSIVE, help="递归搜索子目录")
    parser.add_argument("--ddof", type=int, default=DDof, choices=(0, 1), help="std 自由度（0=总体，1=样本）")
    parser.add_argument("--summary-csv", default=None, help="可选：额外输出汇总 CSV")
    parser.add_argument("--dry-run", action="store_true", help="只打印，不写文件")
    return parser.parse_args()


def get_float(data, keys):
    for key in keys:
        if key in data and data[key] is not None:
            return float(data[key])
    return None


def collect_groups(input_dir, pattern, recursive):
    groups = defaultdict(list)
    paths = input_dir.rglob(pattern) if recursive else input_dir.glob(pattern)
    skipped = []
    for path in sorted(paths):
        match = FILENAME_RE.match(path.name)
        if not match:
            skipped.append(path)
            continue
        info = match.groupdict()
        key = tuple(info[field] for field in FIELD_ORDER)
        groups[key].append((info, path))
    return groups, skipped


def build_output_name(infos):
    info = infos[0]
    seeds = [int(item["seed"]) for item in infos]
    seed_label = str(min(seeds)) if min(seeds) == max(seeds) else f"{min(seeds)}-{max(seeds)}"
    name = (
        f"SSG_{info['area']}"
        f"_NSO{info['n_source']}"
        f"_NSI{info['n_sink']}"
        f"_NAI{info['n_airship']}"
        f"_NC{info['n_control']}"
        f"_IT{info['iterations']}"
        f"_EPS2{info['eps2']}"
        f"_{info['method']}"
        f"_crowding{info['crowding']}"
    )
    if info["mode"]:
        name += f"_fix{info['mode']}"
    return f"{name}_s{seed_label}.json"


def mean_std(values, ddof):
    values = [value for value in values if value is not None]
    if not values:
        return None, None
    mean = statistics.fmean(values)
    if len(values) <= ddof:
        return mean, 0.0
    return mean, statistics.stdev(values) if ddof == 1 else statistics.pstdev(values)


def main():
    args = parse_args()
    input_dir = resolve(args.dir)
    output_dir = resolve(args.out_dir)

    if not input_dir.is_dir():
        print(f"输入目录不存在: {input_dir}")
        return 1

    groups, skipped = collect_groups(input_dir, args.pattern, args.recursive)
    if not groups:
        print(f"在 {input_dir} 中没有找到匹配的文件（pattern={args.pattern}）")
        return 1

    print(f"输入目录: {input_dir}")
    print(f"输出目录: {output_dir}")
    print(f"匹配到 {sum(len(v) for v in groups.values())} 个文件，{len(groups)} 个配置分组")
    if skipped:
        print(f"跳过 {len(skipped)} 个不匹配命名规则的文件")

    output_dir.mkdir(parents=True, exist_ok=True)

    summary_rows = []
    for key, items in sorted(groups.items()):
        infos = [info for info, _ in items]
        values_maxmin = []
        values_minmax = []
        seeds = []
        for info, path in items:
            try:
                with path.open("r", encoding="utf-8") as f:
                    data = json.load(f)
            except Exception as exc:
                print(f"  读取失败，跳过: {path.name} ({exc})")
                continue
            if "MaxMinU_Mean" in data or "MinMaxU_Mean" in data:
                print(f"  跳过已有统计文件: {path.name}")
                continue
            seeds.append(int(info["seed"]))
            values_maxmin.append(get_float(data, MAX_MIN_KEYS))
            values_minmax.append(get_float(data, MIN_MAX_KEYS))

        if not seeds:
            continue

        maxmin_mean, maxmin_std = mean_std(values_maxmin, args.ddof)
        minmax_mean, minmax_std = mean_std(values_minmax, args.ddof)
        output_name = build_output_name(infos)
        output_path = output_dir / output_name

        input_paths = {path.resolve() for _, path in items}
        if output_path.resolve() in input_paths:
            stem = output_name[: -len(".json")]
            seed_pos = stem.rfind("_s")
            output_name = f"{stem[:seed_pos]}_stats{stem[seed_pos:]}.json"
            output_path = output_dir / output_name

        result = {
            "Name": output_name,
            "Area": infos[0]["area"],
            "N_source": int(infos[0]["n_source"]),
            "N_sink": int(infos[0]["n_sink"]),
            "N_airship": int(infos[0]["n_airship"]),
            "N_ControlPoints": int(infos[0]["n_control"]),
            "Iterations": int(infos[0]["iterations"]),
            "Eps2": infos[0]["eps2"],
            "Method": infos[0]["method"],
            "Crowding": infos[0]["crowding"].lower() == "true",
            "Mode": infos[0]["mode"],
            "FileCount": len(seeds),
            "Seeds": sorted(seeds),
            "MaxMinU_Mean": maxmin_mean,
            "MaxMinU_Std": maxmin_std,
            "MinMaxU_Mean": minmax_mean,
            "MinMaxU_Std": minmax_std,
            "Files": sorted(path.name for _, path in items),
        }

        print(
            f"  {output_name}: n={len(seeds)}, "
            f"MaxMinU={maxmin_mean:.6g}±{maxmin_std:.3g}, "
            f"MinMaxU={minmax_mean:.6g}±{minmax_std:.3g}"
        )

        if not args.dry_run:
            with output_path.open("w", encoding="utf-8") as f:
                json.dump(result, f, ensure_ascii=False, indent=2)

        summary_rows.append(
            {
                "Name": output_name,
                "Area": infos[0]["area"],
                "N_source": infos[0]["n_source"],
                "N_sink": infos[0]["n_sink"],
                "N_airship": infos[0]["n_airship"],
                "N_ControlPoints": infos[0]["n_control"],
                "Iterations": infos[0]["iterations"],
                "Eps2": infos[0]["eps2"],
                "Method": infos[0]["method"],
                "Crowding": infos[0]["crowding"],
                "Mode": infos[0]["mode"],
                "FileCount": len(seeds),
                "SeedMin": min(seeds),
                "SeedMax": max(seeds),
                "MaxMinU_Mean": maxmin_mean,
                "MaxMinU_Std": maxmin_std,
                "MinMaxU_Mean": minmax_mean,
                "MinMaxU_Std": minmax_std,
            }
        )

    if args.summary_csv and summary_rows and not args.dry_run:
        summary_path = resolve(args.summary_csv)
        summary_path.parent.mkdir(parents=True, exist_ok=True)
        with summary_path.open("w", encoding="utf-8", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=list(summary_rows[0].keys()))
            writer.writeheader()
            writer.writerows(summary_rows)
        print(f"汇总 CSV: {summary_path}")

    print("dry-run 结束。" if args.dry_run else "统计完成。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
