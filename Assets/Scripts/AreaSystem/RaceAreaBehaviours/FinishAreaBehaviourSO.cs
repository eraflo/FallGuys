using Eraflo.Common.AreaSystem;
using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using Unity.Netcode;
using UnityEngine;

namespace FallGuys.AreaSystem
{
    [CreateAssetMenu(fileName = "FinishAreaBehaviour", menuName = "FallGuys/Areas/Behaviours/FinishArea")]
    public class FinishAreaBehaviourSO : AreaBehaviourSO
    {
        public override void OnUpdate(BaseObject owner, Blackboard blackboard)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (blackboard.Get<bool>("FinishTimerStarted", false))
            {
                float timer = blackboard.Get<float>("FinishTimer", 0f);
                timer -= Time.deltaTime;
                blackboard.Set("FinishTimer", timer);

                if (timer <= 0)
                {
                    blackboard.Set("FinishTimerStarted", false);
                    blackboard.Set("RaceEnded", true);
                    Debug.Log("[Race] FINISH AREA: RACE ENDED!");
                }
            }
        }

        protected override void OnAreaEnter(BaseObject owner, Blackboard blackboard, Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (blackboard.Get<bool>("RaceEnded", false)) return;

            int count = blackboard.Get<int>("FinishedCount", 0);
            count++;
            blackboard.Set("FinishedCount", count);

            if (count == 1)
            {
                if (owner.RuntimeData.Config is FinishAreaSO finishSO)
                {
                    blackboard.Set("FinishTimer", finishSO.EndRaceDelay);
                    blackboard.Set("FinishTimerStarted", true);
                    Debug.Log("[Race] FINISH AREA: First player finished! Timer started.");
                }
            }

            Debug.Log($"[Race] FINISH AREA: Player finished! Rank: {count}");
        }

        protected override void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected override void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other) { }
    }
}
