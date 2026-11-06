using FStudio.MatchEngine.Enums;
using UnityEngine;
using FStudio.MatchEngine.Players.PlayerController;
using FStudio.MatchEngine.Players.InputBehaviours;

namespace FStudio.MatchEngine.Players.Behaviours {
    public class InputTackleBehaviour : BaseBehaviour, IInputBehaviour {
        public MovementType MovementType { get; private set; }
        public bool IsTriggered { get; set; }
        public Vector3 InputDirection { set; private get; }

        bool _slid;

        public override bool Behave(bool isAlreadyActive) {
            if (!IsTriggered && !isAlreadyActive) {
                _slid = false;
                return false;
            }

            if (!Player.isInputControlled) {
                IsTriggered = false;
                return false;
            }

            if (Player.IsThrowHolder) {
                IsTriggered = false;
                return false;
            }

            if (Player.IsCornerHolder) {
                IsTriggered = false;
                return false;
            }

            if (Player.IsHoldingBall) {
                IsTriggered = false;
                return false;
            }

            var playerPos = Player.Position;

            Vector3 targetPos;

            var holderPlayer = ball.HolderPlayer;
            if (holderPlayer == null) {
                var loose = ball.transform.position;
                loose.y = 0f;
                var gap = Vector3.Distance(playerPos, loose);
                if (gap < 1.35f && ball.transform.position.y < 1.8f)
                    ball.Hold(Player);
                else {
                    Player.CurrentAct = Acts.GoingToTackle;
                    Player.MoveTo(deltaTime, loose, true, MovementType.BestHeCanDo);
                    if (!_slid && gap < 3.2f) {
                        _slid = true;
                        Player.PlayerController.Animator.SetTrigger(PlayerAnimatorVariable.Tackling);
                    }
                }
                return true;
            }

            if (holderPlayer.GameTeam == Player.GameTeam)
                return false;

            var holderPlayerPos = holderPlayer.Position;
            targetPos = PlayerBase.Predicter(Player, holderPlayer);

            if (Vector3.Distance(holderPlayerPos, playerPos) < 6f) {
                Player.Tackle(ball, 6f);
                return true;
            }

            Player.CurrentAct = Acts.GoingToTackle;

            Player.AvoidMarkers(teammates, ref targetPos, 3);

            // we need to go for tackling.
            Player.MoveTo(deltaTime, targetPos, true);

            return true;
        }
    }
}
