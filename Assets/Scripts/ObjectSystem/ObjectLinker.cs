using Eraflo.Common.ObjectSystem;
using Unity.Netcode;
using UnityEngine;

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
            // If the object has a logic key, we check if we need to attach the driver
            if (!string.IsNullOrEmpty(baseObject.RuntimeData.Config.LogicKey))
            {
                // NETWORK ANCHOR SUPPORT:
                // If this object is a child of a NetworkObject, the Driver should be on the Anchor (parent),
                // not on the visual/physical prefab itself.
                if (baseObject.GetComponentInParent<NetworkObject>() != null)
                {
                    return;
                }

                // Only add if not already present (safety check)
                if (baseObject.GetComponent<ObjectBehaviourDriver>() == null)
                {
                    baseObject.gameObject.AddComponent<ObjectBehaviourDriver>();
                }
            }
        }
    }
}
