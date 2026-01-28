using System.Threading;
using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.States
{
    [CreateAssetMenu(fileName = "LauncherSearchState", menuName = "StateMachine/States/Launcher/Search")]
    public class LauncherSearchStateSO : StateBaseSO
    {
        public override void OnUpdate(Blackboard bb)
        {
            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            if (baseObj == null) return;

            LauncherTrapSO config = baseObj.RuntimeData.Config as LauncherTrapSO;
            if (config == null) return;

            // Only update transform logic on server if we expect NetworkTransform to sync it
            // OR if we want deterministic visual on everyone. 
            // Since it's deterministic (Mathf.Sin(Time.time)), running on everyone is actually fine for visual sync.
            // However, to be purely authoritative, we should use the server's time or similar.

            Quaternion initialRot = bb.Get<Quaternion>("_initialRotation");
            
            float angle = Mathf.Sin(Time.time * (config.RotationSpeed / 45f)) * config.SearchAngleRange;
            owner.transform.rotation = initialRot * Quaternion.Euler(0, angle, 0);
        }
    }
}
