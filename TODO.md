# TODO

## 控制台（ConsoleUI / ConsoleController）待办

当前实现走 **Unity 内置 IMGUI（`OnGUI`）**，不依赖任何第三方 UI 包：
` 开关、Esc 关闭、回车执行、滚动输出区（有新输出时跟随到最底）、反射自动注册 `ICommand`、
`error: ` 前缀染红、打开时用 `GameplayInputGate` 压制游戏输入。

以下特性先记录，暂不实现：

- [ ] **命令历史**：↑↓ 翻历史命令。要在画 `GUI.TextField` 之前截获 `KeyCode.UpArrow` / `DownArrow` 并 `Event.current.Use()`，否则方向键会被输入框用来移动光标。
- [ ] **Tab 补全**：唯一匹配直接补全，多个匹配列出候选；`ConsoleController.Commands` 已经是公开的指令表，用它的 `Keys` 即可。
- [x] **help / usage**：`ICommand` 加了实例属性 `Usage`（用法串），内置 `help` 列出全部指令名、`help <指令名>` 打印单条 `Usage`。`help` 需要读指令表，由 `ConsoleController.Awake` 显式注册，走不了反射自动发现。`Description`（一句话简介）仍未做，列表里只有名字。
- [ ] **捕获 Unity 日志**：订阅 `Application.logMessageReceived`，把 `Debug.Log/Warning/Error` 也显示到输出区（按级别着色）。需要级别信息，所以 `Line.IsError` 要换成级别枚举。
- [ ] **窗口交互**：可拖动、可拉伸、可折叠。现在靠 `heightRatio` 定高、顶部贴边；IMGUI 里可改用 `GUILayout.Window` 换成真窗口。
- [ ] **引号参数**：`Tokenize` 现在只按空白切分，需要支持 `"a b"` 这类带空格的参数。
- [ ] **指令别名**：`ICommand` 目前只有单个 `Name`。
- [ ] **Toggle 键可配置**：现在写死 `KeyCode.BackQuote`。
- [ ] **精细的滚动跟随**：现在只在有新输出时拉到底，用户手动上滚不会被拽回；但也没法判断「用户已经滚回底部」。要做完整需要自己算内容高度（`GUIStyle.CalcHeight` + `GUILayoutUtility.GetLastRect`），或改成固定行数视口只渲染末尾 N 行。

## 已知取舍 / 注意点

- **UI 用的是 Unity 内置 IMGUI（`OnGUI`），和 Dear ImGui 是两套 API**（`GUILayout.TextField` ≠ `ImGui.InputText`），照抄 ImGui 的例子会编译不过。好处是零依赖，不碰 URP / 渲染管线 / shader；代价是视觉为老式灰底（可用 `GUIStyle` 调色、调字号），且 `OnGUI` 每帧有 Layout + Repaint 两趟调用，控制台这个规模无感。
- 按键走 IMGUI 自己的 `Event.current`，与 `activeInputHandler` 设置无关。工程里 `PlayerController` 用的是传统 `Input.*`，当前设置是 Both，两边互不干扰。
- **一次按键会来两个 `KeyDown` 事件**：一个 `keyCode` 有效、`character` 为空（物理键），另一个 `character` 有效、`keyCode` 为 `None`（打出的字符）。只按 `keyCode == BackQuote` 吃事件会漏掉第二个，反引号就会落进输入框。所以 `HandleKeyEvents` 里开关只认 `keyCode`，`character` 是反引号的字符事件也必须一起 `Use()`。（CommandTerminal 等开源控制台用「开完后把文本回滚成缓存值」绕这个问题。）
- 打开时记录原 `Cursor.lockState` 并解锁光标，关闭时还原；`GameplayInputGate` 延到关闭后的下一帧才放开，避免同一帧 `PlayerController` 把那次 Esc 又当成「解锁光标」处理掉。
- `timescale` 只改 `Time.timeScale`，不联动 `Time.fixedDeltaTime`。`timescale 0` 时物理步长仍按原值计算，需要「慢放但物理不变形」时再加联动选项。
- 反射自动注册要求公开无参构造函数，且不会注册 `UnityEngine.Object` 派生类。IL2CPP 裁剪可能丢掉只被反射引用的指令类型，正式包需要 `link.xml` 保留，或改用 `ConsoleController.Register` 手动注册。
- `PlayerController` 现在在 `Update` 开头检查 `GameplayInputGate.Blocked`，并且 `HandleMoveInput` 也受 `IsMouseLocked` 约束（解锁光标不再能移动/跳跃）。后续加暂停菜单可以复用 `GameplayInputGate`。
- `Packages/manifest.json` 里的 `com.psydack.uimgui` 现在无人使用，可以移除。
- 上游 UImGui 包的 README（7.1.0 与更早的 git 版本都一样）承诺了 `Tools > UImGui > Add Render Feature to URP` 菜单，但包里 `grep -rn "MenuItem"` 零结果，该菜单并不存在。这是当初弃用它的原因之一。
