using Eraflo.Common.AreaSystem;
using Eraflo.Common.ObjectSystem;
using FallGuys.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.AreaSystem
{
    public abstract class AreaBehaviourSO : SimpleBehaviourSO
    {
        public override void OnStart(BaseObject owner, Blackboard blackboard)
        {
            if (owner is AreaDetector areaDetector)
            {
                // Sync initial size/radius from blackboard (handles LevelEditable overrides)
                if (blackboard.Has("_areaSize"))
                {
                    areaDetector.SetSize(blackboard.Get<Vector3>("_areaSize"));
                }
                if (blackboard.Has("_radius"))
                {
                    areaDetector.SetRadius(blackboard.Get<float>("_radius"));
                }
                if (blackboard.Has("_capsuleHeight"))
                {
                    areaDetector.SetHeight(blackboard.Get<float>("_capsuleHeight"));
                }
                if (blackboard.Has("_capsuleDirection"))
                {
                    areaDetector.SetDirection(blackboard.Get<int>("_capsuleDirection"));
                }

                areaDetector.onTriggerEnter += (other) => OnAreaEnter(owner, blackboard, other);
                areaDetector.onTriggerStay += (other) => OnAreaStay(owner, blackboard, other);
                areaDetector.onTriggerExit += (other) => OnAreaExit(owner, blackboard, other);
            }
        }

        protected abstract void OnAreaEnter(BaseObject owner, Blackboard blackboard, Collider other);
        protected abstract void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other);
        protected abstract void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other);
    }
}
