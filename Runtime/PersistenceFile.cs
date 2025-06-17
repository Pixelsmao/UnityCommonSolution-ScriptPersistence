using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    public class PersistenceFile
    {
        public PersistenceFormat format { get; set; }
        public string saveDirectory { get; set; } = string.Empty;
        public string saveName { get; set; } = "Application";
        public string saveExtensionName { get; set; } = string.Empty;
        internal int scriptCount => scripts.Count;
        internal FileInfo file => new FileInfo(Path.Combine(GetSaveDirectory(), $"{saveName}{GetSaveExtensionName()}"));
        private readonly List<PersistenceScript> scripts = new List<PersistenceScript>();
        private readonly string defaultSaveDirectory;

        public PersistenceFile(PersistenceFormat persistenceFormat)
        {
            defaultSaveDirectory = Path.GetDirectoryName(Application.dataPath);
            format = persistenceFormat;
        }

        public PersistenceFile(PersistenceFormat persistenceFormat = PersistenceFormat.Text,
            PersistenceScript[] scripts = null)
        {
            defaultSaveDirectory = Path.GetDirectoryName(Application.dataPath);
            format = persistenceFormat;
            if (scripts != null) this.scripts.AddRange(scripts);
        }

        public void SavePersistenceFile()
        {
            File.WriteAllText(file.FullName, ToString());
        }

        public void LoadPersistenceFile()
        {
            switch (format)
            {
                case PersistenceFormat.Unknown:
                    break;
                case PersistenceFormat.Text:
                    var contentLines = File.ReadAllLines(file.FullName);
                    //将持久化脚本的标题索引位置加入索引列表
                    var scriptSegmentIndexes = contentLines.Select((_, i) => i)
                        .Where(index => IsTextHeader(contentLines[index])).ToList();
                    //添加脚本分段的结束索引
                    scriptSegmentIndexes.Add(contentLines.Length);
                    //为每一个脚本分段创建一个ScriptSegment
                    //填写脚本分段的标题、起始位置、结束位置
                    var scriptSegments = new List<ScriptSegment>();
                    for (var i = 0; i < scriptSegmentIndexes.Count - 1; i++)
                    {
                        var segment = new ScriptSegment(contentLines[scriptSegmentIndexes[i]],
                            scriptSegmentIndexes[i], scriptSegmentIndexes[i + 1]);
                        scriptSegments.Add(segment);
                    }

                    //将脚本分段依次应用于脚本
                    for (var i = 0; i < scripts.Count; i++)
                    {
                        if (i >= scriptSegments.Count)
                        {
                            Debug.LogWarning($"脚本{scripts[i].headerName} 缺少持久化脚本分段！");
                            continue;
                        }

                        var segment = scriptSegments[i];
                        if (!scripts[i].ScriptHeaderEquals(segment.segmentName))
                        {
                            Debug.LogWarning(
                                $"脚本【{scripts[i].headerName}】与对应的持久化脚本名称【{segment.segmentName.Trim('[', ']')}】不匹配," +
                                $"会继续应用对应位置的值，但可能应用为错误的值。");
                        }

                        scripts[i].TryApply(contentLines.Skip(segment.startIndex)
                            .Take(segment.rowCount).ToList());
                    }

                    break;
                case PersistenceFormat.Json:
                    break;
                case PersistenceFormat.Xml:
                    break;
                case PersistenceFormat.Excel:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool IsTextHeader(string line)
        {
            return !line.Contains("<") && !line.Contains(">") && line.Contains("[") && line.Contains("]");
        }

        public void SaveOrLoadPersistenceFile()
        {
            if (!file.Exists) SavePersistenceFile();
            else LoadPersistenceFile();
        }

        public void AddPersistenceScript(PersistenceScript persistenceScript)
        {
            //if (ContainsScript(persistenceScript)) return;
            scripts.Add(persistenceScript);
        }

        public bool ContainsScript(object script)
        {
            return scripts.Any(persistenceScript => ReferenceEquals(persistenceScript, script));
        }

        private string GetSaveExtensionName()
        {
            return format switch
            {
                PersistenceFormat.Unknown => saveExtensionName == string.Empty ? ".txt" : saveExtensionName,
                PersistenceFormat.Text => saveExtensionName == string.Empty ? ".ini" : saveExtensionName,
                PersistenceFormat.Json => saveExtensionName == string.Empty ? ".json" : saveExtensionName,
                PersistenceFormat.Xml => saveExtensionName == string.Empty ? ".xml" : saveExtensionName,
                PersistenceFormat.Excel => saveExtensionName == string.Empty ? ".xlsx" : saveExtensionName,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private string GetSaveDirectory()
        {
            return format switch
            {
                PersistenceFormat.Unknown => saveDirectory == string.Empty ? defaultSaveDirectory : saveDirectory,
                PersistenceFormat.Text => saveDirectory == string.Empty ? defaultSaveDirectory : saveDirectory,
                PersistenceFormat.Json => saveDirectory == string.Empty ? defaultSaveDirectory : saveDirectory,
                PersistenceFormat.Xml => saveDirectory == string.Empty ? defaultSaveDirectory : saveDirectory,
                PersistenceFormat.Excel => saveDirectory == string.Empty ? defaultSaveDirectory : saveDirectory,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var script in scripts)
            {
                builder.Append(script);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        internal class ScriptSegment
        {
            public string segmentName { get; }
            public int startIndex { get; }
            public int endIndex { get; }

            public int rowCount => endIndex - startIndex;

            public ScriptSegment(string segmentName, int startIndex, int endIndex)
            {
                this.segmentName = segmentName;
                this.startIndex = startIndex;
                this.endIndex = endIndex;
            }
        }
    }
}