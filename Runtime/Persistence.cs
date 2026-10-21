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
        private static readonly HashSet<int> initializedSceneHandles = new HashSet<int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneCallbacks()
        {
            initializedSceneHandles.Clear();
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RuntimeInitializeOnLoadMethod()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (initializedSceneHandles.Contains(scene.handle)) continue;

                if (HasPersistenceScripts(scene))
                {
                    Debug.LogWarning($"场景【{scene.name}】未在 Awake 前完成持久化初始化。请确保场景中有已激活的 PersistenceSceneBootstrap 并保存场景；当前配置将在 Awake 之后加载。");
                }
                InitializeScene(scene);
            }

            ScanDontDestroyOnLoadObjects();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded || initializedSceneHandles.Contains(scene.handle)) return;

            if (HasPersistenceScripts(scene))
            {
                Debug.LogWarning($"场景【{scene.name}】未在 Awake 前完成持久化初始化。请确保场景中有已激活的 PersistenceSceneBootstrap 并保存场景；当前配置将在 Awake 之后加载。");
            }
            InitializeScene(scene);
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            initializedSceneHandles.Remove(scene.handle);
        }

        private static bool HasPersistenceScripts(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour != null && behaviour.GetType().GetCustomAttribute<PersistenceScriptAttribute>() != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static void InitializeScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || initializedSceneHandles.Contains(scene.handle)) return;

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
            initializedSceneHandles.Add(scene.handle);
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
