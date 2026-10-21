using UnityEngine;

namespace Pixelsmao.UnityCommonSolution.ScriptPersistence
{
    /// <summary>
    /// Loads persistence data for this scene before ordinary MonoBehaviour Awake methods run.
    /// Place one instance in each scene that contains a PersistenceScript component.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    public sealed class PersistenceSceneBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Persistence.InitializeScene(gameObject.scene);
        }
    }
}
