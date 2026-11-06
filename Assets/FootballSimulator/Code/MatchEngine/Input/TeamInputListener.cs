using UnityEngine;
using UnityEngine.InputSystem;

using FStudio.MatchEngine.Players;
using FStudio.Input;
using FStudio.MatchEngine.Balls;
using FStudio.MatchEngine.Enums;

using System.Linq;

using FStudio.MatchEngine.Players.Behaviours;
using FStudio.MatchEngine.Players.InputBehaviours;
using FStudio.Events;
using FStudio.MatchEngine.Events;
using FStudio.MatchEngine.Tactics;
using FStudio.MatchEngine.Cameras;
using FStudio.MatchEngine.Players.PlayerController;
using static UnityEngine.Rendering.DebugUI;

namespace FStudio.MatchEngine.Input {
    public class TeamInputListener : InputListener {
        private static Vector3 inputWorldOffset = Vector3.up * 2;

        public enum ActionType {
            None,
            Pass,
            Shoot
        }

        private const float MOVE_DEADZONE = 0.3f;

        float _shootPressed;

        private PlayerBase m_ActivePlayer;
        public PlayerBase ActivePlayer { get => m_ActivePlayer; private set {
                Debug.Log("Assigned active player: " + value);
                m_ActivePlayer = value;
            } }

        private readonly GameTeam gameTeam;


        private Vector3 m_lastValidDirection;
        float _skillTime;
        bool _rightFlick;

        /// <summary>
        /// Active direction
        /// </summary>
        private Vector3 direction;

        private readonly Transform inputPointer;
        private readonly Transform inputPointerFollower;

        public TeamInputListener(
            int playerIndex, 
            GameTeam gameTeam) : base("MatchEngine", playerIndex) {

            this.gameTeam = gameTeam;

            inputPointer = Object.Instantiate(Resources.Load<Transform>("UI/InputPointer"));
            inputPointerFollower = inputPointer.GetChild(0);

            // create direction listener.
            RegisterAction("Move", MoveInput);
            RegisterAction("ChangePlayer", ChangePlayerInput);
            RegisterAction("Pass", PassInput);
            RegisterAction("ThroughtPass", ThroughtPass);
            RegisterAction("Tackle", TackleInput);
            RegisterAction("Shoot", ShootInput);
            RegisterAction("Cross", CrossInput);
            RegisterAction("ChangeTacticHigh", ChangeTacticHighInput);
            RegisterAction("ChangeTacticLow", ChangeTacticLowInput);
            //

            Debug.Log("Team Input Listener Created.");
        }

        ~TeamInputListener () {
            Clear();
        }

        private bool ChangeTacticHighInput(InputAction.CallbackContext ctx) {
            UpdateTactic(true);
            return true;
        }

        private bool ChangeTacticLowInput(InputAction.CallbackContext ctx) {
            UpdateTactic(false);
            return true;
        }

        private void UpdateTactic(bool increase) {
            var currentTactic = (int)MatchManager.Current.UserTeam.Team.TacticPresetType;
            currentTactic += increase ? 1 : -1;
            if (currentTactic < 0) {
                currentTactic = (int)TacticPresetTypes.ParameterCount - 1;
            } else if (currentTactic >= (int)TacticPresetTypes.ParameterCount) {
                currentTactic = 0;
            }

            var newTactic = (TacticPresetTypes)currentTactic;

            MatchManager.Current.UserTeam.Team.TacticPresetType = newTactic;
            EventManager.Trigger(new TeamChangedTactic(MatchManager.Current.UserTeam, newTactic));
        }

        private bool MoveInput (InputAction.CallbackContext ctx) {
            if (MatchPause.IsPaused) {
                return false;
            }

            if (ActivePlayer == null || !ActivePlayer.PlayerController.IsPhysicsEnabled) {
                direction = Vector3.zero;
                return true;
            }

            var value = ctx.ReadValue<Vector2>();

            var camMod = CameraSystem.Current.transform.rotation;
            var camMul = Quaternion.Euler(0, camMod.eulerAngles.y, 0);

            direction = camMul * new Vector3(value.x, 0, value.y);

            return true;
        }

        private void ActivatePlayerBehaviour<T>() where T : BaseBehaviour, IInputBehaviour {
            foreach (var behaviour in ActivePlayer.Behaviours) {
                if (behaviour is IInputBehaviour other && behaviour is not T)
                    other.IsTriggered = false;
            }

            var tBehaviour = ActivePlayer.Behaviours.
                Where(x => x is T).
                Select(x => (T)x).
                FirstOrDefault();
            if (tBehaviour == null)
                return;

            tBehaviour.IsTriggered = true;

            var typeName = typeof(T).Name.Split('.').Last();

            Debug.Log("[TeamInputListener]" + typeName);

            ActivePlayer.ActivateBehaviour(typeName);
        }

        private void DectivatePlayerBehaviour<T>() where T : BaseBehaviour, IInputBehaviour {
            var tBehaviour = ActivePlayer.Behaviours.
                Where(x => x is T).
                Select(x => (T)x).
                FirstOrDefault();
            if (tBehaviour == null)
                return;

            tBehaviour.IsTriggered = false;

            ActivePlayer.ResetBehaviours();
        }

        private bool PassInput(InputAction.CallbackContext ctx) {
            if (MatchPause.IsPaused) {
                return false;
            }

            var value = ctx.ReadValue<float>();

            if (ActivePlayer == null || 
                !ActivePlayer.PlayerController.IsPhysicsEnabled) {
                return true;
            }

            if (value == 1) {
                if (ActivePlayer.IsHoldingBall && (Time.time - _shootPressed < 0.65f
                    || ActivePlayer.ActiveBehaviour is InputShootBehaviour
                    || ActivePlayer.ballHitAnimationEvent == BallHitAnimationEvent.Shoot)) {
                    DectivatePlayerBehaviour<InputShootBehaviour>();
                    ActivePlayer.FakeShot();
                    return true;
                }
                ActivatePlayerBehaviour<InputShortPassBehaviour>();
            }

            return true;
        }

        private bool ShootInput (InputAction.CallbackContext ctx) {
            if (MatchPause.IsPaused) {
                return false;
            }

            var value = ctx.ReadValue<float>();

            if (ActivePlayer == null || 
                !ActivePlayer.PlayerController.IsPhysicsEnabled) {
                return true;
            }

            if (value == 1) {
                if (!ActivePlayer.IsHoldingBall) {
                    ActivatePlayerBehaviour<InputStandTackleBehaviour>();
                    return true;
                }
                _shootPressed = Time.time;
                ActivatePlayerBehaviour<InputShootBehaviour>();
            }

            return true;
        }

        private bool ThroughtPass (InputAction.CallbackContext ctx) {
            if (MatchPause.IsPaused) {
                return false;
            }

            if (ActivePlayer == null ||
                !ActivePlayer.PlayerController.IsPhysicsEnabled) {
                return true;
            }

            if (!ActivePlayer.IsHoldingBall) {
                return false;
            }

            ActivatePlayerBehaviour<InputThroughtPassBehaviour>();
            return true;
        }

        private bool CrossInput (InputAction.CallbackContext ctx) {
            if (MatchPause.IsPaused) {
                return false;
            }

            var value = ctx.ReadValue<float>();

            if (ActivePlayer == null ||
                !ActivePlayer.PlayerController.IsPhysicsEnabled) {
                return true;
            }

            if (!ActivePlayer.IsHoldingBall) {
                return false;
            }

            if (value == 1) {
                ActivatePlayerBehaviour<InputCrossBehaviour>();
                return true;
            }

            return false;
        }

        private bool TackleInput(InputAction.CallbackContext ctx) {
            if (MatchPause.IsPaused) {
                return false;
            }

            if (ActivePlayer == null || !ActivePlayer.PlayerController.IsPhysicsEnabled) {
                return true;
            }

            if (ActivePlayer.IsHoldingBall) {
                return false;
            }

            if (ctx.control.IsPressed()) {
                Debug.Log("[TackleInput] Tackle on");
                ActivatePlayerBehaviour<InputTackleBehaviour>();
            } else {
                Debug.Log("[TackleInput] Tackle off");
                DectivatePlayerBehaviour<InputTackleBehaviour>();
            }

            return true;
        }

        private bool ChangePlayerInput(InputAction.CallbackContext ctx) {
            if (MatchPause.IsPaused) {
                return false;
            }

            var value = ctx.ReadValue<float>();
            if (value == 1) {
                Debug.Log("[TeamInputListener] Change Player");
                AssignPlayer();
            }

            return true;
        }

        public void Receive(PlayerBase player) {
            if (player != null && player.GameTeam == gameTeam)
                ActivePlayer = player;
        }

        static bool Sprinting() {
            var pad = Gamepad.current;
            if (pad != null && pad.rightTrigger.ReadValue() > 0.45f)
                return true;
            var keys = Keyboard.current;
            return keys != null && (keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed);
        }

        private void AssignPlayer () {
            var currentBallHolder = Ball.Current.HolderPlayer;

            if (currentBallHolder != null && !MatchManager.Alive(currentBallHolder))
                currentBallHolder = null;

            if (currentBallHolder != null && ActivePlayer == currentBallHolder) {
                return;// cannot select another player.
            }

            var possibleTargets = gameTeam.GamePlayers.Where(x => MatchManager.Alive(x) && !x.IsGK && x != ActivePlayer);

            if (currentBallHolder != null && currentBallHolder.GameTeam == gameTeam && !currentBallHolder.IsGK) {
                ActivePlayer = currentBallHolder;
                return;
            }

            if (currentBallHolder != null && currentBallHolder.GameTeam != gameTeam) {
                // opponent has the ball.
                // select tacklers.

                #region select from tacklers
                var tacklers = possibleTargets.Where(x => x.ActiveBehaviour is TryToTackleBehaviour).Select (x=>x.ActiveBehaviour as TryToTackleBehaviour).ToArray ();

                if (tacklers.Length > 0) {
                    //select one of the tacklers.
                    ActivePlayer = tacklers.OrderBy(x => x.MovementType).FirstOrDefault ().Player;
                    return;
                }
                #endregion
            }
            else {
                // ball is free, select one of chasers.

                #region select from receivers
                var receivers = possibleTargets.Where(x => x.ActiveBehaviour is BallChasingWithoutCondition).Select(x => x.ActiveBehaviour as BallChasingWithoutCondition).ToArray();
                if (receivers.Length > 0) {
                    ActivePlayer = receivers.OrderBy (x=>x.ChasingDistance).FirstOrDefault ().Player;
                    return;
                }
                #endregion

                #region select from chasers
                var chasers = possibleTargets.Where(x => x.ActiveBehaviour is BallChasingBehaviour).ToArray();

                if (chasers.Length > 0) {
                    //select one of the tacklers.
                    ActivePlayer = chasers.OrderBy(x => BallChasingBehaviour.BallChasingDistance(x)).FirstOrDefault();
                    return;
                }
                #endregion

                // select the best option.
                var bestOption = possibleTargets.
                    OrderBy(x => BallChasingBehaviour.BallChasingDistance(x)).FirstOrDefault();

                ActivePlayer = bestOption;
            }
        }

        public void Update (in float deltaTime) {
            if (ActivePlayer == null || !MatchManager.Alive(ActivePlayer)) {
                ActivePlayer = null;
                AssignPlayer();
            }

            if (Ball.Current == null || !MatchManager.Alive(ActivePlayer))
                return;

            var holderPlayer = Ball.Current.HolderPlayer;
            if (holderPlayer != null && !MatchManager.Alive(holderPlayer))
                holderPlayer = null;

            if (ActivePlayer != holderPlayer && holderPlayer != null && !holderPlayer.IsGK && holderPlayer.GameTeam == gameTeam) {
                ActivePlayer = holderPlayer;
            }

            if (ActivePlayer == null) {
                return;
            }

            if (direction.magnitude > 0.2f) {
                m_lastValidDirection = direction;
            }

            if (m_lastValidDirection.magnitude < 0.2f) {
                m_lastValidDirection = ActivePlayer.Rotation * Vector3.forward;
            }

            var behaviours = ActivePlayer.Behaviours.
                Where(x => x is IInputBehaviour).
                Select(x => (IInputBehaviour)x);

            foreach (var ib in behaviours) {
                ib.InputDirection = ib.InputDirection = m_lastValidDirection;
            }

            if (ActivePlayer.PlayerController.IsPhysicsEnabled) {
                if (!MatchManager.Current.MatchFlags.HasFlag( MatchStatus.Playing )) {
                    if (ActivePlayer.IsHoldingBall) {
                        ActivePlayer.LookTo(in deltaTime, direction);
                    }
                }
            }


            if (ActivePlayer.ActiveBehaviour is not IInputBehaviour) {
                TryRightStickSkill();
                var loose = !ActivePlayer.IsHoldingBall && Ball.Current.HolderPlayer == null;
                if (loose) {
                    var point = Ball.Current.BallPosition(ActivePlayer);
                    point.y = 0f;
                    if (direction.magnitude > MOVE_DEADZONE)
                        point += direction.normalized * 1.1f;
                    ActivePlayer.MoveTo(in deltaTime, point, true, Sprinting() ? MovementType.BestHeCanDo : MovementType.Normal);
                } else {
                    var length = direction.magnitude;

                    var activePosition = ActivePlayer.Position;
                    activePosition += direction.normalized * 2;

                    if (length < MOVE_DEADZONE) {
                        ActivePlayer.Stop(in deltaTime);
                    } else {
                        var sprinting = Sprinting();
                        ActivePlayer.MoveTo(in deltaTime, activePosition, true, sprinting ? MovementType.BestHeCanDo : MovementType.Relax);
                        if (!sprinting && ActivePlayer.PlayerController is CodeBasedController body)
                            body.TargetMoveSpeed = Mathf.Clamp(body.TargetMoveSpeed * 2.2f, 0f, 0.55f);
                    }
                }
            }

            // reposition UI Input pointer.
            var cam = Camera.main;
            if (cam == null || inputPointerFollower == null)
                return;

            var arrowIndicatorTransform =
                GetArrowIndicatorPositionAndAngle(cam.WorldToScreenPoint(ActivePlayer.Position + inputWorldOffset));

            inputPointerFollower.position = arrowIndicatorTransform.position;
            inputPointerFollower.rotation = arrowIndicatorTransform.rotation;
        }

        void TryRightStickSkill() {
            if (ActivePlayer == null)
                return;
            var pad = Gamepad.current;
            if (pad == null)
                return;
            var flick = pad.rightStick.ReadValue();
            if (!ActivePlayer.IsHoldingBall) {
                var pushed = flick.sqrMagnitude > 0.45f;
                if (pushed && !_rightFlick)
                    AssignPlayer();
                _rightFlick = pushed;
                return;
            }
            _rightFlick = false;
            if (Time.time < _skillTime)
                return;
            if (flick.sqrMagnitude < 0.4f)
                return;
            _skillTime = Time.time + 0.7f;
            var camMod = CameraSystem.Current != null ? CameraSystem.Current.transform.rotation : Quaternion.identity;
            var world = Quaternion.Euler(0f, camMod.eulerAngles.y, 0f) * new Vector3(flick.x, 0f, flick.y);
            ActivePlayer.SkillMove(world);
        }

        private (Vector2 position, Quaternion rotation) GetArrowIndicatorPositionAndAngle(
                    Vector3 originalPosition) {

            bool isInBounds(Vector3 pos, Vector2 screen) {
                return !(pos.x > screen.x - 50 || pos.x < 50 || pos.y > screen.y - 50 || pos.y < 50);
            }

            var screen = new Vector2(Screen.width, Screen.height);

            if (!isInBounds(originalPosition, screen)) {
                var ballOnScreen = Camera.main.WorldToScreenPoint(Ball.Current.transform.position);
                var dir = (originalPosition - ballOnScreen).normalized;

                originalPosition = ballOnScreen;

                while (isInBounds(originalPosition, screen)) {
                    originalPosition += dir;
                }

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);

                return (originalPosition, q);
            }

            return (originalPosition, Quaternion.Euler(0, 0, -90));
        }
    }
}
