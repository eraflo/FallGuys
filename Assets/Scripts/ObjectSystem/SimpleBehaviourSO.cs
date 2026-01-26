using FallGuys.StateMachine;
using ObjectSystem;
using UnityEngine;

namespace FallGuys.ObjectSystem
{
    public abstract class SimpleBehaviourSO : LogicIdentitySO
    {
        public virtual void OnStart(BaseObject owner, Blackboard blackboard) { }
        public virtual void OnUpdate(BaseObject owner, Blackboard blackboard) { }
    }
}