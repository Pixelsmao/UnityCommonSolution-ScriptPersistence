using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pixelsmao.UnityCommonSolution.SimpleDataPersistence
{
    /// <summary>
    /// unity的一般运行顺序为：
    /// 1. Engine Initialization (引擎初始化)  
    /// 2. BeforeSceneLoad (RuntimeInitializeOnLoadMethod)  
    /// 3. Scene Deserialization (场景反序列化) ，此时场景对象被创建  
    /// 4. Awake() (所有对象的 Awake 被调用)  
    /// 5. AfterSceneLoad (RuntimeInitializeOnLoadMethod)  
    /// 6. Start() (所有对象的 Start 被调用)
    /// 在AfterSceneLoad中的代码将在Awake之后执行，如果脚本的值在Awake中初始化，则保存和加载可能不会按照预期生效。
    /// TODO 考虑使用PlayerLoop在Scene Deserialization之后Awake()之前插入代码进行执行，但是修改 Unity 底层循环逻辑需要谨慎。
    /// </summary>
    public static class Persistence
    {
        private static readonly PersistenceFile defaultPersistenceFile = new PersistenceFile();
        private static readonly List<PersistenceFile> persistenceFiles = new List<PersistenceFile>();

        /// <summary>
        /// 在AfterSceneLoad中的代码会在Awake之后执行，如果脚本的值在Awake中初始化，则保存和加载可能不会按照预期生效。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RuntimeInitializeOnLoadMethod()
        {
            foreach (var root in GetDontDestroyOnLoadGameObjects())
            {
                TraverseHierarchy(root.transform);
            }

            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                TraverseHierarchy(root.transform);
            }


            if (defaultPersistenceFile.scriptCount != 0) defaultPersistenceFile.SaveOrLoadPersistenceFile();
            foreach (var persistenceFile in persistenceFiles)
            {
                persistenceFile.SaveOrLoadPersistenceFile();
            }
        }

        private static void TraverseHierarchy(Transform parent)
        {
            SearchMonoBehaviours(parent);
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                TraverseHierarchy(child);
            }
        }

        private static void SearchMonoBehaviours(Component transform)
        {
            foreach (var monoBehaviour in transform.GetComponents<MonoBehaviour>())
            {
                var attribute = monoBehaviour.GetType().GetCustomAttribute<PersistenceScriptAttribute>();
                if (attribute == null) continue;
                if (attribute.useDefaultPersistenceFile)
                {
                    defaultPersistenceFile.AddPersistenceScript(new PersistenceScript(monoBehaviour));
                }
                else
                {
                    var existedPersistenceFile = persistenceFiles.FirstOrDefault(persistenceFile =>
                        persistenceFile.saveName == monoBehaviour.GetType().Name);
                    if (existedPersistenceFile != null)
                    {
                        existedPersistenceFile.AddPersistenceScript(new PersistenceScript(monoBehaviour));
                    }
                    else
                    {
                        var persistenceFile = new PersistenceFile
                        {
                            saveName = monoBehaviour.GetType().Name
                        };
                        persistenceFile.AddPersistenceScript(new PersistenceScript(monoBehaviour));
                        persistenceFiles.Add(persistenceFile);
                    }
                }
            }
        }

        private static IEnumerable<GameObject> GetDontDestroyOnLoadGameObjects()
        {
            var allGameObjects = new List<GameObject>();
            allGameObjects.AddRange(Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None));
            //移除所有场景包含的对象
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var gameObjects = scene.GetRootGameObjects();
                foreach (var obj in gameObjects)
                {
                    allGameObjects.Remove(obj);
                }
            }

            //移除父级不为null的对象
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