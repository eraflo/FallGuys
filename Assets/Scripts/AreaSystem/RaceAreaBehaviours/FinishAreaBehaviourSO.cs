using Eraflo.Common.AreaSystem;
using Eraflo.Common.ObjectSystem;
using FallGuys.Core;
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
            if (GameManager.Instance == null) return;

            // End-race countdown (starts when first player finishes)
            if (blackboard.Get<bool>("FinishTimerStarted", false))
            {
                float timer = blackboard.Get<float>("FinishTimer", 0f);
                timer -= Time.deltaTime;
                blackboard.Set("FinishTimer", timer);

                if (timer <= 0)
                {
                    blackboard.Set("FinishTimerStarted", false);
                    Debug.Log("[Race] FINISH AREA: RACE ENDED!");
                    GameManager.Instance.EndGame();
                }
            }
        }

        protected override void OnAreaEnter(BaseObject owner, Blackboard blackboard, Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.RaceEnded) return;

            // Detect player using Player script
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            ulong clientId = player.OwnerClientId;

            // Check if this player already finished
            if (GameManager.Instance.CurrentLeaderboard.HasFinished(clientId)) return;

            // Record finish using GameManager's global RaceTimer
            float raceTime = GameManager.Instance.RaceTimer;
            string playerName = $"Player_{clientId}";

            GameManager.Instance.RecordFinish(clientId, playerName, raceTime);

            int finishedCount = GameManager.Instance.CurrentLeaderboard.FinishedCount;

            // Start end-race timer when first player finishes
            if (finishedCount == 1)
            {
                // IMPORTANT: Read from Blackboard to get potentially overridden values
                float delay = blackboard.Get<float>("_endRaceDelay", 10f);

                blackboard.Set("FinishTimer", delay);
                blackboard.Set("FinishTimerStarted", true);
                Debug.Log($"[Race] FINISH AREA: First player finished! End-race timer started ({delay}s).");
            }

            Debug.Log($"[Race] FINISH AREA: {playerName} finished! Rank: {finishedCount}, Time: {raceTime:F2}s");
        }

        protected override void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected override void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other) { }
    }
}
