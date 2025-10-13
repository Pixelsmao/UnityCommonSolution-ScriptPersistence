using System.Diagnostics;
using System.Linq;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    /// <summary>
    /// Windows进程类(Process)实用工具
    /// </summary>
    public static class ProcessUtility
    {
        /// <summary>
        /// 通过进程名查找进程的主窗口进程
        /// </summary>
        /// <param name="processName">进程名</param>
        /// <param name="mainWindow">主窗口进程</param>
        public static bool GetProcessMainWindowByName(string processName, out Process mainWindow)
        {
            var mainWindowResult = Process.GetProcessesByName(processName)
                .FirstOrDefault(process => process.IsMainWindow());
            if (mainWindowResult != null)
            {
                mainWindow = mainWindowResult;
                return true;
            }

            mainWindow = null;
            return false;
        }
    }
}