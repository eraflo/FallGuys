using Eraflo.Common.AreaSystem;
using Eraflo.Common.ObjectSystem;
using Eraflo.Common.Player;
using FallGuys.Core;
using FallGuys.StateMachine;
using Unity.Netcode;
using UnityEngine;

namespace FallGuys.AreaSystem
{
    [CreateAssetMenu(fileName = "StartAreaBehaviour", menuName = "FallGuys/Areas/Behaviours/StartArea")]
    public class StartAreaBehaviourSO : AreaBehaviourSO
    {
        public override void OnUpdate(BaseObject owner, Blackboard blackboard)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // Countdown phase (starts when first player enters)
            if (blackboard.Get<bool>("IsCountingDown", false))
            {
                float timer = blackboard.Get<float>("Timer", 0f);
                timer -= Time.deltaTime;
                blackboard.Set("Timer", timer);

                if (timer <= 0)
                {
                    blackboard.Set("IsCountingDown", false);
                    blackboard.Set("RaceStarted", true);
                    Debug.Log("[Race] START AREA: RACE STARTED!");

                    // Signal GameManager to start the race
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.StartRace();
                    }
                }
            }
        }

        protected override void OnAreaEnter(BaseObject owner, Blackboard blackboard, Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            // Detect player using Player script
            var player = other.GetComponentInParent<Player>();
            if (player == null) return;

            // Start countdown if not already started
            if (!blackboard.Get<bool>("IsCountingDown", false) && !GameManager.Instance.RaceStarted)
            {
                // IMPORTANT: Read from Blackboard to get potentially overridden values
                float duration = blackboard.Get<float>("_countdownDuration", 3f);

                blackboard.Set("Timer", duration);
                blackboard.Set("IsCountingDown", true);
                Debug.Log($"[Race] START AREA: Countdown started ({duration}s)");
            }
        }

        protected override void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected override void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other) { }
    }
}
