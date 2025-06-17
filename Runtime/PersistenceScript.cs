using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    public class PersistenceScript
    {
        internal Object script { get; }
        internal Type scriptType => script.GetType();
        internal string scriptName => scriptType.Name;
        internal string scriptObjectName => script.name;
        internal string headerName => $"{scriptObjectName}.{scriptName}";

        public BindingFlags bindingFlags { get; } =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        public PersistenceFormat format { get; set; } = PersistenceFormat.Text;

        private readonly List<PersistenceMember> members = new List<PersistenceMember>();

        public PersistenceScript(Object script)
        {
            this.script = script;
        }

        public PersistenceScript(Object script, BindingFlags bindingFlags)
        {
            this.script = script;
            this.bindingFlags = bindingFlags;
        }

        public bool TryApply(List<string> sectionLines)
        {
            try
            {
                //从1开始跳过配置标题
                var validLines = sectionLines.Where(item => item.Contains("=")).ToList();
                var persistenceMembers = GetPersistenceMembers();
                for (var i = 0; i < validLines.Count; i++)
                {
                    persistenceMembers[i].ParseMemberValue(validLines[i]);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }


        public bool ScriptHeaderEquals(string header)
        {
            return header.Trim('[', ']') == headerName;
        }

        private List<PersistenceMember> GetPersistenceMembers()
        {
            return (from memberInfo in scriptType.GetMembers(bindingFlags)
                where (memberInfo.IsValueMember() && memberInfo.GetCustomAttribute<NonPersistenceAttribute>() == null)
                select new PersistenceMember(script, memberInfo)).ToList();
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[{headerName}]");
            foreach (var member in GetPersistenceMembers())
            {
                builder.AppendLine(member.ToString(format));
            }

            return builder.ToString();
        }
    }
}