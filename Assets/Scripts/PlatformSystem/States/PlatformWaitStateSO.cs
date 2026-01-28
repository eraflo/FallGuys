using System.Threading;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.PlatformSystem.States
{
    /// <summary>
    /// State where the platform remains stationary at a destination.
    /// </summary>
    [CreateAssetMenu(fileName = "PlatformWaitState", menuName = "StateMachine/States/Platform/Wait")]
    public class PlatformWaitStateSO : StateBaseSO
    {
        public override void OnEnter(Blackboard bb, CancellationToken ct)
        {
            bb.Set("_waitStartTime", Time.time);
        }

        public override void OnServerUpdate(Blackboard bb)
        {
            // Do nothing, just wait
        }
    }
}
