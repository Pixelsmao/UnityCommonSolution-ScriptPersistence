using System;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PersistenceScriptAttribute : Attribute
    {
        public string persistenceFileName = string.Empty;
        public PersistenceFormat format;

        /// <summary>
        /// 持久化脚本
        /// </summary>
        /// <param name="persistenceFileName">保持持久化脚本的文件名，无则保存到默认文件中。</param>
        /// <param name="format">持久化文件保存的格式</param>
        public PersistenceScriptAttribute(string persistenceFileName = null,PersistenceFormat format = PersistenceFormat.Text)
        {
            if (persistenceFileName != null) this.persistenceFileName = persistenceFileName;
            this.format = format;
        }
        
    }
}