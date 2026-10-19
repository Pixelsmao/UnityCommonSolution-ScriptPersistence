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
            foreach (var root in GetDontDestroyOnLoadGameObjects())
            {
                TraverseHierarchy(root.transform);
            }

            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                TraverseHierarchy(root.transform);
            }
        }

        private static void TraverseHierarchy(Transform parent)
        {
            SearchMonoBehaviours(parent);
            for (var i = 0; i < parent.childCount; i++)
            {
                TraverseHierarchy(parent.GetChild(i));
            }
        }

        private static void SearchMonoBehaviours(Component transform)
        {
            foreach (var monoBehaviour in transform.GetComponents<MonoBehaviour>())
            {
                if (monoBehaviour.GetType().GetCustomAttribute<PersistenceScriptAttribute>() == null) continue;

                var persistenceScript = new PersistenceScript(monoBehaviour);
                var persistenceFile = new PersistenceFile(PersistenceFormat.Text, new[] { persistenceScript })
                {
                    saveDirectory = configDirectory,
                    saveName = persistenceScript.persistenceFileName
                };
                persistenceFile.SaveOrLoadPersistenceFile();
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
