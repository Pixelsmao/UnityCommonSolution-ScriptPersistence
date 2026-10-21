# UnityCommonSolution-ScriptPersistence

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/Platform-Windows-red)

Unity 运行时脚本字段持久化包。为 `MonoBehaviour` 添加 `[PersistenceScript]` 后，包会在场景加载时读取或保存该组件的公开实例字段和属性，并按场景将配置写入 INI 文件。

## 功能

- 按场景保存配置；`DontDestroyOnLoad` 对象使用独立目录。
- 每个含 `[PersistenceScript]` 脚本的场景使用一个 `PersistenceSceneBootstrap`，在普通脚本的 `Awake` 前加载配置；编辑器会自动补齐缺失组件。
- 支持常用基础值类型、枚举、字符串、Unity 向量/颜色类型，以及相应的 `List<T>`。
- 使用 `[NonPersistence]` 排除不需要保存的字段或属性。
- 场景扫描后自动将不再对应持久化脚本的配置归档到场景目录下的 `Backups`。
- 包含 `ApplicationManager` 等应用辅助功能，可管理分辨率、全屏、目标帧率、光标、窗口置顶、快捷方式和运行日志收集。

## 安装

### 通过 Unity Package Manager 安装（推荐）

在 Unity 中打开 **Window > Package Manager**，点击 **+ > Add package from git URL...**，输入：

```text
https://github.com/Pixelsmao/UnityCommonSolution-ScriptPersistence.git
```

包清单会声明并由 UPM 解析 UnityExtensions 依赖。

### 克隆到 Assets

也可以将仓库克隆到项目的 `Assets` 目录。由于 Unity 不会为 `Assets` 内的包目录自动解析 `package.json` 依赖，请在项目的 `Packages/manifest.json` 中另行添加：

```json
"com.pixelsmao.unitycommonsolution.unityextensions": "https://github.com/Pixelsmao/UnityCommonSolution-UnityExtensions.git"
```

## 快速开始

将以下脚本挂到场景中的 GameObject 上。打开包含此脚本的场景时，编辑器会自动在场景根层级添加 `PersistenceSceneBootstrap`。保存场景后，运行时会在普通脚本的 `Awake` 前加载已有配置；配置不存在时，会用脚本当前值创建配置。

```csharp
using System.Collections.Generic;
using Pixelsmao.UnityCommonSolution.ScriptPersistence;
using UnityEngine;

[PersistenceScript]
public class PlayerSettings : MonoBehaviour
{
    public string playerName = "Player";
    public int level = 1;
    public List<string> unlockedItems = new List<string> { "sword", "shield" };

    [NonPersistence]
    public int runtimeCounter;
}
```

每个包含 `[PersistenceScript]` 组件的场景需要一个引导组件即可。编辑器在打开场景、添加持久化脚本或脚本重新编译后检查并补齐；自动添加会使场景变为未保存状态，需保存场景才能带入运行时。运行时动态加载的场景也必须在场景文件中包含引导组件。

引导组件只扫描场景初始化时已存在的对象；场景加载后才动态创建的对象不会自动加载配置。

引导组件默认早于普通脚本执行。如果项目中的脚本显式设置了与引导组件相同或更早的 Script Execution Order，它们可能先于引导组件运行；请协调执行顺序，或不要在这些脚本的 `Awake` 中读取持久化字段。

## 支持的数据

列表元素支持布尔、字符、数值类型、枚举、字符串、`Vector2`/`Vector3`/`Vector4`、`Vector2Int`/`Vector3Int`、`Quaternion` 和 `Color`。列表使用竖线分隔，例如：

```ini
unlockedItems=[sword|shield]
```

字符串元素读取时沿用现有逻辑移除空白字符。列表字符串中不能包含 `|`；仅包含一个空字符串的列表无法与空列表区分，这两种情况都会警告并跳过该成员。嵌套列表、自定义列表元素类型和 Unity 对象引用不受支持。

## 配置位置与归档

配置保存在应用程序目录下，目录结构如下：

```text
Config/
├─ <场景名>/
│  ├─ <层级与脚本名>.ini
│  └─ Backups/
└─ DontDestroyOnLoad/
```

场景加载时，持久化脚本会从对应场景目录读取配置。场景扫描完成后，目录顶层不再对应当前持久化脚本的 `.ini` 文件会移动到 `Backups` 并附加时间戳。旧版直接放在 `Config/` 根目录的配置不会自动迁移；首次使用新的场景目录时会以脚本当前值创建配置。

## 平台说明

此包当前列出的支持平台为 Windows。`ApplicationManager` 的窗口置顶、桌面/启动快捷方式和部分路径操作依赖 Windows API。

## 许可证

[MIT](LICENSE)
