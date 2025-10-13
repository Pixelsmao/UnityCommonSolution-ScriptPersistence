using System;
using System.IO;
using System.Runtime.InteropServices;
using IWshRuntimeLibrary;
using UnityEngine;
using File = System.IO.File;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    public static class ApplicationUtility
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr windowHandle, int zOrder, int x, int y, int width, int height,
            uint flags);

        private const int Z_ORDER_TOPMOST = -1;
        private const int Z_ORDER_NO_TOPMOST = -2;
        private const uint WINDOW_NOT_MOVE = 0x0002;
        private const uint WINDOW_NOT_CHANGE_SIZE = 0x0001;


        public static string applicationDirectory => Path.GetDirectoryName(Application.dataPath);
        public static string applicationPath => $"{applicationDirectory}\\{Application.productName}.exe";

        public static string desktopShortcutPath => Path.Combine(desktopPath, $"{Application.productName}.lnk");
        public static string startupShortcutPath => Path.Combine(startupPath, $"{Application.productName}.lnk");

        public static string commonStartupShortcutPath =>
            Path.Combine(commonStartupPath, $"{Application.productName}.lnk");

        public static string desktopPath => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public static string localDesktopPath => Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string startupPath => Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        public static string commonStartupPath => Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);
        public static string programsPath => Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        public static string localPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public static string localLowPath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}Low";

        public static string roamingPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string windowsPath => Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        public static string systemPath => Environment.GetFolderPath(Environment.SpecialFolder.System);
        public static string systemX86Path => Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
        public static string programFilesPath => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        public static string playerLogPath =>
            $"{localLowPath}\\{Application.companyName}\\{Application.productName}\\Player.log";

        public static string playerLogCollectDirectory => Path.Combine(applicationDirectory, "RuntimeLogs");
        public static string consoleLogPath => Application.consoleLogPath;

        public static string programFilesX86Path =>
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        public static DirectoryInfo CreateLocalDirectory(string directoryName)
        {
            return Directory.CreateDirectory($"{localPath}\\{directoryName}");
        }

        public static DirectoryInfo CreateLocalLowDirectory(string directoryName)
        {
            return Directory.CreateDirectory($"{localLowPath}\\{directoryName}");
        }

        public static DirectoryInfo CreateRoamingDirectory(string directoryName)
        {
            return Directory.CreateDirectory($"{roamingPath}\\{directoryName}");
        }

        public static DirectoryInfo CreateDesktopDirectory(string directoryName)
        {
            return Directory.CreateDirectory($"{desktopPath}\\{directoryName}");
        }

        public static DirectoryInfo CreateLocalDesktopDirectory(string directoryName)
        {
            return Directory.CreateDirectory($"{localDesktopPath}\\{directoryName}");
        }

        public static DirectoryInfo CreateApplicationDirectory(string directoryName)
        {
            return Directory.CreateDirectory($"{applicationPath}\\{directoryName}");
        }

        public static DirectoryInfo CreateProgramFilesDirectory(string directoryName)
        {
            return Directory.CreateDirectory($"{programFilesPath}\\{directoryName}");
        }

        public static DirectoryInfo CreateProgramFilesX86Directory(string directoryName)
        {
            return Directory.CreateDirectory($"{programFilesX86Path}\\{directoryName}");
        }

        public static string GetSpecialFolder(Environment.SpecialFolder specialFolder)
        {
            return Environment.GetFolderPath(specialFolder);
        }

        public static void CreateDesktopShortcut()
        {
            if (File.Exists(desktopShortcutPath)) return;
            CreateShortcut(applicationPath, desktopShortcutPath);
        }

        public static void DeleteDesktopShortcut()
        {
            if (File.Exists(desktopShortcutPath)) File.Delete(desktopShortcutPath);
        }


        public static bool CreateStartupShortcut()
        {
            //使用Inno setup打包的程序可能在公共启动项中创建快捷方式
            //因此需要先检测公共启动项中是否存在快捷方式
            if (File.Exists(commonStartupShortcutPath)) return false;
            if (File.Exists(startupShortcutPath)) return false;
            //通常Unity在公共启动项中创建快捷方式时会失败，需要解决以管理员权限运行的问题。
            if (CreateShortcut(applicationPath, commonStartupShortcutPath)) return true;
            //因此会在个人启动项中创建快捷方式
            if (CreateShortcut(applicationPath, startupShortcutPath)) return true;
            return false;
        }

        public static void DeleteStartupShortcut()
        {
            //如果使用Inno setup安装的程序，则启动项可能在公共启动项中存在，因此需要先检测删除公共启动项中的快捷方式
            if (File.Exists(commonStartupShortcutPath)) File.Delete(commonStartupShortcutPath);
            if (File.Exists(startupShortcutPath)) File.Delete(startupShortcutPath);
        }

        public static bool CreateShortcut(string targetPath, string shortcutPath)
        {
            try
            {
                IWshShell shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.Save();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void CreateShortcut(string targetPath, DirectoryInfo directory)
        {
            FileInfo targetFileInfo = new FileInfo(targetPath);
            IWshShell shell = new WshShell();
            IWshShortcut shortcut =
                (IWshShortcut)shell.CreateShortcut($"{directory.FullName}\\{targetFileInfo.Name}.lnk");
            shortcut.TargetPath = targetPath;
            shortcut.Save();
        }

        public static void CreateShortcut(FileInfo targetFileInfo, DirectoryInfo shortcutDirectoryInfo)
        {
            IWshShell shell = new WshShell();
            IWshShortcut shortcut =
                (IWshShortcut)shell.CreateShortcut(
                    $"{shortcutDirectoryInfo.FullName}\\{targetFileInfo.Name}");
            shortcut.TargetPath = targetFileInfo.FullName;
            shortcut.Save();
        }
    }
}