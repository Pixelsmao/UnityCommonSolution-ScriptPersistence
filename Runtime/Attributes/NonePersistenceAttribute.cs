using System;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    /// <summary>
    /// 非持久化属性：被此属性标记的成员不会被持久化。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class NonPersistenceAttribute : Attribute
    {
    }
}