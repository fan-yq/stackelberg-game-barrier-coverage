# 代码

## 开发测试代码

### 1. 测试不同场景下的典型行为

- 起点、终点不同疏密程度的行为
- 不规则的区域

- 默认seed，起点终点比较聚集
- seed 20260609，起点比较聚集，终点比较稀疏
20260609相比于20260515，20260515的结果是完全塌缩的，20260609的路径更加丰富
- 不同终点数量比较
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDENoAttUpdate --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method ACDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 2 --iter 1000 --case Case3 --method ACDE --seed 20260609
```

- [x] 终点稀疏，起点聚集
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3TargetSparse --method ACDE --n-sink 4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3TargetSparse --method SSGDE --n-sink 4
```
这个样例的效果比Case3更加明显

- [x] 终点稀疏，起点稀疏
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3BothSparse --method ACDE --n-sink 4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3BothSparse --method SSGDE --n-sink 4
```
没有特别的特点

- [x] 不规则区域
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method ACDE
```
意外发现该样例的路径丰富程度更高

- [ ] 区域大小的影响，如case2 case3


## 仿真设计

### Section 4.1
近似最优的路径
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_Case320260609_NSO3_NSI3_NAI2_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json" --def-index=13 
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json" --def-index=48
```
路径随s的Psurv概率
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 901 --def-index 48
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 1730 --def-index 48
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGProbSurvWithS -f data/SSG/PredefinedCase_defIndex48_SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260609.json --att-index 0 --def-index 0
```

### Section 4.2
LargeArea
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 2 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 16 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
```

### Section 4.3
近似最优的路径
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_ACDE_crowdingFalse_fixProj_s20260515.json" --eps2=0.001
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT200_EPS20.001_AttackerOracle2_crowdingFalse_fixProj_s20260515.json" --eps2=0.001
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGPredefined predefined -f "data/SSG/PredefinedLine_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_fixProj_s20260515.json" --eps2=0.001

```


### Section 4.4 性能比较
多次运行
```sh
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDE
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 50 --case HullArea5 --method AttackerOracle2
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDENoAttUpdate
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method ACDE
python py/SSG/run-seeds.py --num-runs 30 --seed2-step 1 --seed2-start 20261112 --iter 1000 --case HullArea5 --method SSGDEPureDE

python py/SSG/seed-stats.py --dir data/SSG/SSGDE_multi
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSGDE_multi/*  --eps=0.05
```
- 正文：cross compare,分散样例

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
```sh
[DebugConfig]
const bool recordAllMaxMinU = true;

dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 3000 --case HullArea4 --method ACDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 3000 --case HullArea4 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 3000 --case HullArea4 --method SSGDENoAttUpdate --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 3000 --case HullArea4 --method SSGDEPureDE --seed 20260609
```


### Section 4.6
- 附录：消融试验，比较SSGDE不同模块的作用
改pabest，不使用pabest，改AttPopSize，attacker的DE

```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m ACGA
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDE
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDECrowding
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDEClamp
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDEPabestRandHalf
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDEPabestDisable
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F5 --iterations 500 -m SGDEAttPop1
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples -c F12 --iterations 500 -m ACGA --repeat=1
dotnet run -c Release --project BuoyancyAircraft.Samples -- MinimaxBenchSamples --all=true --repeat=1
```


## 审稿专家要求增加的算例

### Support Section 1: 打印位置
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case Case3  --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case HullArea4
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 3 -s 3 --case HullArea5
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPrintLaunchSitesTargets -k 4 -s 3 --case LargeArea
```

### S Section 3
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --seed 20260609 --method SSGDE --point-fix-mode Resample
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --seed 20260609 --method SSGDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --seed 20260609 --method SSGDEPureDE --point-fix-mode Resample
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --seed 20260609 --method SSGDEPureDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_*_crowdingFalse_fix*_s20260609.json --only-best=true --eps=0.05
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSG_Case320260609_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_*_crowdingFalse_fix*_s20260609.json  --eps=0.05


dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5  --method SSGDE --point-fix-mode Resample
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDEPureDE --point-fix-mode Resample
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDEPureDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_*_crowdingFalse_fix*_s20260515.json --only-best=true --eps=0.05
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_*_crowdingFalse_fix*_s20260515.json  --eps=0.05


```

### S Section 4.1
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

### S Section 4.2 number of airships
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case HullArea5 --method SSGDE
```

### S Section 5.1
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

### S Section 5.3
3. [ ] 大量飞艇，不同的sinkValue
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 2 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 2 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 4 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 4 -d 12 -k 3 -s 3 --iter 1000 --case LargeArea --method SSGDE
```

### S Section 6.1 heterogeneous target values
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeAreaV --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 16 -k 3 -s 3 --iter 3000 --case LargeAreaV --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE # support
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 16 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE # support
```

### S Section 6.2 different configuration of area
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Triangle             --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Pentagon             --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case LongCorridor         --method SSGDE
# dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case NarrowNeck           --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case MultiSourceMultiSink --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case CircleApprox         --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case RandomConvex         --method SSGDE
```

### S Section 7 method compare
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method ACDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 3000 --case LargeArea --method SSGDEPureDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 -k 3 -s 3 --iter 50 --case LargeArea --method AttackerOracle2

dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\SSG_LargeArea20260515_NSO3_NSI3_NAI12_NC3_IT*_EPS20.001_*_crowdingFalse_fixProj_s20260515.json --only-best=true --eps=0.1
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCompareWithFixed data\SSG\SSG_LargeArea20260515_NSO3_NSI3_NAI12_NC3_IT*_EPS20.001_*_crowdingFalse_fixProj_s20260515.json

```

### 待整理命令

5. [x] 线，人工
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPredefined line --case HullArea4 -n 3 -d 6 --iter 1000
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\compare/*

```

6. [x] 投影

发现投影效果确实好一些，而且自己设计的算子没有优势
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE 
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE  --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDEPureDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --point-fix-mode Proj
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE --point-fix-mode Proj
```

验证
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples -- SSGCrossCompare data\SSG\SSG_HullArea520260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixResample_s20260515.json data\SSG\SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDEPureDE_crowdingFalse_fixResample_s20260515.json data\SSG\SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDE_crowdingFalse_fixProj_s20260515.json data\SSG\SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDEPureDE_crowdingFalse_fixProj_s20260515.json data\SSG\SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDEDefH_crowdingFalse_fixProj_s20260515.json data\SSG\SSG_HullArea420260515_NSO3_NSI3_NAI6_NC3_IT1000_EPS20.001_SSGDEAttH_crowdingFalse_fixProj_s20260515.json
```

7. [x] attacker oracle
```sh
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 200 --case HullArea5 --method AttackerOracle2
```

## 运行论文的样例(前半部分)

```sh
# 方法对比+不同数量对比 Section 4.1
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case Case3 --method SSGDE --seed 20260609
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 2 --iter 1000 --case Case3 --method SSGDE --seed 20260609
# Section 4.1 不同方法
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 1 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 12 --iter 1000 --case HullArea4 --method SSGDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea4 --method ACDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 200 --case HullArea4 --method AttackerOracle2
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPredefined line --case HullArea4 -n 3 -d 6 --iter 1000

dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDE

# 表 4.2
dotnet run -c Release --project BuoyancyAircraft.Samples SSGPredefined line --case HullArea5 -n 3 -d 6 --iter 1000
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 50 --case HullArea5 --method AttackerOracle2
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDENoAttUpdate
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method ACDE
dotnet run -c Release --project BuoyancyAircraft.Samples SSGBarrierSamples2 -n 3 -d 6 --iter 1000 --case HullArea5 --method SSGDEPureDE
```
