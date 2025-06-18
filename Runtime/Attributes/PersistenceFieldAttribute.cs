using System;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property )]
    public class PersistenceMemberAttribute : Attribute
    {
    }
}