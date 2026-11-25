# SSG — Stackelberg Security Game for Barrier Coverage

[![License: MPL 2.0](https://img.shields.io/badge/license-MPL--2.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4.svg)](https://dotnet.microsoft.com/)

> **Status:** this repository accompanies a manuscript that is currently under
> review. The code is released under MPL-2.0; citation details will be added
> once the review is complete.

## Overview

Stratospheric airships provide persistent surveillance against low-altitude
aerial threats, but barrier-coverage methods for them are mostly geometry-based
and ignore strategic interaction, while Stackelberg security games are usually
discrete and cannot handle continuous deployment.

This repository implements a continuous barrier-coverage game for that setting.
The defender (leader) commits to airship deployments and patrol paths between
launch sites and targets; the attacker (follower) responds with the trajectory
that maximizes its payoff, a detection probability derived from path geometry.
The solver, **SGCA**, is a minimax coevolutionary algorithm with a one-to-many
defender–attacker representation: it maintains several attacker populations per
defender solution, uses a worst-case oriented update rule and heuristic
operators, and returns the defender's optimal mixed strategy with its
equilibrium value. The formal model and the experimental protocol are described
in the accompanying manuscript.

## Paper-to-code map

| Component in the paper | Where it lives in this repository |
| --- | --- |
| Proposed method, **SGCA** | [`Airship/SSGBarrier/SGDE`](Airship/SSGBarrier/SGDE) |
| Baseline, **ACGA** | [`Airship/SSGBarrier/ACGA`](Airship/SSGBarrier/ACGA) |
| Baseline, **MMDE** | [`Airship/SSGBarrier/MMDE`](Airship/SSGBarrier/MMDE) |
| Utility evaluation | [`Airship/SSGBarrier/SSGBarrierUtility.cs`](Airship/SSGBarrier/SSGBarrierUtility.cs) |

## Naming: paper vs. code

The identifiers in the code predate the names used in the paper, so they do not
match:

| Name in the paper | Name in the code |
| --- | --- |
| SGCA-H | `SSGDE` |
| SGCA-DE | `SSGDEPureDE` |
| AttackerOracle | `AttackerOracle2` |

`SGCA-DE` is not a separate class: it is the same solver as `SGCA-H` with the
differential-evolution operators selected on both sides (no heuristic operators).
The ablations `SSGDEAttH`, `SSGDEDefH`, `SSGDEPabestRandHalf` and
`SSGDENoAttUpdate` follow the same pattern and correspond to variants of
`SGCA-H` discussed in the paper.

## What is not open-sourced

Two baselines used in the paper are **not** included in this repository, because
they are used in another paper:

- `PredefinedLine` — the predefined-line deployment baseline;
- `AttackerOracle` — the exact attacker oracle (`AttackerOracle2` in the code).

Consequently these entry points are absent from the command line. The enum
member `AttackerOracle2` is still declared in
[`SSGBarrierMethod.cs`](BuoyancyAircraft.Samples/Airship.SSGBarrier/SSGBarrierMethod.cs)
but has no implementation behind it, so it cannot be selected with `--method`.
Results that depend on these two baselines cannot be reproduced from this
repository alone.

## Requirements

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) or later.
- A platform supported by .NET 10 (Windows, Linux, or macOS).

NuGet package versions are managed centrally in
[`Directory.Packages.props`](Directory.Packages.props); no manual restore
configuration is needed.

## Build

```bash
git clone https://github.com/<your-account>/stackelberg-game-barrier-coverage.git
cd stackelberg-game-barrier-coverage
dotnet build BuoyancyAircraft.Samples/BuoyancyAircraft.Samples.csproj -c Release
```

The solution file [`SSG.slnx`](SSG.slnx) contains the same two projects and can
be built with `dotnet build SSG.slnx -c Release`. Building the executable
project directly pulls in the `Airship` library as a project reference.

## Configuration

Result files are written below the directory configured by the `OutputPath`
setting. It is read from a `.env` file (searched upwards from the working
directory):

```dotenv
OutputPath=data
```

Generated result files are placed in `<OutputPath>/SSG/`. Create the directory
if it does not exist.

## Usage

The executable project is `BuoyancyAircraft.Samples`; its root command is
`samples`, and every experiment is one of its subcommands. Run the commands from
the repository root so that the `.env` file is discovered. The exact command
lines used for the figures and tables of the paper are collected in
[`docs/Commands.md`](docs/Commands.md).

Solve a scenario and write the result to `<OutputPath>/SSG/`:

```bash
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGBarrierSamples2 \
  -n 3 -d 6 -k 3 -s 3 --iter 1000 --case Case3 --method SSGDE --seed 20260609
```

On Windows PowerShell the command is written on a single line.

### `SSGBarrierSamples2` options

| Option | Default | Meaning |
| --- | --- | --- |
| `--case` | `Simple` | scenario name (see [Scenarios](#scenarios)) |
| `-d`, `--n-airship` | `5` | number of airships in the barrier |
| `-n`, `--n-control` | `3` | number of control points per airship path |
| `-s`, `--n-source` | `3` | number of launch sites |
| `-k`, `--n-sink` | `3` | number of targets |
| `--iter` | `1000` | number of iterations of the solver |
| `--method` | `ACDE` | solver, see [Solvers](#solvers) |
| `--seed` | `20260515` | seed for the launch site / target distribution |
| `--seed2` | same as `--seed` | seed for the solver population |
| `--eps2` | `0.001` | tolerance used when constructing the utility matrix |
| `--crowding` | `false` | enable crowding in the evolutionary solvers |
| `--point-fix-mode` | `Proj` | how infeasible points are repaired: `Proj` or `Resample` |
| `--att-factor` | `8` | attacker population size = factor × (airships + 1) |
| `--def-factor` | `10` | defender population size = factor × airships |
| `--model-file` | – | JSON file with an `SSGBarrierSetting` that overrides the model constants |
| `--suffix` | – | suffix appended to the result file name |

The result file is named

```text
SSG_<Case><seed>_NSO<n-source>_NSI<n-sink>_NAI<n-airship>_NC<n-control>_IT<iter>_EPS2<eps2>_<method>_crowding<bool>_fix<mode>_s<seed2>[_<suffix>].json
```

### Other commands

| Command | Purpose |
| --- | --- |
| `SSGBarrierSamples2` | generate a scenario, solve it, and write a result file |
| `SSGCrossCompare` | load result files (paths or globs), cross-evaluate the utility matrices, and write a comparison file |
| `SSGPrintLaunchSitesTargets` | print the coordinates of the launch sites and targets of a scenario |
| `SSGProbSurvWithS` | for a stored result, print the defender's utility along a chosen attacker path as a function of the path progress `s` |
| `MinimaxBenchSamples` | minimax benchmark problems comparing solver variants |
| `SSGBarrierBenchmark` | timing benchmark (BenchmarkDotNet) |

Examples:

```bash
# print the geometry of a scenario
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPrintLaunchSitesTargets -k 3 -s 3 --case HullArea4 --seed 20260609

# compare all result files of one scenario
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare "data/SSG/SSG_Case3*.json" --only-best=true --eps=0.05

# utility along a stored attacker path
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/<result>.json --def-index 48 --att-index 901
```

## Scenarios

| Case | Region |
| --- | --- |
| `Simple` | small rectangle, single launch area and target area |
| `Case2` | rectangle, attacker region slightly larger than the defender region |
| `Case3` | rectangle, attacker region extends beyond the defender region |
| `Case3TargetSparse` | as `Case3`, two small and separated target areas |
| `Case3BothSparse` | as `Case3`, separated launch and target areas |
| `HullArea4` | irregular convex region, three scattered launch areas |
| `HullArea5` | irregular convex region, one compact launch area |
| `LargeArea` | large irregular region, three launch areas and three target areas |
| `LargeAreaV` | `LargeArea` with heterogeneous target values |
| `Triangle`, `Pentagon`, `LongCorridor`, `MultiSourceMultiSink`, `CircleApprox`, `RandomConvex` | additional convex geometries (single or multiple sources and sinks) |

`-s` and `-k` must be compatible with the scenario: some scenarios require them
to be multiples of the number of launch/target areas.

## Solvers

`--method` accepts one of the following (see
[`SSGBarrierMethod.cs`](BuoyancyAircraft.Samples/Airship.SSGBarrier/SSGBarrierMethod.cs)):

| Method | Paper name | Description |
| --- | --- | --- |
| `SSGDE` | SGCA-H | co-evolutionary differential evolution for the Stackelberg game, main method |
| `SSGDEPureDE` | SGCA-DE | `SGCA-H` with differential evolution operators only |
| `SSGDEAttH`, `SSGDEDefH` | – | `SGCA-H` with heuristic operators on one side only |
| `SSGDEPabestRandHalf` | – | `SGCA-H` with half of the population using p-best/rand mutation |
| `SSGDENoAttUpdate` | – | `SGCA-H` without updating the incumbent attacker |
| `ACDE` | ACGA | alternating co-evolutionary differential evolution |
| `MMDE` | MMDE | min–max differential evolution |

`AttackerOracle2` is declared but not implemented in this release; see
[What is not open-sourced](#what-is-not-open-sourced).

## Reproducibility notes

- Runs are reproducible from the seeds recorded in the result file name: `--seed`
  fixes the scenario geometry and `--seed2` fixes the solver initialization.
- `py/SSG/run-seeds.py` runs a scenario over a range of seeds, and
  `py/SSG/seed-stats.py` aggregates the resulting files.
- The result JSON files also contain the scenario geometry, the strategy pools
  and the utility matrix, so `SSGCrossCompare` and `SSGProbSurvWithS` can be run
  without regenerating the scenario.

## License

SSG is released under the **Mozilla Public License 2.0** (MPL-2.0). The full
text is in [`LICENSE`](LICENSE).

MPL-2.0 is a file-level copyleft license:

- **Using the library is unrestricted.** You may link SSG from a closed-source
  program, including static linking, trimming or single-file publishing, and your
  own files stay under whatever license you choose.
- **Modifications stay open.** If you change a file of SSG — or add a new file
  that contains SSG code — and distribute it, that file must remain under
  MPL-2.0 and its source must be made available.
- Each contributor grants an express patent license for their contributions.

Adding the Exhibit A notice from [`LICENSE`](LICENSE) to the header of each
source file is recommended, so that the covered files are explicit.
