
using FStudio.MatchEngine;
using FStudio.MatchEngine.Enums;
using UnityEngine;
using System.Linq;

namespace FStudio.MatchEngine.Players.Behaviours {
    public class KickOffBehaviour : BaseBehaviour {
        private PlayerBase teammateToPass;

        public override bool Behave(bool isAlreadyActive) {
            if (matchStatus != MatchStatus.WaitingForKickOff) {
                return false;
            }

            if (ball.HolderPlayer != Player) {
                return false;
            }

            if (!isAlreadyActive) {
                var field = MatchManager.Current.SizeOfField;
                var forward = Player.toGoalXDirection;
                teammateToPass = teammates.Where(x =>
                    x != Player &&
                    !x.IsGK &&
                    (x.Position.x - Player.Position.x) * forward > 3f &&
                    x.Position.z > field.y * 0.32f &&
                    x.Position.z < field.y * 0.68f &&
                    Vector3.Distance(x.Position, Player.Position) < 22f).
                    OrderBy(x => Vector3.Distance(x.Position, Player.Position)).
                    FirstOrDefault();

                if (teammateToPass == null) {
                    teammateToPass = teammates.Where(x => x != Player && !x.IsGK).
                        OrderBy(x => Mathf.Abs(x.Position.z - field.y * 0.5f)).
                        FirstOrDefault();
                }

                if (teammateToPass != null) {
                    isAlreadyActive = true;
                }
            }

            if (isAlreadyActive && teammateToPass != null) {
                var point = Vector3.Lerp(Player.Position, teammateToPass.Position, 0.82f);
                point.y = 0f;
                if (Player.PassToTarget(in deltaTime, point)) {
                    Player.PassingTarget = teammateToPass;
                }
            }

            return true;
        }
    }
}
