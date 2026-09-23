# 美标设计实用工具集

基于 .NET 8 和 WPF 的结构工程计算工具，涵盖中美规范参数转换和结构构件设计验算。

---

## 功能模块

### ASCE 7

| #   | 模块       | 规范                       | 说明                        |
| --- | -------- | ------------------------ | ------------------------- |
| 1   | 反应谱比较    | GB50011-2010 / ASCE 7-16 | 中美规范地震反应谱对比分析             |
| 2   | 地震人工波模拟  | GB50011-2010 / ASCE 7-16 | 频域迭代拟合生成符合目标反应谱的人工地震波     |
| 3   | 风速转换     | ASCE 7 / GB50009-2012    | ASCE 7 风速转换为 GB50009 基本风压 |
| 4   | 阵风响应因子 G | ASCE 7-16 §26.11         | 刚性/柔性建筑阵风效应因子计算           |
| 5   | 脉动风模拟    | ASCE 7-16                | Kaimal 谱 + FFT 方法模拟脉动风时程  |

### ACI 318

| #   | 模块     | 规范         | 说明                   |
| --- | ------ | ---------- | -------------------- |
| 6   | 单筋混凝土梁 | ACI 318-25 | 矩形梁受弯受剪配筋设计          |
| 7   | 混凝土矩形柱 | ACI 318-19 | 双向偏压设计，P-M 交互图，抗剪验算  |
| 8   | 混凝土圆形柱 | ACI 318-19 | 双向偏压设计，绑扎/螺旋箍筋       |
| 9   | 双向板抗冲切 | ACI 318-19 | 板柱节点抗冲切验算，支持栓钉/箍筋    |
| 10  | 钢筋锚固长度 | ACI 318-19 | 受拉直钢筋 ℓd 计算（精确法/简化法） |

### AISC 360

| #   | 模块      | 规范               | 说明                    |
| --- | ------- | ---------------- | --------------------- |
| 11  | 钢构件验算   | AISC 360-16      | 工字钢和矩形钢管抗弯抗剪验算        |
| 12  | 钢构件抗拉验算 | AISC 360-16 Ch.D | 工字钢/HSS 抗拉验算（屈服+断裂）   |
| 13  | 钢构件抗压验算 | AISC 360-16 Ch.E | 工字钢/HSS 抗压验算（弯曲+扭转屈曲） |

### AISC 341

| #   | 模块         | 规范                                      | 说明                             |
| --- | ---------- | --------------------------------------- | ------------------------------ |
| 14  | 延性分类与宽厚比校核 | AISC 341-16 §D1.1                       | 抗震体系构件延性等级判定与 Table D1.1 宽厚比校核 |
| 15  | SMF 强柱弱梁校核 | AISC 341-16 §E3.4a                      | 特殊抗弯框架柱梁弯矩比验算，支持截面库选型          |
| 16  | 梁侧向稳定支撑验算  | AISC 341-16 §D1.2 + AISC 360-16 App 6   | 梁/连梁侧向与抗扭支撑强度及刚度需求计算           |
| 17  | 梁柱节点剪切区验算  | AISC 341-16 §E3.6e + AISC 360-16 §J10.6 | 梁柱节点域剪切强度与补强板设计（SMF/IMF/OMF）   |

### AISC 358

| #   | 模块              | 规范                | 说明                                   |
| --- | --------------- | ----------------- | ------------------------------------ |
| 18  | RBS 连接验算        | AISC 358-16 Ch.5  | 削弱梁截面连接设计验算（SMF/IMF）                 |
| 19  | End-Plate 连接验算  | AISC 358-16 Ch.6  | 端板连接设计验算（4E/4ES/8ES）                 |
| 20  | BFP 连接验算        | AISC 358-16 Ch.7  | 螺栓翼缘板连接设计验算（A490/F2280）              |
| 21  | WUF-W 连接验算      | AISC 358-16 Ch.8  | 焊接翼缘连接设计验算（C_pr=1.4）                 |
| 22  | KBB 连接验算        | AISC 358-16 Ch.9  | Kaiser Bolted Bracket 支架连接验算（W/B 系列） |
| 23  | ConXL 连接验算      | AISC 358-16 Ch.10 | ConXL 矩钢管混凝土柱连接验算（可选 RBS）            |
| 24  | SidePlate 连接验算  | AISC 358-16 Ch.11 | SidePlate 侧板连接验算（焊接/螺栓两种类型）          |
| 25  | SST 连接验算        | AISC 358-16 Ch.12 | Simpson Strong-Tie Yield-Link 连接验算   |
| 26  | DoubleTee 连接验算  | AISC 358-16 Ch.13 | 双 T 连接设计验算（T-Stub 螺栓连接）              |
| 27  | SlottedWeb 连接验算 | AISC 358-16 Ch.14 | 开槽腹板连接设计验算（SMF only）                 |

---

## 技术栈

| 依赖                   | 版本          | 用途                 |
| -------------------- | ----------- | ------------------ |
| .NET                 | 8.0         | 运行平台               |
| WPF                  | .NET 8 内置   | UI 框架              |
| LiveCharts2          | 2.0.0-rc3.3 | P-M 交互图等图表         |
| OxyPlot.Wpf          | 2.2.0       | 风速时程、PSD 图表        |
| MaterialDesignThemes | 5.1.0       | Material Design 样式 |
| MathNet.Numerics     | 5.0.0       | FFT、数值计算           |
| ClosedXML            | 0.104.2     | 读取 AISC 截面库        |

---

## 项目文件结构

```
├── USCodeTools.csproj                        # 项目配置
│
├── 计算类 (Model)
│   ├── CoreCalculations.cs                   # 反应谱 + 风速转换
│   ├── ArtificialWaveCalculations.cs         # 人工地震波（频域迭代）
│   ├── GustEffectFactorCalculations.cs       # 阵风响应因子
│   ├── WindSimulationCalculations.cs         # 脉动风模拟（Kaimal + FFT）
│   ├── BeamDesignCalculations.cs             # 梁受弯受剪
│   ├── ColumnDesignCalculations.cs           # 矩形柱 P-M
│   ├── CircularColumnDesignCalculations.cs   # 圆形柱 P-M
│   ├── PunchingShearCalculations.cs          # 抗冲切
│   ├── DevelopmentLengthCalculations.cs      # 锚固长度
│   └── SteelBeamCalculations.cs              # 钢构件抗弯抗剪
│   ├── SteelTensionCalculations.cs            # 钢构件抗拉
│   └── SteelCompressionCalculations.cs        # 钢构件抗压
│   ├── DuctilityClassificationCalculations.cs # 延性分类与宽厚比校核
│   ├── StrongColumnWeakBeamCalculations.cs    # SMF 强柱弱梁校核
│   ├── BeamStabilityBracingCalculations.cs    # 梁侧向稳定支撑验算
│   ├── PanelZoneCalculations.cs               # 梁柱节点剪切区验算
│   ├── RbsCalculations.cs                     # RBS 连接验算
│   ├── EndplateCalculations.cs                # 端板连接验算
│   ├── BfpCalculations.cs                     # BFP 连接验算
│   └── WufwCalculations.cs                    # WUF-W 连接验算
│   ├── KbbCalculations.cs                      # KBB 连接验算
│   ├── ConxlCalculations.cs                    # ConXL 连接验算
│   ├── SideplateCalculations.cs                # SidePlate 连接验算
│   ├── SstCalculations.cs                      # SST 连接验算
│   ├── DoubleteeCalculations.cs                # DoubleTee 连接验算
│   └── SlottedwebCalculations.cs               # SlottedWeb 连接验算
│
├── ViewModel
│   ├── SpectrumViewModel.cs
│   ├── ArtificialWaveViewModel.cs
│   ├── WindConversionViewModel.cs
│   ├── GustEffectFactorViewModel.cs
│   ├── WindSimulationViewModel.cs
│   ├── BeamDesignViewModel.cs
│   ├── ColumnDesignViewModel.cs
│   ├── CircularColumnDesignViewModel.cs
│   ├── PunchingShearViewModel.cs
│   ├── DevelopmentLengthViewModel.cs
│   └── SteelBeamViewModel.cs
│   ├── SteelTensionViewModel.cs
│   └── SteelCompressionViewModel.cs
│   ├── DuctilityClassificationViewModel.cs
│   ├── StrongColumnWeakBeamViewModel.cs
│   ├── BeamStabilityBracingViewModel.cs
│   ├── PanelZoneViewModel.cs
│   ├── RbsViewModel.cs
│   ├── EndplateViewModel.cs
│   ├── BfpViewModel.cs
│   └── WufwViewModel.cs
│   ├── KbbViewModel.cs
│   ├── ConxlViewModel.cs
│   ├── SideplateViewModel.cs
│   ├── SstViewModel.cs
│   ├── DoubleteeViewModel.cs
│   └── SlottedwebViewModel.cs
│
├── View
│   ├── SpectrumView.xaml / .cs
│   ├── ArtificialWaveView.xaml / .cs
│   ├── WindConversionView.xaml / .cs
│   ├── GustEffectFactorView.xaml / .cs
│   ├── WindSimulationView.xaml / .cs
│   ├── BeamDesignView.xaml / .cs
│   ├── ColumnDesignView.xaml / .cs
│   ├── CircularColumnDesignView.xaml / .cs
│   ├── PunchingShearView.xaml / .cs
│   ├── DevelopmentLengthView.xaml / .cs
│   ├── SteelBeamView.xaml / .cs
│   ├── SteelTensionView.xaml / .cs
│   ├── SteelCompressionView.xaml / .cs
│   ├── DuctilityClassificationView.xaml / .cs
│   ├── StrongColumnWeakBeamView.xaml / .cs
│   ├── BeamStabilityBracingView.xaml / .cs
│   ├── PanelZoneView.xaml / .cs
│   ├── RbsView.xaml / .cs
│   ├── EndplateView.xaml / .cs
│   ├── BfpView.xaml / .cs
│   ├── WufwView.xaml / .cs
│   ├── KbbView.xaml / .cs
│   ├── ConxlView.xaml / .cs
│   ├── SideplateView.xaml / .cs
│   ├── SstView.xaml / .cs
│   ├── DoubleteeView.xaml / .cs
│   ├── SlottedwebView.xaml / .cs
│   └── WeChatView.xaml / .cs
│
├── 公共文件
│   ├── MainWindow.xaml / .cs                 # 主窗口导航（Expander 二级分类）
│   ├── Styles.xaml                           # Fluent Design 样式
│   ├── Aisc358ShapeHelper.cs                 # AISC 358 共享 W-shape 截面加载
│   ├── RelayCommand.cs                       # ICommand 实现
│   ├── BooleanToVisibilityConverter.cs       # 可见性转换器
│   └── InverseBoolConverter.cs               # 布尔取反转换器
│
└── 资源文件
    ├── aisc-shapes-database-v160-2.xlsx      # AISC 截面库（W + HSS）
    └── qrcode_for_gh_88e97e7d9e0f_344.jpg   # 公众号二维码
```

---

## 模块详细说明

### 1. 反应谱比较 (SpectrumView)

对比 GB50011-2010 和 ASCE 7-16 的设计反应谱。

**输入参数：**

| 参数                   | 单位  | 默认值       | 说明                        |
| -------------------- | --- | --------- | ------------------------- |
| Damping              | -   | 0.05      | 阻尼比 ξ                     |
| ChinaIntensity       | -   | 7度(0.10g) | 设防烈度（6~9度）                |
| ChinaSiteCategory    | -   | II        | 场地类别（I₀, I₁, II, III, IV） |
| ChinaEarthquakeGroup | -   | 第一组       | 地震分组                      |
| UsSs                 | g   | 0.51      | 短周期谱加速度                   |
| UsS1                 | g   | 0.18      | 1s 谱加速度                   |
| UsSiteClass          | -   | D         | 场地类别（A~D）                 |
| UsTl                 | s   | 24        | 长周期过渡期                    |
| UsR                  | -   | 5.0       | 反应修正系数                    |

**输出：** 中美反应谱对比图（周期 vs 谱加速度），αmax、Tg、SDS、SD1、Fa、Fv。

**核心方法：**

- `CoreCalculations.CalculateChineseSpectrum()` — GB50011 五段式反应谱
- `CoreCalculations.CalculateUsSpectrum()` — ASCE 7-16 设计反应谱
- `CoreCalculations.GetFaFv()` — 场地系数线性插值

---

### 2. 地震人工波模拟 (ArtificialWaveView)

基于频域迭代拟合方法，生成符合目标反应谱的人工地震波。

**输入参数：**

| 参数                 | 单位  | 默认值   | 说明        |
| ------------------ | --- | ----- | --------- |
| UseChineseCode     | -   | false | 中国/美国规范切换 |
| Damping            | -   | 0.05  | 阻尼比       |
| NumberOfWaves      | -   | 5     | 生成波数量     |
| Dt                 | s   | 0.01  | 时间步长      |
| TTotal             | s   | 30    | 总时长       |
| NumberOfIterations | -   | 7     | 迭代拟合次数    |
| T1                 | s   | 3.0   | 包络线上升段结束  |
| T2                 | s   | 25.0  | 包络线平稳段结束  |
| CDecay             | /s  | 0.2   | 衰减系数      |

**输出：** 目标谱（红）+ 各波谱（灰）+ 平均谱（蓝）对比图，CSV 导出。

**核心方法：**

- `GenerateArtificialWaves()` — 频域迭代拟合主流程
- `CalculateResponseSpectrum()` — SDOF 弹性反应谱计算
- `GetEnvelope()` — 三段式非平稳包络线
- `ButterworthHighPass()` — 4 阶高通滤波基线校正

---

### 3. 风速转换 (WindConversionView)

将 ASCE 7 风速转换为中国 GB50009 基本风压。

**输入：** 风速（mph 或 m/s）、测量高度（m）、时距（3s~1h）、重现期（300~3000年）。

**输出：** 50年重现期 10m 高度 10 分钟平均风速（m/s）、基本风压（kN/m²）。

**转换步骤：** 单位转换 → 重现期转换 → 时距转换 → 高度转换 → 风压计算。

---

### 4. 阵风响应因子 G (GustEffectFactorView)

计算 ASCE 7-16 §26.11 阵风效应因子。

**输入参数：**

| 参数               | 单位  | 默认值  | 说明            |
| ---------------- | --- | ---- | ------------- |
| Exposure         | -   | C    | 场地类别（B, C, D） |
| BuildingHeight   | m   | 120  | 建筑高度 h        |
| BuildingWidth    | m   | 40   | 建筑宽度 B        |
| BuildingDepth    | m   | 40   | 建筑深度 L        |
| WindSpeed        | m/s | 45   | 基本风速 V        |
| NaturalFrequency | Hz  | 0.25 | 基频 n₁         |
| Beta             | -   | 0.01 | 阻尼比 β         |

**输出：** 自动判断刚性（n₁ ≥ 1Hz）或柔性建筑，计算 G 或 Gf，含背景响应 Q、共振响应 R。

---

### 5. 脉动风模拟 (WindSimulationView)

基于 Kaimal 谱 + FFT 随机相位法模拟脉动风时程。

**输入参数：**

| 参数         | 单位  | 默认值 | 说明   |
| ---------- | --- | --- | ---- |
| VRef       | mph | 115 | 基本风速 |
| Exposure   | -   | C   | 场地类别 |
| Height     | ft  | 100 | 目标高度 |
| Duration   | s   | 600 | 模拟时长 |
| SampleRate | Hz  | 20  | 采样频率 |

**输出：** 风速时程图、目标 Kaimal 谱 vs 模拟 PSD 对比图（OxyPlot 双对数坐标），CSV 导出。

---

### 6. 单筋混凝土梁 (BeamDesignView)

ACI 318-25 矩形梁受弯受剪配筋设计。

**输入参数：**

| 参数    | 单位     | 默认值   | 说明      |
| ----- | ------ | ----- | ------- |
| Fc    | psi    | 4000  | 混凝土抗压强度 |
| Fy    | psi    | 60000 | 钢筋屈服强度  |
| B     | in     | 14    | 截面宽度    |
| H     | in     | 24    | 截面高度    |
| Cover | in     | 1.5   | 保护层     |
| Mu    | kip-ft | 298.1 | 设计弯矩    |
| Vu    | kips   | 49.7  | 设计剪力    |
| Nu    | kips   | 0     | 设计轴力    |

**输出：** 所需/提供钢筋面积、根数、φMn、弯矩比、受拉/过渡/受压控制截面判别、箍筋间距设计、截面钢筋预览。

**核心方法：**

- `CalculateFlexuralReinforcement()` — 受弯配筋（β1、ρ、a、c、εt、φ 判定）
- `CalculateShearReinforcement()` — 抗剪配筋（Vc、Vs、箍筋间距、构造验算）

---

### 7. 混凝土矩形柱 (ColumnDesignView)

ACI 318-19 双向偏压设计，含 P-M 交互图和抗剪验算。

**输入：** Fc/Fy、截面 B×H、保护层、纵筋规格/根数(Nx×Ny)、箍筋、Pu/Mux/Muy/Vux/Vuy。

**输出：** X/Y 双向 P-M 交互图（名义曲线 + 设计曲线 + 荷载点）、配筋率验算（1%~8%）、最小根数（≥4）、纵筋间距验算、抗剪设计、截面钢筋可视化。

---

### 8. 混凝土圆形柱 (CircularColumnDesignView)

ACI 318-19 圆形柱设计，支持绑扎/螺旋箍筋。

**输入：** Fc/Fy、直径、保护层、纵筋规格/根数（≥6）、箍筋类型（Tied: φ=0.65 / Spiral: φ=0.75）、Pu/Mux/Muy/Vux/Vuy。

**输出：** 合成弯矩 Mu=√(Mux²+Muy²)、各向同性 P-M 图、圆形截面应力积分、抗剪验算。

---

### 9. 双向板抗冲切 (PunchingShearView)

ACI 318-19 板柱节点抗冲切验算。

**输入参数：**

| 参数                | 单位     | 默认值      | 说明            |
| ----------------- | ------ | -------- | ------------- |
| H                 | in     | 8        | 板厚            |
| C1, C2            | in     | 18       | 柱尺寸           |
| Location          | -      | Interior | 柱位置（中柱/边柱/角柱） |
| Vu                | kips   | 80       | 设计剪力          |
| Msc               | kip-ft | 120      | 不平衡弯矩         |
| ReinforcementType | -      | None     | 无/箍筋/栓钉       |

**输出：** 尺寸效应系数 λs=√(2/(1+d/10))、临界截面属性（bo, Jc, γv）、混凝土承载力 vc（三公式取小）、剪应力需求 vu、抗剪钢筋设计、构造要求检查。

---

### 10. 钢筋锚固长度 (DevelopmentLengthView)

ACI 318-19 受拉直钢筋锚固长度 ℓd。

**输入：** f'c、混凝土类型、钢筋等级（40/60/80/100）、规格、涂层、浇筑位置、计算方法（精确/简化）、约束参数或间距条件、抗震标志（SFRS/屈服区）、多余钢筋折减。

**输出：** 修正系数 ψt/ψe/ψs/ψg/λ、约束项 (cb+Ktr)/db、基础 ℓd、折减、抗震放大（1.25×）、最终 ℓd（≥12in）。

---

### 11. 钢构件验算 (SteelBeamView)

AISC 360-16 工字钢和矩形钢管抗弯抗剪验算。

**截面来源：** AISC 截面库（W 形 + 矩形/方形 HSS）或自定义输入。

**自定义工字钢输入：** d（高度）、bf（翼缘宽度）、tw（腹板厚度）、tf（翼缘厚度），自动计算 A/Ix/Iy/Sx/Sy/Zx/Zy/rx/ry/J/Cw/rts。

**自定义 HSS 输入：** Ht（高度）、B（宽度）、tdes（设计壁厚），自动计算全部截面特性。

**输入参数：**

| 参数     | 单位     | 默认值   | 说明       |
| ------ | ------ | ----- | -------- |
| Fy     | ksi    | 50    | 屈服强度     |
| E      | ksi    | 29000 | 弹性模量     |
| Mu     | kip-ft | 100   | 设计弯矩     |
| Vu     | kips   | 50    | 设计剪力     |
| Lb     | ft     | 10    | 无支撑长度    |
| Cb     | -      | 1.0   | 弯矩梯度系数   |
| Method | -      | LRFD  | LRFD/ASD |

**计算流程：**

1. **截面分类**（Table B4.1b）：
   
   - 工字钢翼缘 Case 10：λp=0.38√(E/Fy)，λr=1.0√(E/Fy)
   - 工字钢腹板 Case 15：λp=3.76√(E/Fy)，λr=5.70√(E/Fy)
   - HSS 翼缘 Case 17：λp=1.12√(E/Fy)，λr=1.40√(E/Fy)
   - HSS 腹板 Case 19：λp=2.42√(E/Fy)，λr=5.70√(E/Fy)

2. **抗弯承载力**：
   
   - 工字钢 Chapter F2（紧凑）或 F3（非紧凑/纤细翼缘）：屈服、LTB、FLB
   - HSS Chapter F7：屈服、FLB、WLB、LTB（方形截面无 LTB）

3. **抗剪承载力**：
   
   - 工字钢 Chapter G2：Vn=0.6Fy·Aw·Cv1，φv=1.0 或 0.9
   - HSS Chapter G4：Vn=0.6Fy·Aw·Cv2，kv=5

4. **设计验算**：LRFD（φMn≥Mu, φVn≥Vu）或 ASD（Mn/Ω≥Ma, Vn/Ω≥Va）

**输出：** 控制极限状态、名义/设计承载力、抗弯应力比、抗剪应力比、PASS/FAIL。

---

### 12. 钢构件抗拉验算 (SteelTensionView)

AISC 360-16 Chapter D 受拉构件设计验算。

**截面来源：** AISC 截面库（W 形 + 矩形/方形 HSS）或自定义输入（同模块 11）。

**输入参数：**

| 参数            | 单位   | 默认值    | 说明                     |
| ------------- | ---- | ------ | ---------------------- |
| Fy            | ksi  | 50     | 屈服强度                   |
| Fu            | ksi  | 65     | 抗拉强度                   |
| Tu            | kips | 100    | 设计拉力                   |
| Method        | -    | LRFD   | LRFD/ASD               |
| Connection    | -    | Case 1 | 连接类型（Table D3.1）       |
| Bolt dia.     | in   | 0.875  | 螺栓公称直径                 |
| Num. holes    | -    | 2      | 螺栓孔数                   |
| Member Length | ft   | 0      | 构件长度（长细比检查，推荐 L/r≤300） |

**连接类型选项（Table D3.1）：**

- Case 1: 全截面连接（U=1.0）
- Case 7: 工字钢翼缘螺栓连接（自动判定 bf/d，U=0.90 或 0.85）
- Case 7c: 工字钢腹板螺栓连接（U=0.70）
- Case 6a/6b: HSS 单侧/双侧节点板（U=1-x̄/l，需输入连接长度 l）
- 用户自定义 U

**计算流程：**

1. **净截面 An**：螺栓孔径 = 螺栓直径 + 1/8"（含 B4.3b 损伤补偿）
2. **剪力滞后系数 U**（Table D3.1），含开口截面下限保护 U≥A_connected/Ag
3. **有效净面积**：Ae = U × An（Eq. D3-1）
4. **屈服承载力**：Pn = Fy × Ag（D2-1，φ=0.90，Ω=1.67）
5. **断裂承载力**：Pn = Fu × Ae（D2-2，φ=0.75，Ω=2.00）
6. **设计验算**：取两者较小值，验算 Tu/Pn ≤ 1.0
7. **长细比检查**：L/r ≤ 300（推荐）

**输出：** 两个极限状态承载力、控制极限状态、应力比、PASS/FAIL。

---

### 13. 钢构件抗压验算 (SteelCompressionView)

AISC 360-16 Chapter E 受压构件设计验算。

**截面来源：** AISC 截面库（W 形 + 矩形/方形 HSS）或自定义输入（同模块 11）。

**输入参数：**

| 参数     | 单位   | 默认值   | 说明            |
| ------ | ---- | ----- | ------------- |
| Fy     | ksi  | 50    | 屈服强度          |
| E      | ksi  | 29000 | 弹性模量          |
| Pu     | kips | 200   | 设计压力          |
| Method | -    | LRFD  | LRFD/ASD      |
| Kx, Ky | -    | 1.0   | 有效长度系数（x/y 轴） |
| Lx, Ly | ft   | 15    | 无支撑长度（x/y 轴）  |
| Lz     | ft   | 15    | 扭转无支撑长度（仅工字钢） |

**计算流程：**

1. **截面分类**（Table B4.1a 受压）：
   
   - 工字钢翼缘：轧制 λr=0.56√(E/Fy)，焊接 λr=0.64√(kc·E/Fy)
   - 工字钢腹板：λr=1.49√(E/Fy)
   - HSS 壁板：λr=1.40√(E/Fy)

2. **长细比**：(KL/r)x、(KL/r)y，取较大值

3. **弯曲屈曲**（E3）：Fe=π²E/(KL/r)²，Fcr 按 E3-2（非弹性）或 E3-3（弹性）

4. **扭转屈曲**（E4，仅工字钢）：Fez=(π²ECw/Lcz²+GJ)/(Ix+Iy)

5. **细长板件有效面积**（E7）：
   
   - 触发条件：λ > λr·√(Fy/Fcr)（E7.1）
   - Fel = (c2·λr/λ)²·Fy（E7-5）
   - be = b(1-c1·√(Fel/Fcr))·√(Fel/Fcr)（E7-3）
   - c1/c2 按 Table E7.1 区分：翼缘(0.22/1.49)、腹板(0.18/1.31)、HSS壁(0.20/1.38)

6. **设计验算**：Pn = Fcr × Ae，φc=0.90，Ωc=1.67

**输出：** 控制屈曲模式、Fcr、Ae（含折减）、名义/设计承载力、应力比、PASS/FAIL。

---

### 14. 延性分类与宽厚比校核 (DuctilityClassificationView)

AISC 341-16 §D1.1 抗震体系构件延性等级判定与 Table D1.1 宽厚比校核。

**截面来源：** AISC 截面库（W 形 + 矩形/方形/圆形 HSS）或自定义输入。

**支持的抗震体系（11 种）：**
OMF, IMF, SMF, STMF, OCCS, SCCS, OCBF, SCBF, EBF, BRBF, SPSW

**支持的截面类型（9 种）：**
I-Shape, HSS Rectangular, HSS Round, Built-up Box, Double Angle, Single Angle, Tee, H-Pile, Channel

**输入参数：**

| 参数            | 单位   | 默认值           | 说明            |
| ------------- | ---- | ------------- | ------------- |
| System        | -    | SMF           | 抗震体系（11 种）    |
| Member        | -    | Beam          | 构件类型（由体系自动筛选） |
| SectionType   | -    | I-Shape       | 截面类型          |
| SectionSource | -    | AISC Database | 截面库或自定义       |
| Fy            | ksi  | 50            | 屈服强度          |
| E             | ksi  | 29000         | 弹性模量          |
| Ry            | -    | 1.1           | 期望屈服比（自动推荐）   |
| Method        | -    | LRFD          | LRFD/ASD      |
| Pu/Pa         | kips | 0             | 轴力（用于 Ca 计算）  |
| Ag            | in²  | 10            | 毛截面面积         |

**计算流程：**

1. **延性等级判定**：根据（体系, 构件）映射到 Highly Ductile / Moderately Ductile / None
2. **材料参数**：√(E/(Ry·Fy))
3. **截面尺寸**：按截面类型提取 b/t、h/t、D/t
4. **轴力比 Ca**：LRFD: Ca = Pu/(φc·Py)，ASD: Ca = Ωc·Pa/Py（Ca < 0 时取 0）
5. **翼缘校核**（Unstiffened Element）：Table D1.1 各截面 λhd/λmd
6. **腹板校核**（Stiffened Element）：含 Ca 依赖公式（I-Shape/Box: 2.57(1-1.04Ca)/0.88(2.68-Ca) ≥ 1.57）、Footnote [b]（SMF/IMF 梁限幅）
7. **综合判定**：翼缘 AND 腹板均通过 → SEISMICALLY COMPACT

**输出：** 延性等级、Ca 计算过程、翼缘/腹板实际值 vs 限值、PASS/FAIL。

---

### 15. SMF 强柱弱梁校核 (StrongColumnWeakBeamView)

AISC 341-16 §E3.4a 特殊抗弯框架（SMF）强柱弱梁（柱梁弯矩比）验算。

**截面来源：** 柱和梁均支持 AISC 截面库（W-shape，自动填充 Zc/Ag）或直接输入。

**输入参数：**

| 参数              | 单位     | 默认值          | 说明                                       |
| --------------- | ------ | ------------ | ---------------------------------------- |
| Method          | -      | LRFD         | LRFD/ASD（决定 αs = 1.0/1.5）                |
| Column Source   | -      | Direct Input | AISC Database / Direct Input             |
| Zc              | in³    | 200          | 柱塑性截面模量（Database 自动填充）                   |
| Ag              | in²    | 30           | 柱毛截面面积（Database 自动填充）                    |
| Fyc             | ksi    | 50           | 柱屈服强度                                    |
| Pr              | kips   | 200          | 柱所需轴压强度                                  |
| SameColumnBelow | -      | true         | 下柱同上柱                                    |
| Beam Source     | -      | Direct Input | AISC Database / Direct Input             |
| Mpr             | kip-in | 15000        | 塑性铰最大可能弯矩（Database 自动计算 = Cpr×Ry×Fyb×Zx） |
| Mv              | kip-in | 500          | 剪力放大附加弯矩（手动输入）                           |

**计算流程：**

1. **设计方法**：LRFD（αs=1.0）或 ASD（αs=1.5）
2. **柱参数**：截面库选型自动填充 Zc=Zx、Ag，支持屋面节点（上柱为空）和底座节点（下柱为空）
3. **梁参数**：截面库选型自动计算 Mpr=Cpr×Ry×Fyb×Zx，Mv 手动输入
4. **柱名义抗弯强度**：ΣM\*pc = ΣZc×max(0, Fyc−αs·Pr/Ag)（Eq. E3-2，含 Pr≥0 截断和压溃保护）
5. **梁预期抗弯强度**：ΣM\*pb = Σ(Mpr+αs·Mv)（Eq. E3-3）
6. **弯矩比校核**：ΣM\*pc/ΣM\*pb > 1.0（Eq. E3-1）
7. **例外情况检查**：Pc=Fyc·Ag/αs（Eq. E3-5），Pr/Pc<0.3 判定，含顶层节点自动豁免
8. **校核总结**：PASS/FAIL，含改进建议

**输出：** 完整计算书（含每步公式引用、规范条文号）、比值、PASS/FAIL。

---

### 16. 梁侧向稳定支撑验算 (BeamStabilityBracingView)

AISC 341-16 §D1.2 + AISC 360-16 Appendix 6 梁侧向与抗扭支撑强度及刚度需求计算。

**截面来源：** AISC 截面库（W-shape，自动填充 Zx, ry, ho, Iy, tw）或自定义输入。

**支持的抗震体系（5 种）：** SMF, IMF, SCBF, EBF, BRBF

**支持的构件类型：** Beam（所有体系）、Link（仅 EBF，高延性）

**输入参数：**

| 参数             | 单位     | 默认值           | 说明                                |
| -------------- | ------ | ------------- | --------------------------------- |
| System         | -      | SMF           | 抗震体系                              |
| Member         | -      | Beam          | 构件类型（EBF 可选 Link）                 |
| Method         | -      | LRFD          | LRFD/ASD（决定 αs = 1.0/1.5）         |
| Zx             | in³    | —             | 塑性截面模量（Database 自动填充）             |
| ry             | in     | —             | 弱轴回转半径（近似计算）                      |
| ho             | in     | —             | 翼缘形心间距（= d - tf）                  |
| Iy             | in⁴    | —             | 弱轴惯性矩（近似计算）                       |
| tw             | in     | —             | 腹板厚度                              |
| Fy             | ksi    | 50            | 屈服强度                              |
| E              | ksi    | 29000         | 弹性模量                              |
| Ry             | -      | 1.1           | 期望屈服比（Auto: Fy≤36→1.5, 否则→1.1）    |
| Lb             | in     | —             | 实际无支撑长度                           |
| BraceType      | -      | Nodal Lateral | Nodal/Relative Lateral, Torsional |
| AtPlasticHinge | -      | false         | 是否位于塑性铰区域                         |
| Mr             | kip-in | Auto          | 所需弯矩（Auto = Ry·Fy·Zx/αs）          |
| Cd             | -      | 1.0           | 曲率系数（AISC 341 规定 1.0）             |

**抗扭支撑附加参数：**

| 参数                 | 单位  | 默认值   | 说明                        |
| ------------------ | --- | ----- | ------------------------- |
| Iy,eff             | in⁴ | Iy    | 有效弱轴惯性矩（双对称 I-shape = Iy） |
| Cb                 | -   | 1.0   | 弯矩梯度系数（塑性铰区建议 1.0）        |
| L                  | in  | —     | 梁跨度                       |
| n                  | -   | 1     | 支撑数量                      |
| FullDepthStiffener | -   | false | 全高加劲肋（βsec = ∞）           |
| tst                | in  | —     | 加劲肋厚度（非全高时）               |
| bs                 | in  | —     | 加劲肋总宽度（双侧之和）              |

**计算流程：**

1. **延性等级判定**：根据（体系, 构件）映射 → SMF Beam: HD / IMF Beam: MD / EBF Link: HD / 其他 Beam: MD
2. **最大无支撑长度**：MD: Lb,max = 0.19·ry·E/(Ry·Fy)（Eq. D1-2）；HD: Lb,max = 0.095·ry·E/(Ry·Fy)（§D1.2b）
3. **Lb ≤ Lb,max 校核**
4. **Mr 确定**：塑性铰区强制 Mr = Ry·Fy·Zx/αs（D1-6）；常规区默认 D1-1 公式，支持用户自定义
5. **支撑强度需求**：
   - 塑性铰区侧向：Pr = 0.06·Mr/ho（D1-4）
   - 塑性铰区抗扭：Mbr = 0.06·Ry·Fy·Zx/αs（D1-5）
   - 常规节点侧向：Pbr = 0.02·Mr·Cd/ho（A-6-7）
   - 常规相对侧向：Vbr = 0.01·Mr·Cd/ho（A-6-5）
   - 常规抗扭：Mbr = 0.02·Mr（A-6-9）
6. **支撑刚度需求**：
   - 侧向节点：βbr = (1/φ)·10·Mr·Cd/(Lb·ho)（A-6-8，φ=0.75, Ω=2.0）
   - 侧向相对：βbr = (1/φ)·4·Mr·Cd/(Lb·ho)（A-6-6）
   - 抗扭：βT = (1/φ)·2.4·L/(n·E·Iy,eff)·(Mr/Cb)²（A-6-11，Ω=3.0）
   - 抗扭截面刚度：βsec（A-6-12，全高加劲肋 βsec=∞）
   - 组合：βbr = βT/(1-βT/βsec)（A-6-10）

**输出：** 延性等级、Lb 限值校核（PASS/FAIL）、支撑所需强度、支撑所需刚度、完整计算书（含规范条文号和公式推导）。

---

### 17. 梁柱节点剪切区验算 (PanelZoneView)

AISC 341-16 §E3.6e + AISC 360-16 §J10.6 梁柱节点域剪切强度验算与补强板设计。

**截面来源：** 柱和梁均支持 AISC 截面库（W-shape）或自定义输入。

**支持的抗震体系（3 种）：** SMF, IMF, OMF

**输入参数：**

| 参数                    | 单位     | 默认值   | 说明                              |
| --------------------- | ------ | ----- | ------------------------------- |
| Method                | -      | LRFD  | LRFD/ASD                        |
| System                | -      | SMF   | SMF / IMF / OMF                 |
| Column dc/bcf/tcf/tcw | in     | —     | 柱截面几何参数（Database 自动填充）          |
| Fyc                   | ksi    | 50    | 柱屈服强度                           |
| Ag                    | in²    | —     | 柱毛截面面积                          |
| Beam db/tfb           | in     | —     | 左/右梁截面参数（Database 自动填充）         |
| Zx                    | in³    | —     | 梁塑性截面模量（SMF/IMF）                |
| Fyb                   | ksi    | 50    | 梁屈服强度（SMF/IMF）                  |
| Ry                    | -      | Auto  | 期望屈服比（Auto: Fy≤36→1.5, 否则→1.1）  |
| Cpr                   | -      | 1.15  | 塑性铰超强系数                         |
| Mpr/Mf                | kip-in | Auto  | 塑性铰弯矩（SMF/IMF 自动）/ 分析弯矩（OMF 手动） |
| Hc                    | in     | 156   | 层高（梁中线间距）                       |
| Pr                    | kips   | 0     | 柱所需轴压强度                         |
| HasRightBeam          | -      | false | 是否有右梁（内节点）                      |

**计算流程（4 步）：**

1. **剪切需求 Ru**：
   
   - SMF/IMF（能力设计法）：Mpr = Cpr × Ry × Fyb × Zx，翼缘力 Pf = Mpr / (db - tfb)
   - OMF（分析荷载法）：用户直接输入 Mf，Pf = Mf / (db - tfb)
   - 柱剪力 Vc = ΣMf / Hc（子结构整体平衡）
   - 节点域剪力 Ru = ΣPf - Vc

2. **剪切承载力 Rn**：
   
   - Pr ≤ 0.4Py（Eq. J10-11）：Rn = 0.6 × Fyc × dc × tw × (1 + 3 × bcf × tcf² / (dc × tw × db_avg))
   - Pr > 0.4Py（Eq. J10-12）：含轴力折减项
   - 设计承载力：LRFD φ=0.90 / ASD Ω=1.67

3. **最小厚度校核**（SMF/IMF only）：
   
   - tmin = (dz + wz) / 90（§E3.6e(3)），OMF 跳过

4. **补强板设计**：
   
   - 强度需求厚度：由 Rn 公式反推 t_total，Tdp = t_total - tcw
   - 屈曲需求厚度：Tdp = tmin - tcw（SMF/IMF 且腹板过薄时）
   - 取两者较大值，推荐标准板厚
   - 塞焊检查：tcw < tdp 时需要塞焊连接

**输出：** 完整计算书（含每步公式引用、规范条文号）、Ru/Rn 比值、补强板厚度推荐、PASS/FAIL。

---

### 18. RBS 连接验算 (RbsView)

AISC 358-16 Chapter 5 削弱梁截面（Reduced Beam Section）连接设计验算。

**截面来源：** AISC 截面库（W-shape）或自定义输入（d, bf, tf, tw, Zx）。

**输入参数：**

| 参数            | 单位    | 默认值        | 说明         |
| ------------- | ----- | ---------- | ---------- |
| Span          | in    | 360        | 梁跨度（中到中）   |
| SystemType    | -     | SMF        | SMF 或 IMF  |
| a, b, c       | in    | 5, 20, 1.5 | RBS 切割几何参数 |
| D, L, S       | kips  | 0          | 重力荷载       |
| Beam Fy/Fu/Ry | ksi/- | 50/65/1.1  | 梁材料属性      |
| Col Fy/Ry     | ksi/- | 50/1.1     | 柱材料属性      |

**计算流程（Section 5.8，11 步）：**

1. RBS 几何限值（Eq. 5.8-1~5.8-3：0.5bf≤a≤0.75bf, 0.65d≤b≤0.85d, 0.1bf≤c≤0.25bf）
2. Z_RBS = Zx - 2·c·tf·(d-tf)（Eq. 5.8-4）
3. C_pr = min((Fy+Fu)/(2Fy), 1.2)，M_pr = C_pr·Ry·Fy·Z_RBS（Eq. 5.8-5）
4. V_RBS = 2·M_pr/L_h + V_gravity
5. S_h = a+b/2, M_f = M_pr + V_RBS·S_h（Eq. 5.8-6）
6. M_pe = Ry·Fy·Zx（Eq. 5.8-7）
7. 抗弯验算：M_f ≤ φ_d·M_pe（Eq. 5.8-8）
8. 抗剪验算（AISC 360 Chapter G）
9. 腹板连接要求（SMF: CJP; IMF: 螺栓单剪板允许）
10. 连续板要求（Chapter 2）
11. 强柱弱梁验算（AISC 341 E3.6c）

---

### 19. End-Plate 连接验算 (EndplateView)

AISC 358-16 Chapter 6 端板连接设计验算，支持三种连接类型。

**截面来源：** AISC 截面库（W-shape）或自定义输入（d, bf, tf, tw, Zx）。

**输入参数：**

| 参数              | 单位  | 默认值        | 说明                  |
| --------------- | --- | ---------- | ------------------- |
| ConnectionType  | -   | 4E         | 4E / 4ES / 8ES      |
| BoltDb          | in  | 1.0        | 螺栓直径                |
| BoltGrade       | -   | A325       | A325 / A490 / F1852 |
| PlateBp/Tp      | in  | 8/0.75     | 端板宽度/厚度             |
| G, Pfo, Pfi     | in  | 4/1.25/1.5 | 栓钉间距/外螺距/内螺距        |
| StiffenerTs/Lst | in  | 0.5/5.0    | 加劲肋（4ES/8ES）        |

**计算流程（Section 6.8，13 步）：**

1. M_f = M_pr + V_u·S_h（4E: S_h=min(d/2,3bf); 4ES/8ES: S_h=Lst+tp）
2. 连接几何（h_i 距离，从受压翼缘中心线量起）
3. 所需螺栓直径（Eq. 6.8-3/6.8-4）
4. 所需板厚（Eq. 6.8-5，Y_p 由 Tables 6.2~6.4 屈服线参数计算）
5. 梁翼缘力 F_fu = M_f/(d-tf)（Eq. 6.8-6）
6. 板剪切屈服/断裂验算（4E: Eq. 6.8-7/6.8-8）
7. 加劲肋设计（4ES/8ES: Eq. 6.8-9/6.8-10）
8. 螺栓剪切（Eq. 6.8-11）、承压/撕裂、焊接
9. 柱侧：柱翼缘弯曲屈服（Eq. 6.8-13）

---

### 20. BFP 连接验算 (BfpView)

AISC 358-16 Chapter 7 螺栓翼缘板（Bolted Flange Plate）连接设计验算。

**截面来源：** AISC 截面库（W-shape）或自定义输入（d, bf, tf, tw, Zx）。

**输入参数：**

| 参数         | 单位  | 默认值   | 说明                 |
| ---------- | --- | ----- | ------------------ |
| PlateBp/Tp | in  | 9/0.5 | 翼缘板宽度/厚度           |
| BoltDb     | in  | 1.0   | 螺栓直径（≤1.125）       |
| BoltGrade  | -   | A490  | A490 或 F2280（仅此两种） |
| NBolts     | -   | 6     | 每侧螺栓数（偶数）          |
| S1, S      | in  | 3/3   | 首排距/间距             |
| PlateFy/Fu | ksi | 50/65 | 板材料（A572Gr50/A36）  |
| Beam Rt    | -   | 1.2   | 梁抗拉超强比             |

**计算流程（Section 7.6，17 步）：**

1. 资格预审（d≤36, weight≤150, tf≤1.0, L/d≥9/7, 仅 A490/F2280, 板Fy≤55）
2. 最大螺栓直径（Eq. 7.6-2）
3. 控制剪切强度 r_n = min(Fnv·Ab, 2.4·Fub·db·tf, 2.4·Fup·db·tp)（Eq. 7.6-3）
4. 试算螺栓数（Eq. 7.6-4）
5. S_h = S1+s·(n/2-1)（Eq. 7.6-5）
6. V_h, M_f（Eq. 7.6-6）
7. F_pr = M_f/(d+tp) — **注意：用 d+tp，非 d-tf**（Eq. 7.6-7）
8. 确认螺栓数（Eq. 7.6-8）
9. 板屈服/断裂验算（Eq. 7.6-9/7.6-10）
10. 块剪验算（Eq. 7.6-11）
11. 压缩屈曲验算（Eq. 7.6-12，KL=0.65·S1）
12. 梁剪切强度（Eq. 7.6-13）
13. 连续板 + 节点域验算

---

### 21. WUF-W 连接验算 (WufwView)

AISC 358-16 Chapter 8 焊接翼缘连接（Welded Unreinforced Flange-Welded Web）设计验算。

**截面来源：** AISC 截面库（W-shape）或自定义输入（d, bf, tf, tw, Zx）。

**输入参数：**

| 参数            | 单位    | 默认值       | 说明        |
| ------------- | ----- | --------- | --------- |
| Span          | in    | 300       | 梁跨度       |
| SystemType    | -     | SMF       | SMF 或 IMF |
| D, L, S       | kips  | 0         | 重力荷载      |
| Vu            | kips  | 0         | 用户指定剪力    |
| Beam Fy/Fu/Ry | ksi/- | 50/65/1.1 | 梁材料       |
| Col Fy/Ry     | ksi/- | 50/1.1    | 柱材料       |

**WUF-W 特有参数（固定值，非用户输入）：**

- C_pr = 1.4（非 Eq. 2.4-2）
- S_h = 0（塑性铰在柱面）
- M_f = M_pr（无剪力放大）

**计算流程（Section 8.7，8 步）：**

1. M_pr = 1.4·Ry·Fy·Zx
2. S_h=0, M_f=M_pr
3. V_h = 2·M_pr/L_h + V_gravity/2
4. 强柱弱梁验算（SMF: M_pc/M_pb* ≥ 1.0）
5. 梁剪切强度（φ_v=1.0）
6. 连续板（tcf_req = √(Ff/(0.9·6.25·Fyc))）
7. 节点域（含翼缘贡献）
8. 连接细部（CJP 焊缝、焊接孔、剪切板）

---

### 22. KBB 连接验算 (KbbView)

AISC 358-16 Chapter 9 Kaiser Bolted Bracket 支架连接设计验算。

**截面来源：** AISC 截面库（W-shape）或自定义输入（d, bf, tf, tw, Zx）。

**输入参数：**

| 参数                   | 单位  | 默认值           | 说明                               |
| -------------------- | --- | ------------- | -------------------------------- |
| BracketSeries        | -   | W             | W 系列（焊接）或 B 系列（螺栓）               |
| BracketModel         | -   | W3.0          | 支架型号（W: W3.0~W1.0, B: B2.1/B1.0） |
| Span                 | in  | 300           | 梁跨度                              |
| SystemType           | -   | SMF           | SMF 或 IMF                        |
| Beam/Col Fy/Fu/Ry/Rt | ksi | 50/65/1.1/1.2 | 材料属性                             |

**计算流程（Section 9.9，18 步）：**

1. 资格预审（梁 d≤W33, weight≤130plf, tf≤1.0, bf≥6"/10", L/d≥9, 柱 bf≥12"）
2. M_pr, V_h, M_f
3. 支架选择 + d_eff（两组螺栓质心距）
4. 柱螺栓抗拉强度（Eq. 9.9-2/9.9-3）
5. 最小柱翼缘宽度/厚度（消除撬力，Eq. 9.9-4~9.9-7）
6. 连续板要求
7. B 系列：梁螺栓抗剪 + 块剪验算
8. W 系列：角焊缝验算
9. 梁剪切 + 节点域

---

### 23. ConXL 连接验算 (ConxlView)

AISC 358-16 Chapter 10 ConXL 矩钢管混凝土柱连接设计验算。

**截面来源：** 梁使用 AISC 截面库；柱为 16" 方钢管混凝土（固定尺寸，手动输入壁厚/材料）。

**输入参数：**

| 参数               | 单位   | 默认值      | 说明              |
| ---------------- | ---- | -------- | --------------- |
| Col_t            | in   | 0.5      | 方钢管壁厚           |
| ColFy/Fu         | ksi  | 50/62    | 钢管材料（A500 Gr C） |
| fc               | ksi  | 4.0      | 混凝土抗压强度         |
| ConcreteWeight   | pcf  | 145      | 混凝土容重           |
| t_leg_CC         | in   | 0.75     | 牛腿角有效肢厚         |
| UseRbs           | -    | false    | 是否使用 RBS        |
| RBS a/b/c        | in   | 5/20/1.5 | RBS 切割参数（可选）    |
| Span/Lh          | in   | 300/240  | 跨度/净跨           |
| StoryAbove/Below | in   | 156/156  | 上下层高            |
| Pu               | kips | 0        | 柱轴力             |

**计算流程（Section 10.8，12 步）：**

1. 资格预审（梁 d∈W18~W30, tf≤1.0, bf≤12, L/d≥7/5, 柱 16" 方, f'c≥3ksi）
2. M_pr（非 RBS: C_pr=1.1; RBS: Eq. 2.4-2）
3. V_h, 柱-梁弯矩比（含混凝土贡献 0.85·Ac·fc）
4. M_bolts（牛腿螺栓处弯矩）
5. 牛腿螺栓抗拉（A574 预紧力 102 kips）
6. 滑移临界抗剪（16 螺栓, Class B）
7. 梁剪切强度
8. CWX 角焊缝 + 牛腿角焊缝
9. 节点域（含牛腿角肢贡献）

---

### 24. SidePlate 连接验算 (SideplateView)

AISC 358-16 Chapter 11 SidePlate 侧板连接设计验算。

**截面来源：** AISC 截面库（W-shape）或自定义输入。

**输入参数：**

| 参数                | 单位  | 默认值       | 说明                     |
| ----------------- | --- | --------- | ---------------------- |
| ConnectionType    | -   | welded    | welded（焊接）或 bolted（螺栓） |
| Lsp               | in  | 0         | 侧板延伸长度（0=0.77d 自动）     |
| Span              | in  | 360       | 梁跨度                    |
| SystemType        | -   | SMF       | SMF 或 IMF              |
| StoryAbove/Below  | in  | 156/156   | 上下层高                   |
| Beam/Col Fy/Fu/Ry | ksi | 50/65/1.1 | 材料属性                   |

**计算流程（Section 11.8，8 步）：**

1. 几何兼容性 + 侧板延伸范围检查
2. 框架建模参数（100% 刚性偏移）
3. 梁资格预审（welded: d≤W40, bolted: d≤W44）
4. 柱资格预审（d≤W44）
5. 设计力（welded: 塑性铰 d/3; bolted: d/6）
6. 柱-梁弯矩比（Eq. 11.4-2~11.4-5）
7. 梁剪切强度
8. 节点域（侧板作为加劲肋）

---

### 25. SST 连接验算 (SstView)

AISC 358-16 Chapter 12 Simpson Strong-Tie Yield-Link 连接设计验算。

**截面来源：** AISC 截面库（W-shape）或自定义输入。

**输入参数：**

| 参数            | 单位          | 默认值   | 说明                          |
| ------------- | ----------- | ----- | --------------------------- |
| LinkType      | -           | tstub | tstub（T-Stub）或 endplate（端板） |
| t_stem        | in          | 0.75  | Yield-Link 茎板厚度             |
| b_col_side    | in          | 0     | 柱侧宽度（0=自动）                  |
| b_bm_side     | in          | 0     | 梁侧宽度（0=自动）                  |
| b_yield       | in          | 0     | 屈服段宽度（0=自动）                 |
| L_col/bm_side | in          | 0/0   | 柱/梁侧长度（0=自动）                |
| L_y_link      | in          | 0     | 最小屈服长度（0=自动）                |
| a_dist        | in          | 3.0   | 剪切螺栓到柱面距离                   |
| Span          | in          | 300   | 梁跨度                         |
| Vu/Mu/Pu_sp   | kips/kip-in | 0     | 荷载参数                        |
| LinkFy/Fu     | ksi         | 50/65 | Yield-Link 材料属性             |

**计算流程（Section 12.9，19 步）：**

1. 资格预审（tstub: d≤W36; endplate: W8~W12, tf≥0.40"）
2. Yield-Link 截面设计（Eqs. 12.9-1~12.9-6）
3. 梁侧螺栓布置（Eq. 12.9-7）
4. 翼缘-柱连接（T-stub/endplate 螺栓抗拉, Eqs. 12.9-8~12.9-12）
5. 屈曲约束组件 BRP（Eqs. 12.9-13~12.9-23）
6. 连接刚度 K1/K2/K3/K_eff（Eqs. 12.9-24~12.9-33）
7. 所需剪力 + 构件验算
8. 柱-梁弯矩比（SMF: AISC 341 E3.6c）
9. 剪切板连接 + 节点域（φ=0.90）
10. 柱翼缘弯曲屈服 + 连续板

---

### 26. DoubleTee 连接验算 (DoubleteeView)

AISC 358-16 Chapter 13 双 T 连接（T-Stub 螺栓连接）设计验算。

**截面来源：** AISC 截面库（W-shape）或自定义输入。

**输入参数：**

| 参数                    | 单位       | 默认值         | 说明           |
| --------------------- | -------- | ----------- | ------------ |
| NTb                   | -        | 4           | 受拉螺栓数（4 或 8） |
| BoltType              | -        | A325        | A325 或 A490  |
| S1/s_vb/g_vb/g_tb     | in       | 3/3/3.5/5.5 | 螺栓布置参数       |
| T-stub t_st/t_ft/b_ft | in       | 0.5/1.0/8   | T-Stub 截面几何  |
| HasSlab               | -        | false       | 是否有混凝土板      |
| Span                  | in       | 360         | 梁跨度          |
| SystemType            | -        | SMF         | SMF 或 IMF    |
| StoryAbove/Below      | in       | 156/156     | 上下层高         |
| Pu/As_col             | kips/in² | 0/0         | 柱轴力/面积       |

**计算流程（Section 13.6，23 步）：**

1. 资格预审（d≤24", weight≤55plf, tf≤5/8", L/d≥9）
2. M_pr + 最大剪切螺栓直径（Eq. 13.6-3）
3. 每螺栓剪切强度 r_nv = min(螺栓剪切, 梁承压, T 茎承压)
4. 剪切螺栓数量 + 塑性铰位置
5. V_h + M_f + 柱-梁弯矩比
6. T-Stub 力 F_pr = M_f/(1.05·d)
7. T 茎尺寸（Whitmore 宽度, 屈服/断裂/屈曲）
8. 受拉螺栓直径 + T 翼缘撬力验算（三种破坏模式）
9. FR 连接刚度检查（K_i ≥ 18EI/L_o）
10. 实际翼缘力 + 回算验证
11. 承压/撕裂 + 块剪
12. 柱翼缘弯曲屈服（屈服线理论）+ 节点域 + 连续板

---

### 27. SlottedWeb 连接验算 (SlottedwebView)

AISC 358-16 Chapter 14 开槽腹板连接设计验算（仅限 SMF）。

**截面来源：** AISC 截面库（W-shape）或自定义输入。

**输入参数：**

| 参数               | 单位       | 默认值           | 说明                |
| ---------------- | -------- | ------------- | ----------------- |
| Beam T           | in       | 0             | 翼缘间净距（0=d-2tf 自动） |
| ShearPlate_lp    | in       | 0             | 剪切板宽度（0=自动计算）     |
| Span             | in       | 360           | 梁跨度（SMF only）     |
| StoryAbove/Below | in       | 156/156       | 上下层高              |
| Pu/As_col        | kips/in² | 0/0           | 柱轴力/面积            |
| Beam Fy/Fu/Ry/Rt | ksi      | 50/65/1.1/1.1 | 梁材料属性             |
| Col Fy           | ksi      | 50            | 柱材料属性             |

**计算流程（Section 14.8，10 步）：**

1. 资格预审（SMF only, d≤36", weight≤400plf, tf≤2.25", L/d≥6.4）
2. 梁腹板槽设计（ls 取四个标准的最小值, Eqs. 14.8-1~14.8-4）
3. 剪切板设计（宽度/高度/厚度, Eqs. 14.8-5/14.8-6）
4. 剪切板-梁腹板焊缝（M_weld, V_weld, e_x, Eqs. 14.8-7~14.8-11）
5. 剪切板-柱翼缘焊缝
6. 安装螺栓（直径 ≥ tw, 间距 ≤ 6"）
7. M_f at 柱面（Eq. 14.8-12）
8. 梁剪切强度（φ_v=1.0 per Commentary C-14.8）
9. 连续板 + 节点域
10. 柱-梁弯矩比（Eq. 14.4-1）

---

## 构建与运行

```bash
dotnet restore USCodeTools.csproj
dotnet build USCodeTools.csproj
dotnet run --project USCodeTools.csproj
```

**发布：**

```bash
dotnet publish USCodeTools.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

---

## 相关规范

| 规范           | 说明                                                                              |
| ------------ | ------------------------------------------------------------------------------- |
| ASCE 7-16    | 荷载标准（地震、风）                                                                      |
| ACI 318-19   | 混凝土建筑规范（柱、板、锚固）                                                                 |
| ACI 318-25   | 混凝土建筑规范最新版（梁设计）                                                                 |
| AISC 360-16  | 钢结构建筑规范                                                                         |
| AISC 358-16  | 预认证抗震连接规范（RBS、End-Plate、BFP、WUF-W、KBB、ConXL、SidePlate、SST、DoubleTee、SlottedWeb） |
| AISC 341-16  | 钢结构抗震规定（强柱弱梁、节点域剪切区、延性分类、梁侧向支撑）                                                 |
| GB50011-2010 | 建筑抗震设计规范                                                                        |
| GB50009-2012 | 建筑结构荷载规范                                                                        |

---

**维护者：** bg5hot
**许可：** 个人学习和研究使用

---

## 代码统计

### 各模块代码行数

| #   | 模块              | Calculations | ViewModel | View (XAML) | View (.cs) | 小计         |
| --- | --------------- | ------------ | --------- | ----------- | ---------- | ---------- |
| 1   | 反应谱比较           | 239*         | 227       | 158         | 12         | 636        |
| 2   | 地震人工波模拟         | 626          | 561       | 212         | 13         | 1,412      |
| 3   | 风速转换            | —            | 106       | 117         | 12         | 235        |
| 4   | 阵风响应因子 G        | 162          | 202       | 161         | 12         | 537        |
| 5   | 脉动风模拟           | 262          | 348       | 95          | 13         | 718        |
| 6   | 单筋混凝土梁          | 506          | 424       | 284         | 12         | 1,226      |
| 7   | 混凝土矩形柱          | 639          | 423       | 334         | 12         | 1,408      |
| 8   | 混凝土圆形柱          | 424          | 371       | 252         | 12         | 1,059      |
| 9   | 双向板抗冲切          | 679          | 284       | 227         | 12         | 1,202      |
| 10  | 钢筋锚固长度          | 399          | 302       | 229         | 12         | 942        |
| 11  | 钢构件验算           | 735          | 573       | 235         | 12         | 1,555      |
| 12  | 钢构件抗拉           | 571          | 644       | 309         | 10         | 1,534      |
| 13  | 钢构件抗压           | 589          | 479       | 253         | 10         | 1,331      |
| 14  | 延性分类与宽厚比校核      | 991          | 544       | 299         | 12         | 1,846      |
| 15  | SMF 强柱弱梁校核      | 421          | 359       | 335         | 12         | 1,127      |
| 16  | 梁侧向稳定支撑验算       | 444          | 318       | 358         | 12         | 1,132      |
| 17  | 梁柱节点剪切区验算       | 511          | 374       | 438         | 12         | 1,335      |
| 18  | RBS 连接验算        | 528          | 295       | 310         | 12         | 1,145      |
| 19  | End-Plate 连接验算  | 797          | 463       | 443         | 12         | 1,715      |
| 20  | BFP 连接验算        | 616          | 678       | 494         | 12         | 1,800      |
| 21  | WUF-W 连接验算      | 423          | 506       | 310         | 12         | 1,251      |
| 22  | KBB 连接验算        | 916          | 722       | 483         | 12         | 2,133      |
| 23  | ConXL 连接验算      | 729          | 572       | 430         | 12         | 1,743      |
| 24  | SidePlate 连接验算  | 744          | 623       | 447         | 12         | 1,826      |
| 25  | SST 连接验算        | 948          | 718       | 549         | 12         | 2,227      |
| 26  | DoubleTee 连接验算  | 1,145        | 790       | 589         | 12         | 2,536      |
| 27  | SlottedWeb 连接验算 | 615          | 611       | 462         | 12         | 1,700      |
|     | **模块小计**        |              |           |             |            | **37,311** |

\* 模块 1 的 CoreCalculations.cs 与模块 3 共享（风速转换计算），239 行计入模块 1。

### 公共文件

| 文件                              | 行数      | 说明                 |
| ------------------------------- | ------- | ------------------ |
| MainWindow.xaml                 | 291     | 主窗口导航              |
| MainWindow.xaml.cs              | 180     | 导航事件处理             |
| Styles.xaml                     | 124     | Fluent Design 全局样式 |
| Aisc358ShapeHelper.cs           | 156     | AISC 358 共享截面加载器   |
| RelayCommand.cs                 | 30      | ICommand 实现        |
| BooleanToVisibilityConverter.cs | 28      | 可见性转换器             |
| InverseBoolConverter.cs         | 55      | 布尔取反转换器            |
| **公共文件小计**                      | **808** |                    |

### 总计

| 类别       | 行数         |
| -------- | ---------- |
| 27 个计算模块 | 37,311     |
| 公共文件     | 808        |
| **总计**   | **38,119** |
