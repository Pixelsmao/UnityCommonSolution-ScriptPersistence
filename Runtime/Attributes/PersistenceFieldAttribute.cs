using System;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property )]
    public class PersistenceMemberAttribute : Attribute
    {
    }
}