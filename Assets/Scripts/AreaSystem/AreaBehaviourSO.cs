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
            owner.onTriggerEnter += (other) => OnAreaEnter(owner, blackboard, other);
            owner.onTriggerStay += (other) => OnAreaStay(owner, blackboard, other);
            owner.onTriggerExit += (other) => OnAreaExit(owner, blackboard, other);
        }

        protected abstract void OnAreaEnter(BaseObject owner, Blackboard blackboard, Collider other);
        protected abstract void OnAreaStay(BaseObject owner, Blackboard blackboard, Collider other);
        protected abstract void OnAreaExit(BaseObject owner, Blackboard blackboard, Collider other);
    }
}
