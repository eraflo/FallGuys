using UnityEngine;
using Eraflo.Common.ObjectSystem;

namespace FallGuys.ObjectSystem
{
    /// <summary>
    /// This class is responsible for automatically attaching project-specific scripts
    /// to shared objects from the Common package.
    /// </summary>
    public static class ObjectLinker
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            BaseObject.OnObjectCreated += HandleObjectCreated;
        }

        private static void HandleObjectCreated(BaseObject baseObject)
        {
            // If the object has a logic key, we attach the driver which will resolve the logic
            if (!string.IsNullOrEmpty(baseObject.RuntimeData.Config.LogicKey))
            {
                // Only add if not already present (safety check)
                if (baseObject.GetComponent<ObjectBehaviourDriver>() == null)
                {
                    baseObject.gameObject.AddComponent<ObjectBehaviourDriver>();
                }
            }
        }
    }
}
