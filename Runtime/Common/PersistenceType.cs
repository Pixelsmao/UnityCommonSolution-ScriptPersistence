namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    public enum PersistenceFormat
    {
        /// <summary>
        /// 未知格式
        /// </summary>
        Unknown,

        /// <summary>
        /// 通用文本格式：标志是使用“=”连接两端的内容，使用@<和@>包裹工具提示
        /// </summary>
        Text,

        /// <summary>
        /// Json格式：标志是使用“:”连接两端的内容，且均用双引号包裹键和值；注释使用#进行标记
        /// </summary>
        Json,

        Xml,
        Excel
    }
}