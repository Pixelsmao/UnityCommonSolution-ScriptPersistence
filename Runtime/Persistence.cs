using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    public static class Persistence
    {
        private static string configDirectory => Path.Combine(ApplicationUtility.applicationDirectory, "Config");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RuntimeInitializeOnLoadMethod()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            var expectedFilesByDirectory = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                var directoryName = PersistenceScript.GetSceneDirectoryName(scene);
                if (!expectedFilesByDirectory.TryGetValue(directoryName, out var expectedFiles))
                {
                    expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    expectedFilesByDirectory.Add(directoryName, expectedFiles);
                }

                ScanSceneObjects(scene, expectedFiles, true);
            }

            foreach (var directoryEntry in expectedFilesByDirectory)
            {
                ReconcileSceneDirectory(directoryEntry.Key, directoryEntry.Value);
            }

            ScanDontDestroyOnLoadObjects();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            var directoryName = PersistenceScript.GetSceneDirectoryName(scene);
            var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ScanSceneObjects(scene, expectedFiles, true);

            // Same-named scenes share a directory, so preserve files for other loaded instances.
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var loadedScene = SceneManager.GetSceneAt(i);
                if (!loadedScene.IsValid() || !loadedScene.isLoaded || loadedScene.handle == scene.handle ||
                    !StringComparer.OrdinalIgnoreCase.Equals(
                        PersistenceScript.GetSceneDirectoryName(loadedScene), directoryName)) continue;

                ScanSceneObjects(loadedScene, expectedFiles, false);
            }

            ReconcileSceneDirectory(directoryName, expectedFiles);
        }

        private static void ScanSceneObjects(Scene scene, HashSet<string> expectedFiles, bool saveOrLoad)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                TraverseHierarchy(root.transform, expectedFiles, saveOrLoad);
            }
        }

        private static void ScanDontDestroyOnLoadObjects()
        {
            var directoryName = "DontDestroyOnLoad";
            var expectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var root in GetDontDestroyOnLoadGameObjects())
            {
                TraverseHierarchy(root.transform, expectedFiles, true);
            }

            ReconcileSceneDirectory(directoryName, expectedFiles);
        }

        private static void TraverseHierarchy(Transform parent, HashSet<string> expectedFiles, bool saveOrLoad)
        {
            SearchMonoBehaviours(parent, expectedFiles, saveOrLoad);
            for (var i = 0; i < parent.childCount; i++)
            {
                TraverseHierarchy(parent.GetChild(i), expectedFiles, saveOrLoad);
            }
        }

        private static void SearchMonoBehaviours(Component transform, HashSet<string> expectedFiles, bool saveOrLoad)
        {
            foreach (var monoBehaviour in transform.GetComponents<MonoBehaviour>())
            {
                if (monoBehaviour == null) continue;
                if (monoBehaviour.GetType().GetCustomAttribute<PersistenceScriptAttribute>() == null) continue;

                var persistenceScript = new PersistenceScript(monoBehaviour);
                expectedFiles.Add(persistenceScript.persistenceFileName);
                if (!saveOrLoad) continue;

                var persistenceFile = new PersistenceFile(PersistenceFormat.Text, new[] { persistenceScript })
                {
                    saveDirectory = Path.Combine(configDirectory, persistenceScript.persistenceDirectoryName),
                    saveName = persistenceScript.persistenceFileName
                };
                persistenceFile.SaveOrLoadPersistenceFile();
            }
        }

        private static void ReconcileSceneDirectory(string directoryName, HashSet<string> expectedFiles)
        {
            var sceneDirectory = Path.Combine(configDirectory, directoryName);
            if (!Directory.Exists(sceneDirectory)) return;

            foreach (var filePath in Directory.GetFiles(sceneDirectory, "*.ini", SearchOption.TopDirectoryOnly))
            {
                if (expectedFiles.Contains(Path.GetFileNameWithoutExtension(filePath))) continue;

                try
                {
                    PersistenceFile.MoveFileToBackups(new FileInfo(filePath), sceneDirectory);
                }
                catch (IOException e)
                {
                    Debug.LogWarning($"孤立持久化配置【{filePath}】移动到备份目录失败：{e.Message}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Debug.LogWarning($"孤立持久化配置【{filePath}】移动到备份目录失败：{e.Message}");
                }
            }
        }

        private static IEnumerable<GameObject> GetDontDestroyOnLoadGameObjects()
        {
            var allGameObjects = new List<GameObject>();
            allGameObjects.AddRange(Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None));
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                foreach (var obj in SceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    allGameObjects.Remove(obj);
                }
            }

            var k = allGameObjects.Count;
            while (--k >= 0)
            {
                if (allGameObjects[k].transform.parent != null)
                {
                    allGameObjects.RemoveAt(k);
                }
            }

            return allGameObjects.ToArray();
        }
    }
}
