import json
import os
import glob
import pandas as pd
import numpy as np
from pathlib import Path


def load_summary_files(directory="results"):
    """
    Load only summary JSON files from the specified directory
    """
    results = []
    summary_files = glob.glob(os.path.join(directory, "summary_*.json"))

    if not summary_files:
        print(f"No summary files found in {directory}")
        return results

    for file in summary_files:
        try:
            with open(file, "r") as f:
                data = json.load(f)
                if isinstance(data, list):
                    results.extend(data)
                else:
                    results.append(data)
                print(f"Loaded: {os.path.basename(file)}")
        except Exception as e:
            print(f"Error reading {file}: {e}")

    return results


def load_single_sumary_file(file):
    results = json.load(open(file))
    return results


def create_dataframe(results):
    """
    Create a pandas DataFrame from the results
    """
    rows = []
    for r in results:
        row = {
            "problem": r.get("ProblemName", "unknown"),
            "method": r.get("MethodName", "unknown"),
            "iterations": r.get("Iterations", 0),
            "repetitions": r.get("Repetitions", 0),
            "mean_mse": r.get("MeanMSE", None),
            "median_mse": r.get("MedianMSE", None),
            "std_mse": r.get("StdMSE", None),
            "max_min": np.mean(r.get("MaxMin", None)),
            "min_max": np.mean(r.get("MinMax", None)),
            "mse_value": r.get("MSEValue", None),
            "optimal_value": r.get("OptimalValue", None),
        }
        rows.append(row)

    return pd.DataFrame(rows)


def analyze_results(df):
    """
    Perform analysis on the results
    """
    print("=" * 80)
    print("MSE RESULTS ANALYSIS (from summary files)")
    print("=" * 80)

    print(f"\nTotal runs: {len(df)}")
    print(f"Problems: {df['problem'].unique().tolist()}")
    print(f"Methods: {df['method'].unique().tolist()}")

    print("\n" + "-" * 80)
    print("MEAN MSE BY PROBLEM AND METHOD")
    print("-" * 80)

    pivot_mean = df.pivot_table(
        index="problem", columns="method", values="mean_mse", aggfunc="mean"
    )
    print(pivot_mean.round(6))

    print("\n" + "-" * 80)
    print("STD MSE BY PROBLEM AND METHOD")
    print("-" * 80)

    pivot_std = df.pivot_table(
        index="problem", columns="method", values="std_mse", aggfunc="mean"
    )
    print(pivot_std.round(6))

    print("\n" + "-" * 80)
    print("BEST METHOD PER PROBLEM (lowest mean MSE)")
    print("-" * 80)

    for problem in df["problem"].unique():
        problem_data = df[df["problem"] == problem]
        best_row = problem_data.loc[problem_data["mean_mse"].idxmin()]
        print(
            f"{problem:10s} -> {best_row['method']:15s} (MSE: {best_row['mean_mse']:.6f})"
        )

    print("\n" + "-" * 80)
    print("OVERALL METHOD RANKING (average mean MSE across all problems)")
    print("-" * 80)

    method_avg = df.groupby("method")["mean_mse"].mean().sort_values()
    for method, mse in method_avg.items():
        print(f"{method:20s} : {mse:.6f}")

    print(
        f"\nBest overall method: {method_avg.index[0]} (MSE: {method_avg.values[0]:.6f})"
    )

    return pivot_mean, pivot_std


def save_summary_stats(df, output_dir="results"):
    """
    Save summary statistics to CSV
    """
    # Summary by problem and method
    summary = (
        df.groupby(["problem", "method"])
        .agg(
            {
                "mean_mse": ["mean", "std", "min", "max"],
                "median_mse": ["mean"],
                "std_mse": ["mean"],
                "repetitions": ["mean"],
            }
        )
        .round(6)
    )
    summary.columns = ["_".join(col).strip() for col in summary.columns.values]
    summary.to_csv(os.path.join(output_dir, "mse_summary_analysis.csv"))

    # Method ranking
    method_ranking = (
        df.groupby("method")["mean_mse"].agg(["mean", "std", "min", "max"]).round(6)
    )
    method_ranking = method_ranking.sort_values("mean")
    method_ranking.to_csv(os.path.join(output_dir, "method_ranking.csv"))

    print(f"\nCSV files saved to {output_dir}/")


def get_optimal_value(df, problem):
    """
    Return the problem-level optimal value (a single value shared by all methods)
    """
    vals = df.loc[df["problem"] == problem, "optimal_value"].values
    for v in vals:
        if v is not None and not pd.isna(v):
            return v
    return None


def print_markdown_table(df, value_col, title, has_optimal=True):
    """
    Print a problem x method markdown table for the given column,
    with the problem-level optimal value as an extra column
    """
    methods = df["method"].unique()

    print("\n" + "=" * 80)
    print(title)
    print("=" * 80)
    if has_optimal:
        header = "| Problem | " + " | ".join(methods) + " | Optimal |"
    else:
        header = "| Problem | " + " | ".join(methods)

    separator = "|---------|" + "|".join(["---------"] * (len(methods) + 1)) + "|"
    print("\n" + header)
    print(separator)
    for problem in df["problem"].unique():
        row = f"| {problem} "
        for method in methods:
            val = df[(df["problem"] == problem) & (df["method"] == method)][
                value_col
            ].values
            if len(val) > 0 and val[0] is not None and not pd.isna(val[0]):
                row += f"| {val[0]:.6f} "
            else:
                row += "| N/A "
        optimal = get_optimal_value(df, problem)
        if has_optimal:
            if optimal is not None:
                row += f"| {optimal:.6f} |"
            else:
                row += "| N/A |"
        else:
            row += "|"
        print(row)


def main():
    """
    Main execution function
    """
    # Load summary files
    # results = load_summary_files("data/SSG")
    # results = load_single_sumary_file("data/SSG/summary_20260616_212431.json")
    # results = load_single_sumary_file("data/SSG/summary_20260630_162014.json")
    # results = load_single_sumary_file("data/SSG/summary_20261110_163006.json")
    # results = load_single_sumary_file("data/SSG/summary_20261114_220300.json")
    # results = load_single_sumary_file("data/SSG/summary_20261114_223449.json")
    results = load_single_sumary_file("data/SSG/summary_20261117_161856.json")

    if not results:
        print("No summary results found!")
        return

    # Create DataFrame
    df = create_dataframe(results)
    print(f"\nCreated DataFrame with {len(df)} rows\n")

    # Analyze
    pivot_mean, pivot_std = analyze_results(df)

    # Save to CSV
    # save_summary_stats(df, "results")

    # Print summary tables as markdown
    print_markdown_table(df, "mean_mse", "MARKDOWN TABLE (mean_mse)", False)
    print_markdown_table(df, "max_min", "MARKDOWN TABLE (max_min)")

    print("\nAnalysis complete!")


if __name__ == "__main__":
    # Check for required packages
    try:
        import pandas as pd
        import numpy as np
    except ImportError:
        print("Installing required packages...")
        import subprocess

        subprocess.check_call(["pip", "install", "pandas", "numpy"])
        import pandas as pd
        import numpy as np

    main()
