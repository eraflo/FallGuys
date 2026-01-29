using System.Threading;
using Eraflo.Common.ObjectSystem;
using Eraflo.Common.PlatformSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.PlatformSystem.States
{
    /// <summary>
    /// State that moves a platform between two points.
    /// </summary>
    [CreateAssetMenu(fileName = "PlatformMoveState", menuName = "StateMachine/States/Platform/Move")]
    public class PlatformMoveStateSO : StateBaseSO
    {
        [SerializeField] private bool _movingToEnd = true;

        public override void OnEnter(Blackboard bb, CancellationToken ct)
        {
            bb.Set("_moveStartTime", Time.time);

            // Store original position if not already present
            if (!bb.Has("_initialPos"))
            {
                bb.Set("_initialPos", bb.GetOwnerObject().transform.position);
            }
        }

        public override void OnServerUpdate(Blackboard bb)
        {
            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            MovingPlatformSO config = baseObj.RuntimeData.Config as MovingPlatformSO;
            if (config == null) return;

            float startTime = bb.Get<float>("_moveStartTime");
            float elapsed = Time.time - startTime;

            // Read overridden values from Blackboard
            float travelTime = bb.Get<float>("_travelTime", config.TravelTime);
            Vector3 startOffset = bb.Get<Vector3>("_startOffset", config.StartOffset);
            Vector3 endOffset = bb.Get<Vector3>("_endOffset", config.EndOffset);

            float t = Mathf.Clamp01(elapsed / travelTime);

            Vector3 initialPos = bb.Get<Vector3>("_initialPos");
            Vector3 startPos = initialPos + startOffset;
            Vector3 endPos = initialPos + endOffset;

            // Move from A to B or B to A
            owner.transform.position = _movingToEnd
                ? Vector3.Lerp(startPos, endPos, t)
                : Vector3.Lerp(endPos, startPos, t);
        }
    }
}
