using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    public class PersistenceScript
    {
        internal Object script { get; }
        internal Type scriptType => script.GetType();
        internal string scriptName => scriptType.Name;
        internal string scriptObjectName => script.name;
        internal string headerName => $"{scriptObjectName}.{scriptName}";
        internal string persistenceFileName => GetPersistenceFileName();

        public BindingFlags bindingFlags { get; } = BindingFlags.Public | BindingFlags.Instance;
        public PersistenceFormat format { get; set; } = PersistenceFormat.Text;

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
            return TryApply(sectionLines, out _);
        }

        internal bool TryApply(List<string> sectionLines, out bool requiresSynchronization)
        {
            requiresSynchronization = false;
            try
            {
                var membersByName = new Dictionary<string, PersistenceMember>(StringComparer.Ordinal);
                foreach (var persistenceMember in GetPersistenceMembers())
                {
                    if (membersByName.ContainsKey(persistenceMember.memberName))
                    {
                        requiresSynchronization = true;
                        Debug.LogWarning($"持久化脚本【{headerName}】包含重名成员【{persistenceMember.memberName}】。");
                    }

                    membersByName[persistenceMember.memberName] = persistenceMember;
                }

                var appliedMemberNames = new HashSet<string>(StringComparer.Ordinal);
                foreach (var line in sectionLines.Where(item => item.Contains("=")))
                {
                    if (!PersistenceMember.TryGetTextMemberName(line, out var memberName))
                    {
                        requiresSynchronization = true;
                        continue;
                    }

                    if (!membersByName.TryGetValue(memberName, out var persistenceMember))
                    {
                        requiresSynchronization = true;
                        Debug.LogWarning($"持久化脚本【{headerName}】包含已不存在的成员【{memberName}】。");
                        continue;
                    }

                    if (!appliedMemberNames.Add(memberName))
                    {
                        requiresSynchronization = true;
                        Debug.LogWarning($"持久化脚本【{headerName}】成员【{memberName}】存在重复配置，最后一项将生效。");
                    }

                    if (persistenceMember.ParseMemberValue(line)) continue;
                    requiresSynchronization = true;
                    Debug.LogWarning($"持久化脚本【{headerName}】成员【{memberName}】值解析失败，将保留当前默认值。");
                }

                foreach (var memberName in membersByName.Keys)
                {
                    if (appliedMemberNames.Contains(memberName)) continue;
                    requiresSynchronization = true;
                    Debug.LogWarning($"持久化脚本【{headerName}】缺少成员【{memberName}】配置，将使用当前默认值。");
                }

                return true;
            }
            catch (Exception e)
            {
                requiresSynchronization = true;
                Debug.LogError(e);
                return false;
            }
        }

        public bool ScriptHeaderEquals(string header)
        {
            return header.Trim('[', ']') == headerName;
        }

        private string GetPersistenceFileName()
        {
            if (script is not Component component)
            {
                return $"Unnamed_{SanitizeFileNameSegment(scriptName)}";
            }

            var hierarchySegments = new List<string> { GetSceneScope(component) };
            var transforms = new Stack<Transform>();
            for (var current = component.transform; current != null; current = current.parent)
            {
                transforms.Push(current);
            }

            while (transforms.Count > 0)
            {
                hierarchySegments.Add(GetHierarchySegment(transforms.Pop()));
            }

            hierarchySegments.Add(GetComponentTypeSegment(component));
            return string.Join("_", hierarchySegments);
        }

        private static string GetSceneScope(Component component)
        {
            var sceneName = component.gameObject.scene.name;
            return string.IsNullOrWhiteSpace(sceneName) ? "DontDestroyOnLoad" : SanitizeFileNameSegment(sceneName);
        }

        private static string GetHierarchySegment(Transform transform)
        {
            var sameNameCount = 0;
            var sameNameIndex = 0;
            if (transform.parent != null)
            {
                for (var i = 0; i < transform.parent.childCount; i++)
                {
                    var sibling = transform.parent.GetChild(i);
                    if (sibling.name != transform.name) continue;
                    sameNameCount++;
                    if (ReferenceEquals(sibling, transform)) sameNameIndex = sameNameCount;
                }
            }
            else
            {
                foreach (var sibling in transform.gameObject.scene.GetRootGameObjects())
                {
                    if (sibling.name != transform.name) continue;
                    sameNameCount++;
                    if (ReferenceEquals(sibling.transform, transform)) sameNameIndex = sameNameCount;
                }
            }

            var segment = SanitizeFileNameSegment(transform.name);
            return sameNameCount > 1 ? $"{segment}_{sameNameIndex}" : segment;
        }

        private string GetComponentTypeSegment(Component component)
        {
            var matchingComponents = component.GetComponents<MonoBehaviour>()
                .Where(item => item.GetType().Name == scriptType.Name)
                .ToArray();
            var componentIndex = Array.IndexOf(matchingComponents, (MonoBehaviour)component) + 1;
            var segment = SanitizeFileNameSegment(scriptType.Name);
            return matchingComponents.Length > 1 ? $"{segment}_{componentIndex}" : segment;
        }

        private static string SanitizeFileNameSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Unnamed";
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Trim().Length);
            foreach (var character in value.Trim())
            {
                builder.Append(invalidCharacters.Contains(character) ? '_' : character);
            }

            return builder.Length == 0 ? "Unnamed" : builder.ToString();
        }

        private List<PersistenceMember> GetPersistenceMembers()
        {
            return (from memberInfo in scriptType.GetMonoBehaviourDerivedMembers(bindingFlags)
                where memberInfo.IsSupportedSerializableMember() &&
                      memberInfo.GetCustomAttribute<NonPersistenceAttribute>() == null
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
