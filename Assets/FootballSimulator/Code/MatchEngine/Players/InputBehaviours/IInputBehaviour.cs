using UnityEngine;

namespace FStudio.MatchEngine.Players.InputBehaviours {
    public interface IInputBehaviour {
        public bool IsTriggered { get; set; }

        public Vector3 InputDirection { set; }
    }
}
