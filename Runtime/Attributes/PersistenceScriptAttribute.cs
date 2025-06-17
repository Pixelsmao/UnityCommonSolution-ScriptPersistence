using System;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PersistenceScriptAttribute : Attribute
    {
        public bool useDefaultPersistenceFile { get; }

        public PersistenceScriptAttribute(bool useDefaultPersistenceFile = true)
        {
            this.useDefaultPersistenceFile = useDefaultPersistenceFile;
        }
    }
}