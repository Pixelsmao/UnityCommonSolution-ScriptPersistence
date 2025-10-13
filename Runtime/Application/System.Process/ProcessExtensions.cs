using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    public static class ProcessExtensions
    {
        #region DllImport

        // 获取当前应用窗口句柄
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        // 用于控制窗口的显示状态
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // 引入 SetForegroundWindow API
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // 引入 FindWindow API (仅用于调试或找窗口)
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // 用于将窗口置于最顶层
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
            uint uFlags);

        private const int SW_HIDE = 0; // 隐藏窗口
        private const int SW_SHOWNORMAL = 1; // 普通显示，恢复到原始大小和位置
        private const int SW_SHOWMINIMIZED = 2; // 最小化
        private const int SW_SHOWMAXIMIZED = 3; // 最大化
        private const uint SWP_NOSIZE = 0x0001; // 忽略宽度和高度参数，保持窗口当前大小。
        private const uint SWP_NOMOVE = 0x0002; // 忽略X和Y参数，保持窗口当前位置。
        private const uint SWP_NOZORDER = 0x0004; // 忽略hWndInsertAfter参数，保持窗口当前Z序。
        private const uint SWP_NOREDRAW = 0x0008; // 不重绘窗口内容。
        private const uint SWP_NOACTIVATE = 0x0010; // 不激活窗口，保持当前激活窗口不变。
        private const uint SWP_SHOWWINDOW = 0x0040; // 显示窗口。
        private const uint SWP_HIDEWINDOW = 0x0080; // 隐藏窗口。
        private const uint SWP_NOOWNERZORDER = 0x0200; // 忽略所有者窗口的Z序。
        private const uint SWP_FRAMECHANGED = 0x0020; // 强制应用窗口的当前框架，通常用于改变窗口样式后强制刷新。

        private static readonly IntPtr HWND_TOP = new IntPtr(0); // 窗口置顶：位置在拥有HWND_TOPMOST的窗口之后的最顶层，且只生效一次
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); // 窗口始终置顶
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2); // 取消窗口始终置顶：恢复到始终置顶之前的状态
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1); // 将窗口置于底层

        #endregion

        /// <summary>
        /// 还原进程主窗口
        /// </summary>
        public static bool Normalize(this Process process)
        {
            return ShowWindow(process.MainWindowHandle, SW_SHOWNORMAL);
        }

        /// <summary>
        /// 最小化进程主窗口
        /// </summary>
        public static bool Minimize(this Process process)
        {
            return ShowWindow(process.MainWindowHandle, SW_SHOWMINIMIZED);
        }

        /// <summary>
        /// 最大化进程主窗口
        /// </summary>
        public static bool Maximize(this Process process)
        {
            return ShowWindow(process.MainWindowHandle, SW_SHOWMAXIMIZED);
        }

        /// <summary>
        /// 置顶进程主窗口
        /// </summary>
        public static bool Topmost(this Process process)
        {
            return SetWindowPos(process.MainWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
        }

        /// <summary>
        /// 取消置顶进程主窗口
        /// </summary>
        public static bool UnTopmost(this Process process)
        {
            return SetWindowPos(process.MainWindowHandle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
        }

        /// <summary>
        /// 置于Topmost窗口之后的最上层
        /// </summary>
        public static bool ToTop(this Process process)
        {
            return SetWindowPos(process.MainWindowHandle, HWND_TOP, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
        }

        /// <summary>
        /// 置于Topmost窗口之后的最底层
        /// </summary>
        public static bool ToBottom(this Process process)
        {
            return SetWindowPos(process.MainWindowHandle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);
        }

        /// <summary>
        /// 是否为主窗口进程
        /// </summary>
        public static bool IsMainWindow(this Process process) => process.MainWindowHandle != IntPtr.Zero;

        /// <summary>
        /// 是否为活动的窗口(进程不为空、为主窗口进程、进程没有退出。)
        /// </summary>
        public static bool IsActiveWindow(this Process process) =>
            process != null && process.IsMainWindow() && !process.HasExited;

        /// <summary>
        /// 将当前进程和目标进程的窗口
        /// </summary>
        /// <param name="process">当前进程</param>
        /// <param name="targetProcess">目标进程</param>
        public static bool SwapProcessWindows(this Process process, Process targetProcess)
        {
            if (!process.IsActiveWindow() | !targetProcess.IsActiveWindow()) return false;
            process.Topmost();
            targetProcess.Topmost();
            process.UnTopmost();
            return true;
        }

        /// <summary>
        /// 交换两个进程窗口的Z顺序
        /// </summary>
        /// <param name="process"></param>
        /// <param name="targetProcess"></param>
        /// (TODO 似乎无效)
        public static bool SwapProcessWindowZOrder(this Process process, Process targetProcess)
        {
            if (!process.IsActiveWindow() | !targetProcess.IsActiveWindow()) return false;
            var firstSwapResult = SetWindowPos(process.MainWindowHandle, targetProcess.MainWindowHandle, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            var secondSwapResult = SetWindowPos(targetProcess.MainWindowHandle, process.MainWindowHandle, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            return firstSwapResult && secondSwapResult;
        }


        /// <summary>
        /// 设置目标进程窗口为前台窗口
        /// </summary>
        public static bool ToFront(this Process process)
        {
            return process.IsActiveWindow() && SetForegroundWindow(process.MainWindowHandle);
        }

        /// <summary>
        /// 查找和当前进程名相同进程的主窗口进程
        /// </summary>
        /// <param name="process">当前进程</param>
        /// <param name="mainWindow">主窗口进程</param>
        public static bool GetProcessMainWindow(this Process process, out Process mainWindow)
        {
            return ProcessUtility.GetProcessMainWindowByName(process.ProcessName, out mainWindow);
        }
    }
}