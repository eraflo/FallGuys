using Eraflo.Common.ObjectSystem;
using Eraflo.Common.PlatformSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.PlatformSystem.Conditions
{
    /// <summary>
    /// Condition that returns true after a specific time has elapsed.
    /// Works for both movement and waiting durations.
    /// </summary>
    [CreateAssetMenu(fileName = "TimerCondition", menuName = "StateMachine/Conditions/Timer")]
    public class TimerConditionSO : ConditionSO
    {
        [SerializeField] private bool _useWaitDelay = true;
        [SerializeField] private string _timeKey = "_waitStartTime";

        public override bool IsMet(Blackboard bb)
        {
            if (!bb.Has(_timeKey)) return false;

            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            MovingPlatformSO config = baseObj.RuntimeData.Config as MovingPlatformSO;
            if (config == null) return false;

            float startTime = bb.Get<float>(_timeKey);
            float duration = _useWaitDelay ? config.WaitDelay : config.TravelTime;

            return Time.time >= startTime + duration;
        }
    }
}
