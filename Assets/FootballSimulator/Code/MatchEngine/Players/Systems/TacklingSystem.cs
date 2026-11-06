using UnityEngine;

using FStudio.MatchEngine.Graphics;
using FStudio.MatchEngine.Enums;
using FStudio.Animation;
using FStudio.MatchEngine.Balls;
using FStudio.MatchEngine.Events;
using FStudio.Events;
using FStudio.MatchEngine.Players.PlayerController;
using FStudio.MatchEngine.Graphics.EventRenderer;

namespace FStudio.MatchEngine.Players {
    public partial class PlayerBase {
        #region tackling
        public const float TACKLING_DISTANCE = 2f;
        private const float TACKLING_RESPONSE_TIME = 0.25f;
        private const float TACKLING_RECOVERY_TIME_FOR_TACKLER = 1.2f;
        private const float TACKLING_RECOVERY_TIME_FOR_TACKLED = 2.25f;
        #endregion

        private readonly string[] spinBlockers = new string[1] { "Spin" };

        /// <summary>
        /// Do a tackling, if you are far away from the ball, you won't get the ball:=)
        /// </summary>
        /// <param name="ball"></param>
        public void DoTackle (Ball ball, float reach = -1f) {
            if (IsHoldingBall) {
                return;
            }

            EventManager.Trigger(new PlayerSlideTackleEvent(this));

            CurrentAct = Acts.Tackling;

            PlayerController.IsPhysicsEnabled = false;

            PlayerController.Animator.SetTrigger(PlayerAnimatorVariable.Tackling);

            DirtRenderer.Current.SetPosition(1, Position, Rotation);

            if (ball.HolderPlayer.IsGK) {
                return;
            }

            new TimerAction(TACKLING_RESPONSE_TIME).GetQuery().Start(MatchManager.Current, () => {
                calculateTackling();
            });

            void restoreTackler() {
                // tackler restore.
                new TimerAction(TACKLING_RECOVERY_TIME_FOR_TACKLER).GetQuery().Start(MatchManager.Current, () => {
                    PlayerController.IsPhysicsEnabled = true;
                });
            }

            void calculateTackling() {
                if (ball.HolderPlayer == null || 
                    ball.HolderPlayer.IsGKUntouchable || 
                    ball.HolderPlayer.GameTeam == GameTeam) {

                    restoreTackler();

                    return;
                }

                if (Vector3.Distance(ball.HolderPlayer.Position, Position) < (reach > 0f ? reach : TACKLING_DISTANCE)) {
                    var ballKeeping = ball.HolderPlayer.MatchPlayer.ActualBallKeeping;
                    var tackling = MatchPlayer.ActualTackling;

                    bool isTacklingSuccess = Random.Range(ballKeeping * 0.5f, ballKeeping) <
                        Random.Range(tackling / EngineSettings.Current.Tackling_Difficulty, tackling);

                    if (isTacklingSuccess) {
                        restoreTackler();

                        EventManager.Trigger(new PlayerWinTheBallEvent (this));
                        EventManager.Trigger(new PlayerLossTheBallEvent (ball.HolderPlayer));
                        EventManager.Trigger(new PlayerTackledEvent(ball.HolderPlayer));

                        // ball holder 
                        var ballHolder = ball.HolderPlayer;
                        ballHolder.PlayerController.IsPhysicsEnabled = false;
                        ballHolder.ballHitAnimationEvent = BallHitAnimationEvent.None;
                        ballHolder.PlayerController.Animator.SetTrigger(PlayerAnimatorVariable.Tackled);

                        new TimerAction(TACKLING_RECOVERY_TIME_FOR_TACKLED).GetQuery().Start(MatchManager.Current, () => {
                            ballHolder.CurrentAct = Acts.Stunned;

                            ballHolder.PlayerController.IsPhysicsEnabled = true;
                        });

                        ball.Release(); // release the ball from the holder.
                    } else {
                        var victim = ball.HolderPlayer;
                        if (victim != null && Vector3.Distance(victim.Position, Position) < 2.6f)
                            MaybeFoul(victim, true);

                        // spin anim.
                        if (ball.HolderPlayer.Velocity.magnitude > 2 && 
                            ball.HolderPlayer.ballHitAnimationEvent == BallHitAnimationEvent.None && 
                            !ball.HolderPlayer.PlayerController.IsAnimationABlocker (in spinBlockers)) {

                            ball.HolderPlayer.PlayerController.Animator.SetTrigger(PlayerAnimatorVariable.Spin);
                        }

                        CurrentAct = Acts.TacklingFailed;

                        restoreTackler();
                    }
                } else {
                    restoreTackler();
                }
            }
        }

        public void StandTackle(Ball ball) {
            if (IsHoldingBall || ball == null || ball.HolderPlayer == null)
                return;
            var holder = ball.HolderPlayer;
            if (holder.IsGK || holder.IsGKUntouchable || holder.GameTeam == GameTeam)
                return;
            if (Vector3.Distance(holder.Position, Position) > 2.4f)
                return;

            CurrentAct = Acts.Tackling;
            PlayerController.Animator.SetTrigger(PlayerAnimatorVariable.Struggle);

            var keeping = holder.MatchPlayer.ActualBallKeeping;
            var tackling = MatchPlayer.ActualTackling;
            var success = Random.Range(keeping * 0.5f, keeping) <
                Random.Range(tackling / EngineSettings.Current.Tackling_Difficulty, tackling);
            if (!success) {
                MaybeFoul(holder, false);
                return;
            }

            EventManager.Trigger(new PlayerWinTheBallEvent(this));
            EventManager.Trigger(new PlayerLossTheBallEvent(holder));
            holder.ballHitAnimationEvent = BallHitAnimationEvent.None;
            holder.PlayerController.Animator.SetTrigger(PlayerAnimatorVariable.Struggle);
            ball.Release();
        }

        public void Tackle(Ball ball, float reach = -1f) {
            // TRY TO TACKLE.
            if (ball.HolderPlayer == null) {
                return; // no one holds the ball. No tackling possible.
            }

            DoTackle(ball, reach);
        }

        void MaybeFoul(PlayerBase victim, bool slide) {
            if (victim == null || MatchManager.Current == null)
                return;
            if (!MatchManager.Current.MatchFlags.HasFlag(MatchStatus.Playing) || MatchManager.Current.MatchFlags.HasFlag(MatchStatus.Freeze))
                return;
            var approach = Position - victim.Position;
            approach.y = 0f;
            var behind = approach.sqrMagnitude > 0.01f && Vector3.Dot(approach.normalized, victim.Rotation * Vector3.forward) > 0.25f;
            var chance = slide ? (behind ? 0.7f : 0.4f) : (behind ? 0.46f : 0.24f);
            if (Random.value > chance)
                return;
            var spot = victim.Position;
            spot.y = 0f;
            var field = MatchManager.Current.SizeOfField;
            spot.x = Mathf.Clamp(spot.x, 1.5f, field.x - 1.5f);
            spot.z = Mathf.Clamp(spot.z, 1.5f, field.y - 1.5f);
            MatchManager.Current.Foul(FoulType.Foul, spot, victim.GameTeam, FStudio.Data.Positions.ParametersCount);
            CoachMaster.UI.Scenes.SimulatorMatchHost.LiveFoul(this, victim);
        }
    }
}
