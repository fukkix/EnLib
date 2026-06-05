# 《长夜书藏》/ EnLib — 设计定稿 v0.1

> 本文件是开发期的设计宪法。所有代码以此为准；改动需在此文件先落定。
> 世界观完全继承 idleditor《永夜出版社》。

---

## 0. 一句话

你是一名前永夜出版社编辑，吸血鬼，刚接手南港旧馆的修复师工作 ——
而上任馆长在你眼前去世，把整座馆留给了你。

无失败、无截止。驱动力来自**收藏完整度**与**好奇心**。

---

## 1. 玩法核心

借鉴《密教模拟器》《司辰之书》的拖卡+verb 系统：

- **卡牌 = 一切**：典籍、知识、工具、材料、状态、访客、契机
- **Verb（工作台）+ 拖拽合成**：拖卡入槽，倒计时跑完，触发 outcome
- **Aspect（相）+ 阈值 + 加权随机**：配方靠 aspect 匹配，结果按 weight 抽
- **实时 + 可暂停**：空格暂停，鼓励"思考-布置-继续"
- **无失败**：血饥不致死，时间不结算

---

## 2. 三层空间

| 层 | 用途 | 卡量 |
|---|---|---|
| **L1 工作桌面 Workspace** | 当前处理的卡 + 5 个工作台 | ≤ 30~50 张 |
| **L2 待处理抽屉 Inbox** | 所有未启动的卡（列表视图，可筛选） | 数百张 |
| **L3 馆藏书架 Stacks** | 已上架的成套书（只读展示） | 无上限 |

---

## 3. 五个工作台（Verb）

| Verb | 槽位 | 输入 | 产出 |
|---|---|---|---|
| **辨识 Identify** | 1 典籍 + 0~3 知识 | 未辨识典籍 | `identified=true`，揭示标签 |
| **修复 Restore** | 1 典籍 + 1~3 工具 + 1~5 材料 | 损伤典籍 | condition +1 档 |
| **整理 Sort** | 2~6 同 series 典籍 | 同系列书 | 检测套书 / 触发装函仪式 / 生成寻书契机 |
| **研究 Study** | 1 完好+ 典籍 (长耗时) | 高品相书 | 新知识卡 / 升级已有知识 |
| **访客台 Reception** | 1 访客 + 可选契机/典籍 | 访客卡 | 各种事件（送书/借书/补血/剧情） |

---

## 4. 卡牌系统

### 4.1 七种 Type（外观分类）

```
book        典籍   棕色
knowledge   知识   蓝色
tool        工具   灰色
material    材料   绿色
state       状态   紫色
visitor     访客   金色
lead        契机   红色
```

### 4.2 Aspect 列表（10~12 个，配方匹配的真正核心）

| Aspect | 中文 | 语义 |
|---|---|---|
| `sun` | 日 | 阳光、禁忌（idleditor 日光幻想） |
| `silver` | 银 | 神圣、腐蚀（银器恐怖） |
| `mortal` | 凡 | 凡人世界（凡间名著） |
| `ancient` | 古 | 远古、神话（远古纪事） |
| `night` | 夜 | 永夜本土（真实研究） |
| `craft` | 工 | 装帧修复工艺 |
| `lore` | 识 | 知识、学识 |
| `decay` | 朽 | 损伤、腐烂 |
| `blood` | 血 | 血族、血饥 |
| `memory` | 忆 | 回忆、历史 |
| `secret` | 秘 | 隐藏、禁忌 |

每张卡带一个 `Dictionary<Aspect, int>`。

### 4.3 典籍五档品相

```
fragment   残页（仅尺寸/年代）
damaged    破损（题材可辨）
worn       旧（标题/作者可辨）
good       完好（解锁 LLM 摘录）
pristine   精装（解锁封面/可入稀有架）
```

---

## 5. Recipe 结构

```yaml
id: restore_silver_book
verb: restore
requires:
  - aspect: decay, min: 1
  - aspect: craft, min: 2
  - aspect: silver, min_in_materials: "{book.silver}"   # 动态阈值
duration_sec: [30, 120]   # 范围按品相
outcomes:
  - id: jackpot
    weight: "(craft_total - 2) * 10"
    effect: condition += 2
    text_pool: restore.jackpot
  - id: success
    weight: 60
    effect: condition += 1
    text_pool: restore.success_silver
  - id: flaw
    weight: "(book.decay - 3) * 5"
    effect: condition += 0.5
    text_pool: restore.flaw
  - id: reveal
    weight: "max(0, (blood_thirst - 50) * 0.5)"
    effect: condition += 1; spawn: lead.random
    text_pool: restore.reveal
```

---

## 6. 时间与血饥

- **实时推进 + 暂停（空格）**
- **夜晚计数**：每 N 分钟实时 = 1 夜
- **血饥**：工作台运行累积。0~20 正常；20~50 -5% 速；50~80 瑕疵概率↑；80+ -20% 速
- **离线挂机**：仅推进已启动工作台，封顶 8 小时

---

## 7. 套书系统

- `seriesId` 共享，`volumeNo` 卷号，`seriesTotal` 总册
- 整理台检测序列 → 缺册生成寻书契机
- 契机投入访客台 → 提升对应卷流入权重
- 全卷 pristine → 装函仪式 → 入 L3

---

## 8. 开场（第一夜）

馆长 NPC 带你转一圈五个工作台 → 在【整理】台旁去世 → 留小纸条 →
游戏开始，桌上多三张永久遗物卡 + 一张【契机·关于卷七】红卡。

详见 GDD 后续章节（待补）。

---

## 9. 技术栈

- **Godot 4.6.2 .NET**
- **C# / .NET 8 SDK**
- 数据：YAML（编辑时）→ JSON（运行时加载）
- 存档：JSON
- UI：Godot Control 节点
- 国际化：Godot 内置（中英）
- Steam：GodotSteam 插件（后期接入）

---

## 10. 仓库结构

```
enlib/
├── DESIGN.md              # 本文件
├── project.godot          # Godot 项目入口
├── EnLib.csproj           # .NET 项目
├── EnLib.sln              # 解决方案
├── icon.svg
├── scenes/                # .tscn
├── scripts/               # .cs
│   ├── Core/              # Card, Aspect, Recipe, GameState
│   ├── View/              # CardView, VerbSlot, Workspace
│   └── Data/              # 数据加载
├── resources/             # .tres（卡牌定义、配方）
├── data/                  # YAML/JSON 数据（卡池、配方、套书）
│   └── seed/              # 从 idleditor 导出的母库
├── text/                  # 文案池 YAML
├── art/                   # 美术资源
│   └── covers/            # 复用 idleditor 封面 PNG
└── tools/                 # 开发工具脚本（从 idleditor 导出等）
```

---

## 11. 开发节奏（详见 PLAN.md）

```
Phase 0  环境与项目骨架
Phase 1  核心系统：拖卡 → 倒计时 → 产出
Phase 2  完整闭环：流入 → 辨识 → 修复 → 整理 → 上架
Phase 3  内容生产：配方、套书、知识树、血饥、离线
Phase 4  叙事：开场剧情、文案池、主线钩子
Phase 5  打磨：美术、音频、Steamworks、商店页
```

当前阶段：**Phase 0 / Phase 1 交界**。
