# AR 配电柜展示项目课程文档
此项目基于kangruobing/AR-peidiangui.git
## 1. 课程目标

本课程展示项目基于 Unity、EasyAR 和配电柜 AR 示例工程，帮助同学完成一个可运行、可扩展的 AR 交互大作业。通过本项目，同学需要掌握：

- Unity 场景、物体、组件、脚本和 UI 的基本操作。
- EasyAR 在 Unity 中的导入、授权、图片识别和 Android 打包流程。
- 基于图像识别的 AR 内容叠加，包括按钮、说明面板、识别边框等。
- 基于已有示例工程进行二次开发，形成自己的课程大作业。
- Android 真机测试、权限配置和常见问题排查。

本仓库提供的是配电柜 AR 展示项目，同学可以在此基础上更换识别图、修改 AR 内容、扩展交互逻辑、接入问答功能或设计新的应用场景。

## 2. 课程资料

### Unity 与 EasyAR 入门录像

上次课程录像：

```text
【Unity3D 快速基础入门】
https://www.bilibili.com/video/BV1WYHCzKEf4?vd_source=de310bd039bd69aeeaf6668517f041ab
```

建议先观看该视频，了解 Unity 的基本界面、场景操作、组件系统、脚本挂载方式，以及 EasyAR 的基础使用思路。

### EasyAR 快速开始文档

```text
https://www.easyar.cn/doc/zh-cn/develop/unity/getting-started/quickstart.html?tabs=android
```

该文档用于学习 EasyAR Unity 插件的快速配置流程，包括创建项目、导入插件、配置授权、添加 AR 组件、运行识别等。

### EasyAR 工程配置与打包文档

Android 打包前，请参考以下两个官方文档：

```text
https://www.easyar.cn/doc/zh-cn/develop/unity/fundamentals/setup-player.html?tabs=android
https://www.easyar.cn/doc/zh-cn/develop/unity/fundamentals/setup-easyar.html
```

主要关注 Player Settings、包名、权限、EasyAR 设置、License Key 配置和 Android 平台相关参数。

## 3. 示例项目地址

配电柜 AR 项目仓库：

```text
git@github.com:kangruobing/AR-peidiangui.git
```

如果使用 SSH 克隆：

```bash
git clone git@github.com:kangruobing/AR-peidiangui.git
```

如果电脑没有配置 GitHub SSH，也可以在 GitHub 页面使用 HTTPS 地址下载或克隆。

## 4. 环境要求

推荐环境：

| 项目 | 要求 |
|---|---|
| Unity | 2022.3.57f1c2 |
| Unity Hub | 建议使用 Unity Hub 打开项目 |
| AR SDK | EasyAR Sense Unity Plugin |
| 平台 | Android |
| Android 模块 | Android Build Support、SDK、NDK、OpenJDK |
| 示例主场景 | `Assets/Peidiangui.unity` |

如果 Unity 版本略有不同，通常也可以打开项目，但建议尽量使用相同或相近的 Unity 2022.3 LTS 版本，减少插件兼容问题。

## 5. EasyAR 插件包

本项目依赖 EasyAR Sense Unity Plugin。由于插件包涉及授权和商用限制，仓库中不直接包含安装包，也不公开提供下载链接。课程同学请通过课程群或助教私发获取插件包：

```text
文件名：EasyARSenseUnityPlugin_4002.0.0+4956.1ec38c1ad.7z
```

下载后建议解压到以下路径：

```text
D:/EasyARSenseUnityPlugin_4002.0.0+4956.1ec38c1ad/
```

解压后应能找到类似文件：

```text
D:/EasyARSenseUnityPlugin_4002.0.0+4956.1ec38c1ad/com.easyar.sense-4002.0.0+4956.1ec38c1ad.tgz
```

如果解压路径不同，可以在 Unity 中通过以下方式手动导入：

```text
Window > Package Manager > + > Add package from tarball...
```

然后选择解压目录中的 `.tgz` 文件。也可以修改 `Packages/manifest.json` 中 `com.easyar.sense` 对应的本地路径。

## 6. EasyAR License Key

课程统一使用助教在课程群中提供的 EasyAR Sense License Key。

注意：

- License Key 属于授权信息，请不要提交到公开仓库。
- 如果基于本项目另建公开仓库，建议在文档中写“由课程群提供”，不要直接写完整 Key。
- 如果打包或运行时提示 EasyAR 授权异常，请检查 `EasyAR Settings` 中的 License Key 是否已经正确填写。

Unity 中常见配置位置：

```text
Assets/XR/Settings/EasyAR Settings.asset
```

或在 Unity 菜单、Project Settings、EasyAR 相关设置面板中查找 EasyAR License Key 配置项。

## 7. 统一包名

Android 包名统一使用：

```text
com.DefaultCompany.navigation
```

在 Unity 中检查或修改路径：

```text
File > Build Settings > Player Settings > Player > Other Settings > Identification > Package Name
```

如果包名不一致，可能影响 Android 安装、覆盖更新、权限和 EasyAR 授权匹配。

## 8. 项目打开流程

1. 下载或克隆配电柜项目。
2. 下载并解压 EasyAR 插件包。
3. 使用 Unity Hub 打开项目目录。
4. 等待 Unity 导入资源和编译脚本。
5. 如果提示 EasyAR 包缺失，按第 5 节方法重新导入 `.tgz` 插件包。
6. 打开主场景：

```text
Assets/Peidiangui.unity
```

7. 检查 EasyAR License Key 是否已配置。
8. 点击 Unity 顶部 `Play` 运行测试。

## 9. 示例项目功能说明

配电柜示例项目已经实现：

- 配电柜图像识别。
- 透明绿色识别边框。
- AR 虚拟按钮。
- 点击按钮弹出说明面板。
- 面板文字朝向修正，减少手机端文字反向问题。
- 左上角搜索框。
- 搜索框下方回答框。
- 回答框内容可拖动滚动。
- DeepSeek 在线问答预留接口。
- 本地知识库兜底问答。
- Android 摄像头和网络权限配置。

同学可以直接运行示例，也可以以此为模板替换为自己的主题。

## 10. 大作业开发建议

同学可以从以下方向选择一个或多个进行扩展：

- 更换识别图：使用自己的图片、海报、设备面板、实验装置或教学材料作为识别目标。
- 增加 AR 内容：添加文字说明、模型、图片、动画、视频或操作步骤。
- 扩展交互：点击、拖动、切换状态、播放动画、显示隐藏面板。
- 优化 UI：设计适合手机屏幕的搜索框、说明框、按钮和提示信息。
- 接入问答：使用本地知识库或在线大模型回答与主题相关的问题。
- 真机打包：完成 Android APK 构建并在手机上演示。

建议大作业至少包含：

| 模块 | 基本要求 |
|---|---|
| 图像识别 | 能识别至少一张目标图 |
| AR 展示 | 识别成功后显示虚拟内容 |
| 用户交互 | 至少包含一种点击或触摸交互 |
| 说明内容 | 能解释识别对象或操作流程 |
| 真机测试 | 能在 Android 手机上运行 |

## 11. Android 打包流程

1. 在 Unity Hub 中确认安装 Android Build Support、SDK、NDK、OpenJDK。
2. 打开项目后进入：

```text
File > Build Settings
```

3. 选择 `Android`。
4. 点击 `Switch Platform`。
5. 确认 `Scenes In Build` 包含：

```text
Assets/Peidiangui.unity
```

6. 打开 `Player Settings`。
7. 检查包名是否为：

```text
com.DefaultCompany.navigation
```

8. 检查 EasyAR License Key。
9. 检查 Android 权限，项目中已包含：

```text
INTERNET
ACCESS_NETWORK_STATE
CAMERA
```

10. 点击 `Build` 或 `Build And Run`。
11. 将 APK 安装到 Android 手机并测试摄像头识别、交互和 UI 显示。

## 12. 常见问题

### EasyAR 插件缺失

现象：

- Unity 打开后出现 EasyAR 相关脚本报错。
- Package Manager 中没有 EasyAR Sense。
- 场景中的 EasyAR 组件无法识别。

处理：

- 确认已经下载并解压 EasyAR 插件包。
- 通过 Package Manager 手动添加 `.tgz`。
- 检查 `Packages/manifest.json` 中的 EasyAR 本地路径是否正确。

### EasyAR 授权异常

处理：

- 确认使用课程群提供的统一 License Key。
- 检查包名是否为 `com.DefaultCompany.navigation`。
- 检查 EasyAR 官方后台或本地配置中 License Key 与应用信息是否匹配。

### Android 手机上没有摄像头画面

处理：

- 确认手机已授权相机权限。
- 检查 Android Manifest 中是否包含 `CAMERA`。
- 尝试卸载旧 APK 后重新安装。
- 确认手机系统没有禁止应用访问摄像头。

### 打包后网络问答不可用

处理：

- 确认手机能联网。
- 检查 Android Manifest 中是否包含 `INTERNET` 和 `ACCESS_NETWORK_STATE`。
- 确认 API Key 有效。
- 建议不要把真实 API Key 写入公开仓库或提交文件。

### 手机端 UI 大小和 Unity Game 视图不一致

处理：

- 使用真机截图检查 UI。
- 调整 Canvas Scaler、锚点、RectTransform。
- 对手机端单独设置输入框和回答框大小。
- 注意横屏、竖屏和不同分辨率下的显示效果。

## 13. 提交与展示建议

建议同学提交：

- Unity 项目工程。
- APK 安装包。
- 项目说明文档。
- 识别图或演示图片。
- 1 到 3 分钟演示视频或现场演示。

项目说明文档建议包含：

- 项目主题。
- 使用的识别图。
- 主要 AR 内容。
- 交互方式。
- 打包和测试设备。
- 遇到的问题与解决方法。

展示时建议说明：

- 为什么选择这个 AR 场景。
- 识别成功后显示了哪些信息。
- 用户可以进行哪些操作。
- 项目相比基础示例做了哪些扩展。

## 14. 重要提醒

- 不要把真实 API Key、EasyAR License Key、个人账号密码提交到公开仓库。
- 不要提交 Unity 的 `Library/`、`Temp/`、`Logs/` 等临时目录。
- 如果项目无法打开，优先检查 Unity 版本和 EasyAR 插件包路径。
- 如果手机运行效果和电脑 Game 视图不同，以手机真机测试结果为准。
- 大作业不要求完全照搬配电柜项目，鼓励基于模板做自己的主题和交互设计。
