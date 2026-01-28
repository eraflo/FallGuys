using FallGuys.StateMachine;
using Eraflo.Common.ObjectSystem;
using UnityEngine;

namespace FallGuys.Traps.Launcher.States
{
    [CreateAssetMenu(fileName = "LauncherOrientState", menuName = "StateMachine/States/Launcher/Orient")]
    public class LauncherOrientStateSO : StateBaseSO
    {
        public override void OnServerUpdate(Blackboard bb)
        {
            Transform target = bb.Get<Transform>("_currentTarget");
            if (target == null) return;

            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            if (baseObj == null) return;

            LauncherTrapSO config = baseObj.RuntimeData.Config as LauncherTrapSO;
            if (config == null) return;

            // Rotate towards target on server only
            Vector3 toTarget = (target.position - owner.transform.position);
            toTarget.y = 0; 
            
            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget);
                owner.transform.rotation = Quaternion.RotateTowards(owner.transform.rotation, targetRot, config.RotationSpeed * Time.deltaTime);
            }
        }
    }
}
