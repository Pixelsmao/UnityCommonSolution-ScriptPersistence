using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
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
            Directory.CreateDirectory(GetSaveDirectory());
            File.WriteAllText(file.FullName, ToString());
        }

        public void LoadPersistenceFile()
        {
            LoadPersistenceFileInternal();
        }

        public void SaveOrLoadPersistenceFile()
        {
            if (!file.Exists)
            {
                SavePersistenceFile();
                return;
            }

            if (!LoadPersistenceFileInternal()) return;
            MoveFileToBackups();
            SavePersistenceFile();
        }

        public void AddPersistenceScript(PersistenceScript persistenceScript)
        {
            scripts.Add(persistenceScript);
        }

        public bool ContainsScript(object script)
        {
            return scripts.Any(persistenceScript => ReferenceEquals(persistenceScript, script));
        }

        private bool LoadPersistenceFileInternal()
        {
            if (format != PersistenceFormat.Text)
            {
                Debug.LogWarning($"持久化文件【{file.Name}】当前仅支持 Text 格式。");
                return false;
            }

            if (scripts.Count != 1)
            {
                Debug.LogWarning($"持久化文件【{file.Name}】必须只包含一个持久化脚本。");
                return true;
            }

            var contentLines = File.ReadAllLines(file.FullName).ToList();
            var headers = contentLines.Where(IsTextHeader).ToList();
            var requiresSynchronization = headers.Count != 1;
            if (headers.Count == 1 && !scripts[0].ScriptHeaderEquals(headers[0]))
            {
                requiresSynchronization = true;
                Debug.LogWarning($"持久化文件【{file.Name}】的脚本标题与当前脚本不匹配，将同步为当前配置。");
            }

            if (!scripts[0].TryApply(contentLines, out var requiresMemberSynchronization))
            {
                requiresSynchronization = true;
            }

            return requiresSynchronization || requiresMemberSynchronization;
        }

        private void MoveFileToBackups()
        {
            var backupDirectory = Path.Combine(GetSaveDirectory(), "Backups");
            Directory.CreateDirectory(backupDirectory);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
            var baseName = Path.GetFileNameWithoutExtension(file.Name);
            var extension = file.Extension;
            var backupPath = Path.Combine(backupDirectory, $"{baseName}_{timestamp}{extension}");
            var duplicateIndex = 1;
            while (File.Exists(backupPath))
            {
                backupPath = Path.Combine(backupDirectory, $"{baseName}_{timestamp}_{duplicateIndex}{extension}");
                duplicateIndex++;
            }

            File.Move(file.FullName, backupPath);
        }

        private static bool IsTextHeader(string line)
        {
            return !line.Contains("<") && !line.Contains(">") && line.Contains("[") && line.Contains("]");
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
            return saveDirectory == string.Empty ? defaultSaveDirectory : saveDirectory;
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
    }
}
