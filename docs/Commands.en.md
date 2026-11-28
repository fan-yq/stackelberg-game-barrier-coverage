# SSG Experiment Commands

This document collects the commands used for the experiments in every part of the SSG project: development-phase behavior tests, the simulation-design subsections of the paper, and the additional cases requested by reviewers.

Conventions:

- A line starting with `# ` marks a disabled command; remove the leading `# ` to run it again.
- Commands for the **AttackerOracle2** and **PredefinedLine** methods are all disabled; this affects Section 4.3, Section 4.4, and S Section 7.

## Development Test Code

### 1. Typical Behavior in Different Scenarios

Used to observe path behavior under different launch/target densities, area shapes, and target counts.

#### 1.1 Launch/Target Density and Number of Targets

- Default seed: launch sites and targets are both relatively clustered.
- `seed 20260609`: launch sites clustered, targets sparse. Compared with 20260515, the 20260515 results collapse completely, while 20260609 yields richer paths.
- Number of targets: compare `-d 2` with `-d 6`.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDENoAttUpdate --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method ACDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 2 --iter 1000 --case Case3 --method ACDE --seed 20260609
```

#### 1.2 Sparse Targets, Clustered Launch Sites (Case3TargetSparse) (tested)

This case shows the effect more clearly than Case3.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3TargetSparse --method ACDE --n-sink 4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3TargetSparse --method SSGDE --n-sink 4
```

#### 1.3 Sparse Targets and Sparse Launch Sites (Case3BothSparse) (tested)

No particularly distinctive behavior.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3BothSparse --method ACDE --n-sink 4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3BothSparse --method SSGDE --n-sink 4
```

#### 1.4 Irregular Area (HullArea4) (tested)

The paths turn out to be even richer in this case.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method ACDE
```

#### 1.5 Effect of Area Size (Case2, Case3) (not tested)

Not tested yet; no commands are available.

## Simulation Design

### Section 4.1 Typical Paths and Psurv Curves

Reproduce the near-optimal paths in the main text and observe how the survival probability Psurv varies with s along a path.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 2 --iter 1000 --case Case3 --method SSGDE --seed 20260609
```

**Near-optimal paths**

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_Case320260609_NSO3_NSI3_NAI2_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json" --def-index=13
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json" --def-index=48
```

**Survival probability Psurv vs. s**

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 901 --def-index 48
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 1730 --def-index 48
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/PredefinedCase_defIndex48_SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 0 --def-index 0
```

### Section 4.2 Difference Count and Area Scale

Compare different difference counts (`-d`) and parameter combinations in the LargeArea scene.

**Diff number**

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 --iter 1000 --case HullArea4 --method SSGDE
```

**LargeArea**

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 2 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 16 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
```

### Section 4.3 Method Comparison (AttackerOracle2 and PredefinedLine disabled)

Compare SSGDE and ACDE with baselines such as AttackerOracle2 and PredefinedLine.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method ACDE
# dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 200 --case HullArea4 --method AttackerOracle2
# dotnet run -c Release --project BuoyancyAircraft.Samples SSGPredefined line --case HullArea4 -n 3 -d 6 --iter 1000
```

**Near-optimal paths**

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_ACDE_crowdingFalse_fixProj_s20260515.json" --eps2=0.001
# dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT200_EPS20.001_AttackerOracle2_crowdingFalse_fixProj_s20260515.json" --eps2=0.001
# dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/PredefinedLine_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_fixProj_s20260515.json" --eps2=0.001
```

The AttackerOracle2 and PredefinedLine commands are disabled.

### Section 4.4 Performance Comparison

Run multiple seeds, collect statistics, and perform cross-comparisons.

**Multiple runs**

```sh
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDE
# python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 50 --case HullArea5 --method AttackerOracle2
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDENoAttUpdate
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method ACDE
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDEPureDE
```

**Statistics and comparison against a fixed solution**

```sh
python py/SSG/seed-stats.py --dir data/SSG/SSGDE_multi
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSGDE_multi/*  --eps=0.05
```

Main text: cross compare, scattered samples.

```sh
python .\py\SSG\Tab3-pre-copy.py -p data\SSG\SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT*_EPS20.001_*_crowdingFalse_fixProj_s20260515.json . data\SSG\compare
python .\py\SSG\Tab3-pre-copy.py -p data\SSG\SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT*_EPS20.001_*_crowdingFalse_fixProj_s20260515.json . data\SSG\compare
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\compare-s20261112/000* --only-best=true --eps=0.05
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\compare/*
```

### Section 4.5 Runtime Comparison

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGBarrierBenchmark
```

### Section 4.5 Iteration Count Comparison

Enable `recordAllMaxMinU` first to record the maximum and minimum at every iteration:

```csharp
[DebugConfig]
const bool recordAllMaxMinU = true;
```

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 3000 --case HullArea4 --method ACDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 3000 --case HullArea4 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 3000 --case HullArea4 --method SSGDENoAttUpdate --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 3000 --case HullArea4 --method SSGDEPureDE --seed 20260609
```

### Section 4.6 Ablation Study

Appendix: ablation experiments comparing the contributions of SSGDE modules, including pabest, AttackerPopSize, and the DE of the attacker.

```sh
# dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m ACGA
# dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDE
# dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDECrowding
# dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDEClamp
# dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDEPabestRandHalf
# dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDEPabestDisable
# dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDEAttPop1
# dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F12 --iterations 500 -m ACGA --repeat=1
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples --all=true --repeat=10
```

## Additional Cases Requested by Reviewers

### Support Section 1: Print Positions

Print the launch-site and target positions for each case.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case Case3  --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case HullArea4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case HullArea5
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 4 -s 3 --case LargeArea
```

### S Section 3: Point Correction Modes

Compare the Resample and Proj point correction modes (Case3, HullArea5) and run cross-comparisons.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --seed 20260609 --method SSGDE --point-fix-mode Resample
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --seed 20260609 --method SSGDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --seed 20260609 --method SSGDEPureDE --point-fix-mode Resample
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --seed 20260609 --method SSGDEPureDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_*_crowdingFalse_fix*_s20260609.json --only-best=true --eps=0.05
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_*_crowdingFalse_fix*_s20260609.json  --eps=0.05
```

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5  --method SSGDE --point-fix-mode Resample
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDEPureDE --point-fix-mode Resample
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDEPureDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_*_crowdingFalse_fix*_s20260515.json --only-best=true --eps=0.05
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_*_crowdingFalse_fix*_s20260515.json  --eps=0.05
```

### S Section 4.1: Model Parameter Sensitivity

Using the baseline configuration as reference, scale each model parameter up and down by 2x for sensitivity analysis.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --suffix baseline
# K_detect
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_detect_2x.json --suffix K_detect_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_detect_2x.json --suffix K_detect_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_detect_0.5x.json --suffix K_detect_0.5x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_detect_0.5x.json --suffix K_detect_0.5x

# K_anti
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_anti_2x.json --suffix K_anti_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_anti_2x.json --suffix K_anti_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_anti_0.5x.json --suffix K_anti_0.5x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_anti_0.5x.json --suffix K_anti_0.5x

# K_anti_max
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_anti_max_2x.json --suffix K_anti_max_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_anti_max_2x.json --suffix K_anti_max_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_anti_max_0.5x.json --suffix K_anti_max_0.5x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/K_anti_max_0.5x.json --suffix K_anti_max_0.5x

# d_anti
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/d_anti_2x.json --suffix d_anti_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/d_anti_2x.json --suffix d_anti_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/d_anti_0.5x.json --suffix d_anti_0.5x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/d_anti_0.5x.json --suffix d_anti_0.5x

# dMaxAttacker
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/dMaxAttacker_2x.json --suffix dMaxAttacker_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/dMaxAttacker_2x.json --suffix dMaxAttacker_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/dMaxAttacker_0.5x.json --suffix dMaxAttacker_0.5x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/dMaxAttacker_0.5x.json --suffix dMaxAttacker_0.5x

# LambdaPath
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/LambdaPath_2x.json --suffix LambdaPath_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/LambdaPath_2x.json --suffix LambdaPath_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/LambdaPath_0.5x.json --suffix LambdaPath_0.5x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/LambdaPath_0.5x.json --suffix LambdaPath_0.5x

# Kappa
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/Kappa_2x.json --suffix Kappa_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/Kappa_2x.json --suffix Kappa_2x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/Kappa_0.5x.json --suffix Kappa_0.5x
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609 --model-file data/SSG/Models/Kappa_0.5x.json --suffix Kappa_0.5x
```

### S Section 4.2: Number of Airships

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case HullArea5 --method SSGDE
```

### S Section 5.1: Attacker / Defender Count Factors

Starting from the baseline configuration (8 attackers, 10 defenders), scale both factors down and up by 2x.

```sh
# att-factor 0.5x (8 -> 4)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --att-factor 4 --suffix att-factor_0.5x
# att-factor 2x (8 -> 16)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --att-factor 16 --suffix att-factor_2x

# def-factor 0.5x (10 -> 5)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --def-factor 5 --suffix def-factor_0.5x
# def-factor 2x (10 -> 20)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --def-factor 20 --suffix def-factor_2x

# att-factor 1x (8, baseline)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --att-factor 8 --suffix att-factor_1x
# def-factor 1x (10, baseline)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --def-factor 10 --suffix def-factor_1x
```

### S Section 5.3: Large-Scale Scenario and sinkValue

To be tested: many airships with different sinkValue settings.

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 2 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 2 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 4 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 4 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
```

### S Section 6.1: Heterogeneous Target Values

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeAreaV --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 16 -k 3 -s 3 --iter 3000 --case LargeAreaV --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 16 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE # support
```

### S Section 6.2: Different Area Configurations

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Triangle             --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Pentagon             --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case LongCorridor         --method SSGDE
# dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case NarrowNeck           --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case MultiSourceMultiSink --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case CircleApprox         --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case RandomConvex         --method SSGDE
```

### S Section 7: Method Comparison

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method ACDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDEPureDE
# dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 50 --case LargeArea --method AttackerOracle2
```

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\SSG_LargeArea20260515_NSO3_NSI3_NAI12_NC3_IT*_EPS20.001_*_crowdingFalse_fixProj_s20260515.json --only-best=true --eps=0.1
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSG_LargeArea20260515_NSO3_NSI3_NAI12_NC3_IT*_EPS20.001_*_crowdingFalse_fixProj_s20260515.json
```

The AttackerOracle2 command is disabled.
