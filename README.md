# UnityCommonSolution-ScriptPersistence

![GitHub](https://img.shields.io/badge/Unity-2021.3%2B-blue)
![GitHub](https://img.shields.io/badge/license-MIT-green)
![GitHub](https://img.shields.io/badge/Platform-Windows-red)

在Unity中通过反射的方法对场景中的脚本字段和属性成员进行持久化，以指定的格式存储在程序文件夹中，修改配置即可修改程序的运行逻辑。

## 安装

1. **通过克隆仓库安装**

   将本仓库克隆到您的 Unity 项目的 `Assets` 目录下：

   ```bash
   git clone https://github.com/Pixelsmao/UnityCommonSolution-ScriptPersistence.git
   ```

2. **使用UPM进行安装：**

   在 Unity 编辑器中，点击顶部菜单栏,打开 Package Manager 窗口.

   ```
   Window > Package Manager
   ```

   在 Package Manager 窗口的左上角，点击 **+** 按钮，然后选择 **Add package from git URL...**。
   在弹出的输入框中，粘贴本仓库的 Git URL：

   ```
   https://github.com/Pixelsmao/UnityCommonSolution-ScriptPersistence.git
   ```

   然后点击 **Add**。

## 使用方法

1.使用`PersistenceScript`属性标记脚本，然后运行程序，会在程序目录中生成`Applocation.ini`配置文件。