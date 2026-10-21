using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence.Editor
{
    [InitializeOnLoad]
    internal static class PersistenceSceneBootstrapSceneWatcher
    {
        static PersistenceSceneBootstrapSceneWatcher()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
            ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
            EditorApplication.delayCall += EnsureLoadedScenes;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += EnsureLoadedScenes;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            EnsureBootstrap(scene);
        }

        private static void EnsureLoadedScenes()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                EnsureBootstrap(SceneManager.GetSceneAt(i));
            }
        }

        private static void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (var i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.ChangeGameObjectStructure) continue;

                stream.GetChangeGameObjectStructureEvent(i, out var change);
                var changedObject = EditorUtility.InstanceIDToObject(change.instanceId) as GameObject;
                if (changedObject == null || !HasPersistenceScript(changedObject)) continue;

                EnsureBootstrap(changedObject.scene);
            }
        }

        private static void EnsureBootstrap(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded ||
                EditorSceneManager.IsPreviewScene(scene)) return;

            var hasPersistenceScript = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<PersistenceSceneBootstrap>(true) != null) return;
                if (HasPersistenceScript(root)) hasPersistenceScript = true;
            }

            if (!hasPersistenceScript) return;

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add Persistence Scene Bootstrap");

            var bootstrapObject = new GameObject("PersistenceSceneBootstrap");
            SceneManager.MoveGameObjectToScene(bootstrapObject, scene);
            Undo.RegisterCreatedObjectUndo(bootstrapObject, "Create Persistence Scene Bootstrap");
            Undo.AddComponent<PersistenceSceneBootstrap>(bootstrapObject);
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static bool HasPersistenceScript(GameObject gameObject)
        {
            foreach (var behaviour in gameObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null &&
                    System.Attribute.IsDefined(behaviour.GetType(), typeof(PersistenceScriptAttribute), true))
                {
                    return true;
                }
            }

            return false;
        }
    }

    [CustomEditor(typeof(PersistenceSceneBootstrap))]
    internal sealed class PersistenceSceneBootstrapInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new HelpBox(
                "场景引导组件会在场景内普通 MonoBehaviour 的 Awake 前扫描层级并加载持久化配置。每个包含 [PersistenceScript] 脚本的场景放置一个即可。",
                HelpBoxMessageType.Info));
            root.Add(new HelpBox(
                "显式设为与引导组件相同或更早执行顺序的脚本，可能先于引导组件运行。请协调 Script Execution Order，或避免在这些脚本的 Awake 中读取持久化字段。",
                HelpBoxMessageType.Warning));

            var scene = ((PersistenceSceneBootstrap)target).gameObject.scene;
            if (scene.IsValid() && scene.isDirty)
            {
                root.Add(new HelpBox(
                    "此场景存在未保存的更改。请保存场景，确保引导组件在运行时存在。",
                    HelpBoxMessageType.Warning));
            }

            return root;
        }
    }
}
