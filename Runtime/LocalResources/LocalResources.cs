using System.IO;
using Pixelsmao.UnityCommonSolution.ScriptPersistence;

public static class LocalResources
{
    static LocalResources()
    {
        localResourcesDirectory = new DirectoryInfo(localResourcesPath);
        if (!localResourcesDirectory.Exists) localResourcesDirectory.Create();
    }

    /// <summary>
    /// 本地资源路径：只能在Unity的生命周期方法中使用此属性，因为使用了Application.dataPath。
    /// </summary>
    public static string localResourcesPath => Path.Combine(ApplicationUtility.applicationDirectory, "LocalResources");
    /// <summary>
    /// 本地资源目录：只能在Unity的生命周期方法中使用此属性，因为使用了Application.dataPath。
    /// </summary>
    public static DirectoryInfo localResourcesDirectory { get; }
}