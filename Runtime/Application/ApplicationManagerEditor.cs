using System;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    public static class ApplicationManagerEditor
    {
#if UNITY_EDITOR
        [MenuItem("GameObject/Application/ApplicationManager", false, 0)]
        public static void CreateApplicationManager()
        {
            var manager = Object.FindFirstObjectByType<ApplicationManager>();
            if (manager)
            {
                Debug.Log($"【{DateTime.Now}】 【ApplicationManager】已存在!");
                Selection.activeObject = manager;
                return;
            }

            var obj = new GameObject
            {
                name = "ApplicationManager"
            };
            obj.transform.SetParent(Selection.activeTransform);
            obj.transform.localPosition = Vector3.zero;
            obj.AddComponent<ApplicationManager>();
            Selection.SetActiveObjectWithContext(obj, obj);
        }
#endif
    }
}