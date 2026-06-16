# 配电柜 AR 操作与 AI 问答系统

本项目是一个基于 Unity 和 EasyAR 的配电柜 AR 演示系统。应用通过手机或电脑摄像头识别配电柜图片，在识别图像上叠加虚拟按钮、提示边框、说明面板和 AI 问答界面。用户可以点击配电柜上的 AR 按钮查看对应设备说明，也可以在左上角搜索框输入配电柜相关问题，系统会优先调用 DeepSeek，失败时回退到本地知识库回答。

项目适合用于配电柜操作流程演示、设备认知教学、AR 交互展示和移动端真机测试。

## 项目环境

- Unity：`2022.3.57f1c2`
- AR SDK：EasyAR Sense Unity Plugin
- 主场景：`Assets/Peidiangui.unity`
- 中文字体：`Assets/Fonts/ChineseFontSDF.asset`
- 本地问答知识库：`Assets/Resources/OperationData.json`
- Android 权限库：`Assets/Plugins/Android/DeepSeekPermissions.androidlib/AndroidManifest.xml`

## 目录结构

```text
Assets/
├─ Peidiangui.unity                  主场景
├─ Peidiangui/                       配电柜识别图与场景素材
├─ Scripts/                          项目自定义脚本
├─ Resources/                        本地 AI 问答知识库
├─ Fonts/                            中文字体与 TMP 字体资产
├─ Plugins/Android/                  Android 权限与插件配置
├─ StreamingAssets/                  EasyAR 运行资源
├─ TextMesh Pro/                     TextMeshPro 资源
├─ Samples/                          EasyAR 示例资源
└─ XR/                               XR 相关配置
```

## 核心功能

- 图片识别：识别配电柜目标图后显示 AR 内容。
- 透明识别框：使用绿色透明边框提示识别区域。
- AR 按钮交互：点击配电柜上的虚拟按钮，显示或隐藏对应说明面板。
- 防镜像文字面板：弹出面板会尝试选择更可读的朝向，减少手机端文字反向问题。
- AI 问答：支持 DeepSeek 在线问答，并在网络或 API 失败时回退本地知识库。
- 移动端 UI：搜索框固定在屏幕左上角，回答框位于搜索框下方，支持触摸拖动滚动。

## 场景组件说明

| 对象或组件 | 作用 |
|---|---|
| `AR Session (EasyAR)` | EasyAR 会话入口，负责启动 AR 环境。 |
| `Camera Device` | EasyAR 摄像头输入源。 |
| `Image Tracker` | 图片追踪器，用于检测配电柜识别图。 |
| `Image Target1` / `Image Target2` | 配电柜图片识别目标，识别成功后显示 AR 内容。 |
| `Main Camera` | AR 场景主摄像机，也用于点击射线检测。 |
| `InteractionManager` | 挂载 `ARInteractor`，负责鼠标和触摸点击交互。 |
| `Button_*`、`2-*`、`6.*` | 配电柜上可点击的虚拟按钮或交互区域。 |
| 各类说明面板 | 点击按钮后弹出的文字说明面板。 |
| `Canvas` | 屏幕 UI 根节点。 |
| `sousuo` | AI 问答输入框，运行时自动放到屏幕左上角。 |
| `ResultPanel` | AI 回答显示框，运行时自动放到搜索框下方。 |
| `AISearch` | 挂载 `AI_Search_Manager`，负责问答、DeepSeek 调用和本地知识库回退。 |

## 脚本说明

### `AI_Search_Manager.cs`

AI 问答核心脚本。

主要职责：

- 绑定搜索框 `sousuo` 和回答框 `ResultPanel`。
- 调整移动端搜索框和回答框布局。
- 加载 `Assets/Resources/OperationData.json` 本地知识库。
- 监听输入框提交和回车事件。
- 当 `useOnlineAI` 开启且 API Key 有效时调用 DeepSeek。
- DeepSeek 失败时自动使用本地知识库匹配回答。
- 动态生成可滚动回答内容，手机端可拖动查看。

关键字段：

| 字段 | 说明 |
|---|---|
| `apiKey` | DeepSeek API Key。仓库中必须使用占位符，不能提交真实 Key。 |
| `apiUrl` | DeepSeek Chat Completions 地址，默认 `https://api.deepseek.com/chat/completions`。 |
| `model` | DeepSeek 模型，当前默认 `deepseek-v4-flash`。 |
| `useOnlineAI` | 是否启用在线 AI。关闭时只使用本地知识库。 |
| `inputField` | TMP 输入框，对应场景中的 `sousuo`。 |
| `contentPanel` | 回答显示区域，对应 `ResultPanel`。 |
| `chineseFont` | 中文 TMP 字体资源。 |

安全说明：

- README 和场景文件中只保留 `sk-your-deepseek-api-key`。
- 如果 Key 曾经写入 APK 或提交过仓库，建议到 DeepSeek 后台作废并重新生成。

### `ARImageTracker.cs`

EasyAR 图片识别状态管理脚本。

主要职责：

- 绑定 `ImageTargetController`。
- 监听 `TargetFound` 和 `TargetLost`。
- 识别到图片时隐藏提示 UI。
- 丢失目标图时重新显示提示 UI。
- 将识别提示 UI 配置为透明填充加绿色边框。
- 运行时可放大提示边框，当前默认 `280 x 300`，边框厚度 `6`。

关键字段：

| 字段 | 说明 |
|---|---|
| `imageTargetController` | EasyAR 图片识别目标控制器。 |
| `hintUI` | 识别提示 UI。 |
| `useTransparentHintBorder` | 是否使用透明边框提示。 |
| `hintBorderColor` | 识别边框颜色。 |
| `hintBorderThickness` | 识别边框厚度。 |
| `hintBorderSize` | 识别提示框尺寸。 |

### `ARInteractor.cs`

AR 点击交互管理脚本。

主要职责：

- 支持鼠标点击和移动端触摸。
- 从摄像机向点击位置发射射线。
- 命中带 Collider 的交互对象。
- 命中 `ClickableButton3D` 时切换对应文字面板。
- 命中 `ToggleAnimOnTap` 时切换动画播放状态。

关键字段：

| 字段 | 说明 |
|---|---|
| `arCamera` | 用于发射射线的摄像机。通常绑定 `Main Camera`。 |
| `interactableMask` | 可交互对象 Layer。 |
| `maxDistance` | 射线检测最大距离。 |

### `ClickableButton3D.cs`

3D 可点击按钮绑定脚本。

主要职责：

- 挂载到可点击按钮或其父对象上。
- 通过 `targetText` 指向对应说明面板的 `TextToggleController`。
- 被 `ARInteractor` 命中后调用面板的 `Toggle()`。

关键字段：

| 字段 | 说明 |
|---|---|
| `targetText` | 点击后要显示或隐藏的说明面板控制器。 |

### `TextToggleController.cs`

说明面板显示和朝向控制脚本。

主要职责：

- 控制说明面板显示、隐藏和切换。
- 启动时根据 `startHidden` 隐藏面板。
- 显示时启用子对象中的 TMP 文本组件。
- 尝试在正常方向和 Y 轴翻转方向之间选择更可读的朝向，减少真机文字反向。

关键字段：

| 字段 | 说明 |
|---|---|
| `textRoot` | 要显示或隐藏的面板根对象。 |
| `tmpToFill` | 可选，显示时动态填充文字的 TMP 组件。 |
| `textWhenOpen` | 可选，打开面板时写入的文字。 |
| `startHidden` | 是否启动时默认隐藏。 |
| `faceCameraWhenShown` | 显示时是否自动修正朝向。 |

### `ToggleAnimOnTap.cs`

点击切换动画播放状态的脚本。

主要职责：

- 要求对象带有 Collider。
- 自动查找或手动绑定 Animator。
- 点击后切换 Animator bool 参数。

关键字段：

| 字段 | 说明 |
|---|---|
| `animator` | 要控制的 Animator。 |
| `boolParameter` | Animator 中的 bool 参数名，默认 `isPlaying`。 |
| `startPlaying` | 是否启动时默认播放。 |

### `UnityWebRequest.cs`

旧版后端数据请求示例脚本，内部定义了 `DatabaseConnector` 和相关 JSON 数据结构。

当前状态：

- 默认接口仍是示例地址 `https://your-api.com/get_operation?cmd=`。
- 当前主流程主要使用 `AI_Search_Manager.cs`。
- 如果后续接入自建后端，可以在该脚本基础上改真实接口。

### `button.cs`

空脚本，目前没有实际逻辑。可以删除，也可以作为后续按钮扩展脚本保留。

## 使用流程

### 1. 打开项目

1. 使用 Unity Hub 打开项目目录。
2. 推荐 Unity 版本：`2022.3.57f1c2`。
3. 打开主场景：

```text
Assets/Peidiangui.unity
```

### 2. 运行 AR 识别

1. 点击 Unity 顶部 `Play`。
2. 允许摄像头访问。
3. 将摄像头对准配电柜识别图片。
4. 识别成功后显示 AR 内容。
5. 点击配电柜虚拟按钮查看说明面板。

### 3. 使用 AI 问答

1. 运行场景。
2. 点击屏幕左上角搜索框。
3. 输入问题，例如：

```text
停电操作流程
熔断器故障怎么处理
指示灯不亮怎么办
合闸前要检查什么
```

4. 按回车或手机输入法提交。
5. 回答显示在搜索框下方，可以拖动滚动查看完整内容。

### 4. 配置 DeepSeek

场景中的 `AI_Search_Manager` 已经预留 DeepSeek 配置：

```text
apiUrl: https://api.deepseek.com/chat/completions
model: deepseek-v4-flash
apiKey: sk-your-deepseek-api-key
useOnlineAI: true
```

本地测试时：

1. 在 DeepSeek 平台创建 API Key。
2. 在 Unity Inspector 中找到 `AI_Search_Manager`。
3. 将 `apiKey` 临时替换为真实 Key。
4. 保持 `useOnlineAI` 勾选。
5. 测试完成后建议改回占位符，避免真实 Key 留在项目文件中。

建议正式项目不要把 API Key 放在客户端 APK 里。更安全的做法是：

```text
Unity App -> 自己的后端服务 -> DeepSeek API
```

这样 DeepSeek Key 不会暴露在 APK 中。

### 5. Android 打包

1. Unity Hub 安装 Android Build Support、SDK、NDK、OpenJDK。
2. 打开 `File > Build Settings`。
3. 选择 `Android`。
4. 点击 `Switch Platform`。
5. 确认 `Scenes In Build` 中包含：

```text
Assets/Peidiangui.unity
```

6. 点击 `Build` 或 `Build And Run`。

本项目已加入 Android 权限：

```text
INTERNET
ACCESS_NETWORK_STATE
CAMERA
```

对应文件：

```text
Assets/Plugins/Android/DeepSeekPermissions.androidlib/AndroidManifest.xml
```

如果手机端 DeepSeek 不生效，请确认：

- 手机已联网。
- 应用有网络权限。
- `useOnlineAI` 已开启。
- `apiKey` 不是占位符。
- DeepSeek API Key 有额度且未失效。

## 本地知识库格式

`Assets/Resources/OperationData.json` 建议使用如下结构：

```json
[
  {
    "command": "停电操作",
    "title": "低压配电柜停电操作流程",
    "keywords": ["停电", "断电", "检修"],
    "steps": [
      "通知相关人员并确认负荷可以停运。",
      "依次断开负荷开关，避免带负荷直接拉隔离开关。",
      "断开主开关或上级电源开关，并确认开关处于分闸位置。",
      "使用合格验电器验电，确认对应回路无电。",
      "挂上禁止合闸警示牌，并做好必要隔离防护。"
    ]
  }
]
```

当 DeepSeek 不可用或没有配置 Key 时，系统会使用本地知识库进行关键词匹配，并展示最接近的流程。

## 常见问题

### 手机端 DeepSeek 不生效

检查：

- 是否重新打包安装了最新 APK。
- 是否卸载旧 APK 后再安装。
- Android Manifest 是否包含 `INTERNET`。
- 手机网络是否能访问 DeepSeek。
- API Key 是否有效。
- `useOnlineAI` 是否勾选。

### 按钮文字反向

检查：

- 说明面板是否挂载 `TextToggleController`。
- `faceCameraWhenShown` 是否开启。
- 面板初始朝向是否正确。

### 搜索框或回答框位置不合适

当前运行时由 `AI_Search_Manager` 自动设置：

- 搜索框：屏幕左上角。
- 回答框：搜索框下方。

如需调整，可修改：

```text
GetInputFieldPosition()
GetInputFieldSize()
GetResultPanelPosition()
GetResultPanelSize()
```

### 绿色识别框大小不合适

可在 `ARImageTracker` 中修改：

```text
hintBorderSize
hintBorderThickness
hintBorderColor
```

### 中文显示异常

检查 TMP 中文字体资源：

```text
Assets/Fonts/ChineseFontSDF.asset
```

新增大量中文内容后，可能需要重新生成 TMP 字体图集。

## 后续优化建议

- 使用后端代理保护 DeepSeek API Key。
- 整理部分旧脚本中的乱码注释。
- 删除未使用的空脚本或示例脚本。
- 为按钮和说明面板建立更清晰的命名规范。
- 为本地知识库增加字段校验和编辑工具。
- 增加一个手机端“发送”按钮，减少对输入法回车的依赖。
