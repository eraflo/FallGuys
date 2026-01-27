using Eraflo.Common.AreaSystem;
using Eraflo.Common.ObjectSystem;
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
                }
            }
        }

        public override void OnStart(BaseObject owner, Blackboard blackboard)
        {
            base.OnStart(owner, blackboard);

            if (!NetworkManager.Singleton.IsServer) return;

            // When the level is loaded and the Start Zone is initialized, spawn the players!
            if (PlayerManager.Singleton != null)
            {
                Debug.Log("[Race] START AREA: Level loaded, triggering player spawn.");
                PlayerManager.Singleton.SpawnPlayers();
            }
            else
            {
                Debug.LogError("[Race] START AREA: PlayerManager.Singleton not found! Cannot spawn players.");
            }
        }

        protected override void OnAreaEnter(BaseObject owner, Blackboard blackboard, Collider other)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            if (!blackboard.Get<bool>("IsCountingDown", false) && !blackboard.Get<bool>("RaceStarted", false))
            {
                if (owner.RuntimeData.Config is StartAreaSO startSO)
                {
                    blackboard.Set("Timer", startSO.CountdownDuration);
                    blackboard.Set("IsCountingDown", true);
                    Debug.Log($"[Race] START AREA: Countdown started ({startSO.CountdownDuration}s)");
                }
            }
        }

        protected override void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other) { }
        protected override void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other) { }
    }
}
