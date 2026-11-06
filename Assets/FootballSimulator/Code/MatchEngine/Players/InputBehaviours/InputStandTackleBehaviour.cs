using FStudio.MatchEngine.Enums;
using FStudio.MatchEngine.Players.InputBehaviours;
using FStudio.MatchEngine.Players.PlayerController;
using UnityEngine;

namespace FStudio.MatchEngine.Players.Behaviours {
    public class InputStandTackleBehaviour : BaseBehaviour, IInputBehaviour {
        public bool IsTriggered { get; set; }
        public Vector3 InputDirection { set; private get; }

        public override bool Behave(bool isAlreadyActive) {
            if (!IsTriggered && !isAlreadyActive)
                return false;

            if (!Player.isInputControlled || Player.IsHoldingBall || Player.IsThrowHolder || Player.IsCornerHolder) {
                IsTriggered = false;
                return false;
            }

            var holder = ball.HolderPlayer;
            if (holder == null || holder.GameTeam == Player.GameTeam) {
                IsTriggered = false;
                return false;
            }

            var distance = Vector3.Distance(holder.Position, Player.Position);
            if (distance <= 2.4f) {
                IsTriggered = false;
                Player.StandTackle(ball);
                return false;
            }

            if (distance > 4.5f) {
                IsTriggered = false;
                return false;
            }

            Player.CurrentAct = Acts.GoingToTackle;
            Player.MoveTo(deltaTime, holder.Position, true, MovementType.BestHeCanDo);
            return true;
        }
    }
}
