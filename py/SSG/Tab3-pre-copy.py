#!/usr/bin/env python3
"""
把 src 下匹配 pattern 的文件，按 name/mtime/size/input 顺序复制到 dst 子目录，
文件名加 0001_ 前缀以在文件系统中保留顺序。
用法:
    python copy_ordered.py src dst -p "*.json" --order name
"""

import argparse
import shutil
from pathlib import Path


def collect(src: Path, pattern: str) -> list[Path]:
    files = [p for p in src.glob(pattern) if p.is_file()]
    return files


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src", type=Path, help="源目录")
    ap.add_argument("dst", type=Path, help="目标子目录（不存在会创建）")
    ap.add_argument("-p", "--pattern", default="*", help="glob 模式，默认 *")

    ap.add_argument(
        "--prefix",
        default=True,
        action=argparse.BooleanOptionalAction,
        help="是否加 0001_ 序号前缀，默认加",
    )
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    files = collect(args.src, args.pattern)
    if not files:
        print(f"没有匹配到文件: {args.src}/{args.pattern}")
        return

    args.dst.mkdir(parents=True, exist_ok=True)
    width = max(4, len(str(len(files))))

    for i, f in enumerate(files, 1):
        name = f"{i:0{width}d}_{f.name}" if args.prefix else f.name
        target = args.dst / name
        print(f"{f}  ->  {target}")
        if not args.dry_run:
            shutil.copy2(f, target)

    print(f"完成，共 {len(files)} 个文件 -> {args.dst}")


if __name__ == "__main__":
    main()
