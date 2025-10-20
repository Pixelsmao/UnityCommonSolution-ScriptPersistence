using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Pixelsmao.UnityCommonSolution.Extensions;
using UnityEngine;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    public static class ApplicationWindow
    {
        static ApplicationWindow()
        {
            application = Process.GetCurrentProcess();
            Application.runInBackground = true;
            Application.quitting += () =>
            {
                foreach (var subProcess in subProcesses.Where(subProcess => !subProcess.CloseMainWindow()))
                {
                    subProcess.Kill();
                }
            };
        }

        /// <summary>
        /// 应用程序主进程
        /// </summary>
        public static Process application { get; }

        /// <summary>
        /// 附加进程池，此部分的进程将跟随主线程的关闭而结束进程。
        /// </summary>
        private static readonly List<Process> subProcesses = new List<Process>();

        /// <summary>
        /// 独立进程池：此部分的进程将独立于主线程，主线程关闭而不结束进程。
        /// </summary>
        private static readonly List<Process> standAloneProcesses = new List<Process>();


        /// <summary>
        /// 将进程添加到主线程的附加进程池中
        /// </summary>
        public static void AddSubProcess(Process process)
        {
            if (!subProcesses.Contains(process)) subProcesses.Add(process);
        }

        /// <summary>
        /// 启动附加进程
        /// </summary>
        /// <param name="startInfo">启动信息</param>
        /// <param name="followMainThread">生命周期是否跟随主线程</param>
        /// <returns></returns>
        public static Process StartSubProcess(ProcessStartInfo startInfo, bool followMainThread)
        {
            var subProcess = new Process();
            subProcess.StartInfo = startInfo;
            subProcess.Start();
            if (followMainThread) subProcesses.Add(subProcess);
            else standAloneProcesses.Add(subProcess);
            return subProcess;
        }

        /// <summary>
        /// 还原应用程序窗口
        /// </summary>
        public static void Normalize() => application.Normalize();


        /// <summary>
        /// 最小化应用程序窗口
        /// </summary>
        public static void Minimize() => application.Minimize();

        /// <summary>
        /// 最大化应用程序窗口
        /// </summary>
        public static void Maximize() => application.Maximize();

        /// <summary>
        /// 置顶进程主窗口
        /// </summary>
        public static void Topmost() => application.Topmost();

        /// <summary>
        /// 取消置顶进程主窗口
        /// </summary>
        public static void UnTopmost() => application.UnTopmost();

        /// <summary>
        /// 置于Topmost窗口之后的最上层
        /// </summary>
        public static void ToTop() => application.ToTop();

        /// <summary>
        /// 置于Topmost窗口之后的最底层
        /// </summary>
        public static void ToBottom() => application.ToBottom();

        /// <summary>
        /// 置于Topmost窗口之后的最底层
        /// </summary>
        public static void ToFront() => application.ToFront();

        /// <summary>
        /// 仅限Windows平台。设置您的应用程序，使其具有唯一的全屏使用显示器。
        /// 此模式改变显示的操作系统分辨率，以匹配应用程序选择的分辨率。
        /// 在Windows以外的平台上，此模式退回到FullScreenWindow模式。
        /// </summary>
        public static void FullScreenExclusive() => Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;

        /// <summary>
        /// 仅限Windows平台。设置您的应用程序，使其具有唯一的全屏使用显示器。
        /// 此模式改变显示的操作系统分辨率，以匹配应用程序选择的分辨率。
        /// 在Windows以外的平台上，此模式退回到FullScreenWindow模式。
        /// </summary>
        public static void FullScreenExclusive(Vector2Int resolution)
        {
            Screen.SetResolution(resolution.x, resolution.y, true);
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        }

        /// <summary>
        /// 将应用程序窗口设置为全屏本机显示分辨率，覆盖整个屏幕。
        /// Unity以脚本设置的分辨率呈现应用程序，如果没有设置分辨率，则使用本机显示分辨率，并缩放应用程序以填充窗口。
        /// Unity在渲染输出中添加黑边以匹配显示宽高比，以防止内容拉伸。
        /// 操作系统的覆盖UI，如输入法编辑器（IME）窗口，全部显示在上面
        /// </summary>
        public static void FullScreen() => Screen.fullScreenMode = FullScreenMode.FullScreenWindow;

        /// <summary>
        /// 仅限桌面平台。将应用程序设置为非全屏的标准可移动窗口。窗口的大小取决于应用程序的分辨率
        /// </summary>
        public static void Windowed() => Screen.fullScreenMode = FullScreenMode.Windowed;

        /// <summary>
        /// 仅限桌面平台。将应用程序设置为非全屏的标准可移动窗口。窗口的大小取决于应用程序的分辨率。
        /// </summary>
        public static void MaximizedMac() => Screen.fullScreenMode = FullScreenMode.MaximizedWindow;

        public static bool IsWindowsPlatform()
        {
            return Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.WindowsEditor;
        }

        /// <summary>
        /// 退出应用程序
        /// </summary>
        public static void Quit() => Application.Quit();
    }
}