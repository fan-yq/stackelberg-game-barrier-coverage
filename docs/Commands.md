# SSG 实验命令

本文档汇总 SSG 项目各章节实验所用的命令：开发阶段的行为测试、论文仿真设计各小节，以及审稿专家要求补充的算例。

约定：

- 命令行以 `# ` 开头表示该命令已停用，删除行首的 `# ` 即可恢复执行。
- 当前 **AttackerOracle2** 与 **PredefinedLine** 两类方法的命令已全部停用，涉及 Section 4.3、Section 4.4 与 S Section 7。

## 开发测试代码

### 1. 测试不同场景下的典型行为

用于观察不同起终点疏密、区域形态与终点数量下的路径行为。

#### 1.1 起终点疏密与终点数量

- 默认 seed：起点、终点都比较聚集。
- `seed 20260609`：起点比较聚集，终点比较稀疏。与 20260515 相比，20260515 的结果完全塌缩，20260609 的路径更加丰富。
- 不同终点数量：通过 `-d 2` 与 `-d 6` 对比。

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDENoAttUpdate --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method ACDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 2 --iter 1000 --case Case3 --method ACDE --seed 20260609
```

#### 1.2 终点稀疏、起点聚集（Case3TargetSparse）（已测试）

该样例的效果比 Case3 更加明显。

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3TargetSparse --method ACDE --n-sink 4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3TargetSparse --method SSGDE --n-sink 4
```

#### 1.3 终点稀疏、起点稀疏（Case3BothSparse）（已测试）

没有特别的特点。

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3BothSparse --method ACDE --n-sink 4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3BothSparse --method SSGDE --n-sink 4
```

#### 1.4 不规则区域（HullArea4）（已测试）

意外发现该样例的路径丰富程度更高。

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method ACDE
```

#### 1.5 区域大小的影响（Case2、Case3）（待测）

尚未测试，暂无对应命令。

## 仿真设计

### Section 4.1 典型路径与 Psurv 曲线

复现正文中的近似最优路径，并观察路径随 s 的 Psurv 存活概率。

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 2 --iter 1000 --case Case3 --method SSGDE --seed 20260609
```

**近似最优的路径**

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_Case320260609_NSO3_NSI3_NAI2_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json" --def-index=13
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json" --def-index=48
```

**路径随 s 的 Psurv 概率**

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 901 --def-index 48
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 1730 --def-index 48
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/PredefinedCase_defIndex48_SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 0 --def-index 0
```

### Section 4.2 差分数量与区域规模

对比不同差分数量（`-d`）以及 LargeArea 场景下的参数组合。

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

### Section 4.3 方法对比（AttackerOracle2、PredefinedLine 已停用）

对比 SSGDE、ACDE 与 AttackerOracle2、PredefinedLine 等基线方法。

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method ACDE
# dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 200 --case HullArea4 --method AttackerOracle2
# dotnet run -c Release --project BuoyancyAircraft.Samples SSGPredefined line --case HullArea4 -n 3 -d 6 --iter 1000
```

**近似最优的路径**

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_ACDE_crowdingFalse_fixProj_s20260515.json" --eps2=0.001
# dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT200_EPS20.001_AttackerOracle2_crowdingFalse_fixProj_s20260515.json" --eps2=0.001
# dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/PredefinedLine_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_fixProj_s20260515.json" --eps2=0.001
```

其中 AttackerOracle2 与 PredefinedLine 的命令已停用。

### Section 4.4 性能比较

多 seed 重复运行并统计，再进行交叉比较。

**多次运行**

```sh
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDE
# python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 50 --case HullArea5 --method AttackerOracle2
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDENoAttUpdate
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method ACDE
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDEPureDE
```

**统计与固定解比较**

```sh
python py/SSG/seed-stats.py --dir data/SSG/SSGDE_multi
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSGDE_multi/*  --eps=0.05
```

正文：cross compare，分散样例。

```sh
python .\py\SSG\Tab3-pre-copy.py -p data\SSG\SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT*_EPS20.001_*_crowdingFalse_fixProj_s20260515.json . data\SSG\compare
python .\py\SSG\Tab3-pre-copy.py -p data\SSG\SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT*_EPS20.001_*_crowdingFalse_fixProj_s20260515.json . data\SSG\compare
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\compare-s20261112/000* --only-best=true --eps=0.05
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\compare/*
```

### Section 4.5 时间性能比较

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGBarrierBenchmark
```

### Section 4.5 迭代次数比较

需先开启 `recordAllMaxMinU`，记录每次迭代的最大值与最小值：

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

### Section 4.6 消融实验

附录：消融试验，比较 SSGDE 不同模块的作用，包括 pabest、AttackerPopSize 以及 attacker 的 DE。

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

## 审稿专家要求增加的算例

### Support Section 1：打印位置

输出各算例的发射点与目标点位置。

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case Case3  --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case HullArea4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case HullArea5
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 4 -s 3 --case LargeArea
```

### S Section 3：点修正方式对比

对比 Resample 与 Proj 两种点修正方式（Case3、HullArea5），并进行交叉比较。

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

### S Section 4.1：模型参数敏感性

以基准配置为参照，将各模型参数分别放大、缩小 2 倍进行敏感性分析。

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

### S Section 4.2：飞艇数量

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case HullArea5 --method SSGDE
```

### S Section 5.1：attacker / defender 数量因子

在基准配置（attacker 8、defender 10）的基础上，将两类数量因子分别缩小、放大 2 倍。

```sh
# att-factor 0.5x (8 -> 4)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --att-factor 4 --suffix att-factor_0.5x
# att-factor 2x (8 -> 16)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --att-factor 16 --suffix att-factor_2x

# def-factor 0.5x (10 -> 5)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --def-factor 5 --suffix def-factor_0.5x
# def-factor 2x (10 -> 20)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --def-factor 20 --suffix def-factor_2x

# att-factor 1x (8，基准)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --att-factor 8 --suffix att-factor_1x
# def-factor 1x (10，基准)
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --def-factor 10 --suffix def-factor_1x
```

### S Section 5.3：大规模场景与 sinkValue

待测：大量飞艇、不同 sinkValue 的情形。

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 2 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 2 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 4 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 4 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
```

### S Section 6.1：异构目标值

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeAreaV --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 16 -k 3 -s 3 --iter 3000 --case LargeAreaV --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 16 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE # support
```

### S Section 6.2：不同区域构型

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Triangle             --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Pentagon             --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case LongCorridor         --method SSGDE
# dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case NarrowNeck           --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case MultiSourceMultiSink --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case CircleApprox         --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case RandomConvex         --method SSGDE
```

### S Section 7：方法对比

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

其中 AttackerOracle2 的命令已停用。
