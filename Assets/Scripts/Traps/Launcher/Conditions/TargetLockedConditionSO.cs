using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.Conditions
{
    [CreateAssetMenu(fileName = "TargetLockedCondition", menuName = "StateMachine/Conditions/Launcher/TargetLocked")]
    public class TargetLockedConditionSO : ConditionSO
    {
        [SerializeField] private float _angleThreshold = 30f;

        public override bool IsMet(Blackboard bb)
        {
            Transform target = bb.Get<Transform>("_currentTarget");
            if (target == null) return false;

            GameObject owner = bb.GetOwnerObject();
            Vector3 toTarget = (target.position - owner.transform.position).normalized;
            float angle = Vector3.Angle(owner.transform.forward, toTarget);

            return angle <= _angleThreshold;
        }
    }
}
