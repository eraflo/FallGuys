using FallGuys.StateMachine;
using ObjectSystem;
using UnityEngine;

namespace FallGuys.ObjectSystem
{
    public abstract class SimpleBehaviourSO : LogicIdentitySO
    {
        public abstract void OnStart(BaseObject owner, Blackboard blackboard);
        public abstract void OnUpdate(BaseObject owner, Blackboard blackboard);
    }
}