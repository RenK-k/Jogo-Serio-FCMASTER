using System;
using System.Collections.Generic;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.Presentation;
using CoachMaster.UI;
using UnityEngine;

namespace CoachMaster.MatchPlay
{
    public sealed class MatchEngine
    {
        const float FixedStep = 1f / 60f;
        const float MaxFrameDelta = 0.2f;
        const float ReferenceMatchSpeed = 0.68f;
        const int MaxStepsPerTick = 12;
        readonly MatchSession _match;
        readonly System.Random _rng;
        readonly float _injuryMultiplier;
        readonly float _refereeMultiplier;
        float _stepAccumulator;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        float _nextDiagnosticTime;
#endif
        public Vector2 Ball { get; private set; } = new Vector2(0.5f, 0.5f);
        Vector2 _ballVelocity;
        Vector2 _ballTarget = new Vector2(0.5f, 0.5f);
        public Vector2 BallTarget
        {
            get => _ballTarget;
            private set
            {
                if (!IsFinite(value) || value.x < -0.12f || value.x > 1.12f || value.y < -0.12f || value.y > 1.12f)
                {
                    Diagnose("bola", "alvo inválido", _ballTarget, value, 0f);
                    _ballTarget = IsFinite(Ball) ? Ball : new Vector2(0.5f, 0.5f);
                    SnapBallVisual();
                    return;
                }
                _ballTarget = value;
            }
        }
        public float ActionTime { get; private set; }
        public string ActionKind { get; private set; } = "kickoff";
        public float Hold { get; private set; }
        public float Travel { get; private set; } = 0.34f;
        public float BallHeight { get; private set; }
        public float MovementSpeedScale { get; private set; } = 1f;
        public bool DeadBall => Hold > 0f || ActionKind == "goal" || ActionKind == "corner" || ActionKind == "card" || ActionKind == "injury" || ActionKind == "kickoff" || ActionKind == "freekick" || ActionKind == "throwin" || ActionKind == "penalty" || ActionKind == "goalkick";
        readonly Dictionary<string, float> _load = new Dictionary<string, float>();
        bool _pieceLive;
        bool _pieceHome;
        bool _piecePenalty;
        float _quietCards;
        float _nextPositionSampleMinute = 0.5f;
        Vector2 _pieceSpot;
        int _pieceTaker;
        int[] _wall = new int[0];
        string _lastPasser;
        MatchPassEvent _pendingPass;
        readonly int[] _passChain = { -1, -1, -1, -1 };
        int _passSide;
        bool _finishNext;
        bool _finishHome;
        bool _headerNext;
        bool _throughBall;
        float _settle;
        Vector2 _touchAim;
        Vector2 _playAim;
        Vector2 _drawn;
        bool _drawnReady;
        int _intended = -1;
        float _curl;
        readonly Vector2[] _homeMotion = new Vector2[11];
        readonly Vector2[] _awayMotion = new Vector2[11];
        readonly Vector2[] _homePreviousMotion = new Vector2[11];
        readonly Vector2[] _awayPreviousMotion = new Vector2[11];
        int _homeRunner = -1;
        int _awayRunner = -1;
        bool _motionReady;
        bool _lastPasserHome;
        string _pieceKind = "";
        float _flightApex;
        Vector2 _flightFrom;
        float _flightSpan;
        bool _pendingGoal;
        bool _pendingGoalHome;
        bool _pendingPenalty;
        bool _goalCelebration;
        bool _goalRestartHome;
        float _goalCelebrationRemaining;
        string _pendingScorerId;
        string _pendingAssistId;
        string _pendingGoalDescription;
        public MatchEngine(MatchSession match, string userClubId = null, float injuryMultiplier = 1f, float refereeMultiplier = 1f)
        {
            _match = match;
            _injuryMultiplier = Mathf.Clamp(injuryMultiplier, 0.5f, 1.5f);
            _refereeMultiplier = Mathf.Clamp(refereeMultiplier, 0.6f, 1.4f);
            _rng = new System.Random(match.Fixture.Id.GetHashCode());
            Kickoff(true);
        }

        public void Tick(float dt, float minutesPerSecond)
        {
            if (_match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime || _match.Finished)
                return;

            if (!IsFinite(dt) || dt <= 0f)
                return;
            if (!IsFinite(minutesPerSecond) || minutesPerSecond <= 0f)
                minutesPerSecond = ReferenceMatchSpeed;
            MovementSpeedScale = Mathf.Clamp(minutesPerSecond / ReferenceMatchSpeed, 0.25f, 4f);

            _stepAccumulator = Mathf.Min(_stepAccumulator + Mathf.Min(dt, MaxFrameDelta), MaxFrameDelta);
            var steps = 0;
            while (_stepAccumulator + 0.000001f >= FixedStep && steps < MaxStepsPerTick)
            {
                _stepAccumulator -= FixedStep;
                SimulateStep(FixedStep, minutesPerSecond);
                steps++;
                if (_goalCelebration || _match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime || _match.Finished)
                {
                    _stepAccumulator = 0f;
                    break;
                }
            }
        }

        void SimulateStep(float elapsed, float minutesPerSecond)
        {
            if (_match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime || _match.Finished)
                return;

            if (_goalCelebration)
            {
                Ball = BallTarget;
                _drawn = Ball;
                _drawnReady = true;
                BallHeight = 0f;
                _goalCelebrationRemaining -= Mathf.Max(0f, elapsed);
                if (_goalCelebrationRemaining <= 0f)
                {
                    _goalCelebration = false;
                    if (_match.Phase == MatchPhase.SecondHalf && _match.Minute >= 90f)
                        Finish();
                    else
                    {
                        Kickoff(_goalRestartHome);
                        _match.LastEvent = "Pontapé de saída no meio-campo.";
                    }
                }
                return;
            }

            var dt = elapsed * MovementSpeedScale;
            _match.Minute += elapsed * minutesPerSecond;
            var previousBall = Ball;
            var targetDelta = BallTarget - Ball;
            var targetDistance = targetDelta.magnitude;
            var maxSpeed = Mathf.Clamp(Travel, 0.1f, 1.1f);
            var brakingSpeed = Mathf.Sqrt(Mathf.Max(0f, 2.4f * targetDistance));
            var desiredSpeed = Mathf.Min(maxSpeed, brakingSpeed);
            var desiredVelocity = targetDistance > 0.0001f ? targetDelta / targetDistance * desiredSpeed : Vector2.zero;
            _ballVelocity = Vector2.MoveTowards(_ballVelocity, desiredVelocity, 2.4f * dt);
            if (_ballVelocity.magnitude > maxSpeed)
                _ballVelocity = _ballVelocity.normalized * maxSpeed;
            var displacement = _ballVelocity * dt;
            var projectedTravel = targetDistance > 0.0001f
                ? Vector2.Dot(displacement, targetDelta / targetDistance)
                : 0f;
            var lateralTravel = targetDistance > 0.0001f
                ? Mathf.Abs(displacement.x * targetDelta.y - displacement.y * targetDelta.x) / targetDistance
                : 0f;
            if (targetDistance <= 0.0001f)
            {
                Ball = BallTarget;
                _ballVelocity = Vector2.zero;
            }
            else if (projectedTravel >= targetDistance && lateralTravel <= 0.003f)
            {
                Ball = BallTarget;
                _ballVelocity = Vector2.zero;
            }
            else
                Ball += displacement;
            if (!IsFinite(Ball) || Vector2.Distance(previousBall, Ball) > maxSpeed * dt + 0.001f)
            {
                Diagnose("bola", "deslocamento inválido", previousBall, Ball, dt);
                Ball = IsFinite(previousBall) ? previousBall : new Vector2(0.5f, 0.5f);
                _ballVelocity = Vector2.zero;
                SnapBallVisual();
                return;
            }
            PresentBall(dt);
            UpdateFlight();
            if (!_pendingGoal && BallLeftPlay(_match.HomeHasBall))
                return;
            Glide(dt);
            Sweep();
            CapturePositionSample();
            if (_pendingGoal && Vector2.Distance(Ball, BallTarget) <= 0.02f)
            {
                Ball = BallTarget;
                SnapBallVisual();
                CompleteGoal();
                return;
            }
            Drain(dt);
            if (PossessionControlled())
            {
                if (_match.HomeHasBall)
                    _match.HomePossession += dt;
                else
                    _match.AwayPossession += dt;
            }

            if (!_pendingGoal && _match.Phase == MatchPhase.FirstHalf && _match.Minute >= 45f)
            {
                ResolvePendingPass(false, false, null);
                _match.Phase = MatchPhase.HalfTime;
                _match.AddNote("Intervalo. O plantel espera a tua conversa.", "half");
                return;
            }

            if (!_pendingGoal && _match.Phase == MatchPhase.SecondHalf && _match.Minute >= 90f)
            {
                Finish();
                return;
            }

            if (Hold > 0f)
            {
                if (Vector2.Distance(Ball, BallTarget) <= 0.02f)
                    Hold -= dt;
                return;
            }

            if (_settle > 0f)
            {
                var feet = Feet(_match.HomeHasBall, _match.BallHolder);
                if (Vector2.Distance(Ball, feet) > 0.08f)
                    _settle = 0f;
                else
                {
                    BallTarget = feet;
                    var roll = dt * 0.35f;
                    if (roll > 0.02f)
                        roll = 0.02f;
                    Ball = Vector2.MoveTowards(Ball, feet, roll);
                    PresentBall(dt);
                    UpdateFlight();
                    _settle -= dt;
                    return;
                }
            }

            if (TryIntercept(dt))
                return;

            if (Contest(dt))
                return;
            if (Vector2.Distance(Ball, BallTarget) > 0.035f)
                return;

            if (Ball.x < 0f || Ball.x > 1f || Ball.y < 0f || Ball.y > 1f)
            {
                BallLeftPlay(_match.HomeHasBall);
                return;
            }

            if (ActionKind == "pass" || ActionKind == "cross" || ActionKind == "solo" || ActionKind == "turnover" || ActionKind == "loose")
            {
                var side = _match.HomeHasBall;
                var squad = side ? _match.HomeOnField : _match.AwayOnField;
                var near = -1;
                var nearDistance = 99f;
                for (var i = 0; i < squad.Count && i < 11; i++)
                {
                    var distance = Vector2.Distance(Ball, MotionOf(side, i));
                    if (distance < nearDistance)
                    {
                        nearDistance = distance;
                        near = i;
                    }
                }

                if (near >= 0 && nearDistance <= 0.1f)
                {
                    if (ActionKind == "pass")
                        ResolvePendingPass(true, false, squad[near]);
                    _match.BallHolder = near;
                    var control = ControlOf(side, near);
                    var lead = Mathf.Lerp(0.014f, 0.008f, control);
                    var ahead = Ball + (side ? Vector2.up : Vector2.down) * lead;
                    if (Vector2.Distance(Ball, ahead) > 0.004f)
                    {
                        BallTarget = new Vector2(Mathf.Clamp(ahead.x, 0.04f, 0.96f), Mathf.Clamp(ahead.y, 0.04f, 0.96f));
                        _settle = Mathf.Lerp(0.28f, 0.08f, control);
                        ActionKind = "touch";
                        return;
                    }

                    NextAction();
                    return;
                }

                if (ActionKind == "loose")
                {
                    BallTarget = Ball;
                    _ballVelocity = Vector2.zero;
                    _flightFrom = Ball;
                    _flightSpan = 0f;
                    _flightApex = 0f;
                    BallHeight = 0f;
                    return;
                }

                var dir = Ball - _flightFrom;
                if (dir.sqrMagnitude < 0.0004f)
                    dir = side ? Vector2.up : Vector2.down;
                dir.Normalize();
                BallTarget = Ball + dir * 0.3f;
                Travel = 0.42f;
                ActionKind = "loose";
                Launch(0f);
                return;
            }

            NextAction();
        }

        void CapturePositionSample()
        {
            if (_match.Minute + 0.0001f < _nextPositionSampleMinute)
                return;
            var home = new Vector2[11];
            var away = new Vector2[11];
            for (var i = 0; i < 11; i++)
            {
                home[i] = MotionOf(true, i);
                away[i] = MotionOf(false, i);
            }
            _match.CapturePositions(_match.Minute, home, away, _match.HomeOnField.ToArray(), _match.AwayOnField.ToArray());
            while (_nextPositionSampleMinute <= _match.Minute)
                _nextPositionSampleMinute += 0.5f;
        }

        bool PossessionControlled()
        {
            return ActionKind == "touch" || ActionKind == "solo" || ActionKind == "kickoff"
                || ActionKind == "corner" || ActionKind == "freekick" || ActionKind == "throwin"
                || ActionKind == "penalty" || ActionKind == "goalkick";
        }

        public Vector2 PlayerSpot(bool home, int index)
        {
            if (ActionKind == "kickoff" && home == _match.HomeHasBall && index == _match.BallHolder)
                return new Vector2(0.5f, home ? 0.46f : 0.54f);
            if (_pieceLive && home == _pieceHome && index == _pieceTaker)
            {
                var taker = Person(home, index);
                if (_pieceKind == "freekick" && taker != null && taker.HasTrait(PlayerTrait.LivrePotente))
                    return new Vector2(_pieceSpot.x, Mathf.Clamp(_pieceSpot.y + (home ? -0.055f : 0.055f), 0.05f, 0.95f));
                return _pieceSpot;
            }
            if (_pieceLive && home != _pieceHome)
            {
                for (var w = 0; w < _wall.Length; w++)
                {
                    if (_wall[w] != index)
                        continue;
                    var dir = _pieceHome ? 1f : -1f;
                    var x = Mathf.Clamp(_pieceSpot.x + (w - (_wall.Length - 1) * 0.5f) * 0.04f, 0.2f, 0.8f);
                    var y = Mathf.Clamp(_pieceSpot.y + dir * 0.07f, 0.08f, 0.92f);
                    return new Vector2(x, y);
                }
            }

            if (_pieceLive && _pieceKind == "corner")
                return CornerMark(home, index);
            if (_pieceLive && _pieceKind == "throwin" && index > 0)
                return ThrowMark(home, index);

            if (index == 0)
                return Goalkeeper(home);

            var formation = home ? _match.HomeTactics.Formation : _match.AwayTactics.Formation;
            var raw = RawSlot(index, formation);
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var attacking = home == _match.HomeHasBall;
            Player actor = null;
            if (index >= 0 && index < squad.Count)
                _match.People.TryGetValue(squad[index], out actor);
            if (actor != null)
            {
                raw += IndividualOrders.Offset(actor.Duty, attacking, raw);
                if (actor.Fatigue > 75)
                    raw.y -= 0.03f;
            }

            var shape = home ? _match.HomeTactics : _match.AwayTactics;
            PackBlock(ref raw, attacking, shape);
            var ballOwnX = home ? Ball.x : 1f - Ball.x;
            var ballOwnY = home ? Ball.y : 1f - Ball.y;
            var wing = raw.x < 0.34f || raw.x > 0.66f;
            var striker = raw.y >= 0.6f;
            var chasing = (home ? _match.HomeGoals : _match.AwayGoals) <= (home ? _match.AwayGoals : _match.HomeGoals);
            var push = chasing ? 0.05f : 0.02f;
            if (attacking && shape != null && shape.Width != null && shape.Width.Contains("estreita"))
                raw.x = Mathf.Lerp(raw.x, 0.5f, 0.12f);
            if (!attacking && raw.y < 0.58f)
            {
                var lifted = shape != null && shape.DefensiveLine != null && shape.DefensiveLine.Contains("alta");
                var band = Mathf.Clamp(ballOwnY - 0.14f, raw.y - 0.05f, raw.y + (lifted ? 0.06f : 0.03f));
                raw.y = Mathf.Lerp(raw.y, band, 0.4f);
            }
            else if (attacking && index != _match.BallHolder && ballOwnY > 0.3f)
            {
                if (CrossPlan(shape) && wing)
                {
                    raw.x = Mathf.Lerp(raw.x, raw.x < 0.5f ? 0.14f : 0.86f, 0.35f);
                    raw.y = Mathf.Min(raw.y + 0.08f + push, 0.82f);
                }
                else if (DirectPlan(shape) && striker)
                    raw.y = Mathf.Min(raw.y + 0.12f + push, 0.86f);
                else if (TikiPlan(shape) && !striker)
                {
                    raw = Vector2.Lerp(raw, new Vector2(Mathf.Lerp(raw.x, ballOwnX, 0.35f), raw.y), 0.2f);
                    if (index % 2 == 0)
                        raw.y = Mathf.Min(raw.y + 0.06f + push, 0.74f);
                }
                else if (striker)
                    raw.y = Mathf.Min(raw.y + 0.09f + push, 0.84f);
                else if (raw.y > 0.36f && index % 2 == 0)
                    raw.y = Mathf.Min(raw.y + 0.07f + push, 0.72f);
                if (CrossPlan(shape) && !wing && raw.y > 0.48f)
                {
                    raw.y = Mathf.Min(raw.y + 0.08f, 0.86f);
                    raw.x = Mathf.Lerp(raw.x, 0.5f, 0.18f);
                }

                if (attacking && shape != null && shape.FullBacks != null && shape.FullBacks.Contains("sobem") && wing && raw.y < 0.48f)
                    raw.y = Mathf.Min(raw.y + 0.16f, 0.62f);
                if (DirectPlan(shape) && (striker || wing) && index != _match.BallHolder)
                    raw.y = Mathf.Min(raw.y + 0.08f + (actor != null && actor.Attributes != null ? PlayerAttributes.CurrentValue(actor, "Velocidade") / 20f * 0.06f : 0f), 0.9f);

                if (chasing && wing && raw.y < 0.48f)
                    raw.y = Mathf.Min(raw.y + 0.04f, 0.52f);
                var activeRunner = home ? _homeRunner : _awayRunner;
                if (index == activeRunner && ballOwnY > 0.3f)
                    raw.y = Mathf.Min(raw.y + 0.045f, 0.9f);
                if (actor != null && actor.HasTrait(PlayerTrait.Rapido) && (striker || wing))
                    raw.y = Mathf.Min(raw.y + 0.04f, 0.86f);
                if (actor != null && actor.HasTrait(PlayerTrait.Cruzador) && wing)
                    raw.x = Mathf.Lerp(raw.x, raw.x < 0.5f ? 0.12f : 0.88f, 0.25f);
            }

            if (!attacking && index > 0 && index <= 5 && ballOwnY < 0.22f)
            {
                var keeperBuffer = 0.16f + Mathf.Clamp01((0.22f - ballOwnY) / 0.22f) * 0.05f;
                raw.y = Mathf.Max(raw.y, keeperBuffer);
            }

            var spot = home ? raw : new Vector2(1f - raw.x, 1f - raw.y);
            if (attacking && index > 0 && index != _match.BallHolder)
                PeelAway(home, ref spot, index);
            if (index > 0 && _motionReady)
            {
                var carrierHome = _match.HomeHasBall;
                var carrier = MotionOf(carrierHome, Mathf.Clamp(_match.BallHolder, 0, 10));
                if (!attacking)
                {
                    var press = PressAmount(shape);
                    var rank = ClosestRank(home, index, carrier);
                    var gap = Vector2.Distance(spot, carrier);
                    var marker = actor != null && actor.HasTrait(PlayerTrait.Marcador);
                    var pressers = press > 0.3f ? 3 : press > 0.16f ? 2 : 1;
                    var marking = shape == null ? "" : shape.Marking ?? "";
                    var followedOpponent = false;
                    if (marking.IndexOf("homem", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var opponents = home ? _match.AwayOnField : _match.HomeOnField;
                        var nearestOpponent = -1;
                        var nearestDistance = float.MaxValue;
                        var nearestSpot = spot;
                        for (var opponentIndex = 1; opponentIndex < opponents.Count && opponentIndex < 11; opponentIndex++)
                        {
                            if (IsPlayerInjured(opponents[opponentIndex]))
                                continue;
                            var opponentSpot = MotionOf(!home, opponentIndex);
                            var opponentDistance = Vector2.Distance(spot, opponentSpot);
                            if (opponentDistance >= nearestDistance)
                                continue;
                            nearestDistance = opponentDistance;
                            nearestOpponent = opponentIndex;
                            nearestSpot = opponentSpot;
                        }
                        if (nearestOpponent >= 0 && nearestDistance < 0.34f)
                        {
                            spot = Vector2.MoveTowards(spot, nearestSpot, (marker ? 0.075f : 0.04f) + press * 0.05f);
                            followedOpponent = true;
                        }
                    }
                    if (DirectPlan(shape) && shape.Pressing != null && shape.Pressing.Contains("baixa"))
                    {
                        spot.x = Mathf.Lerp(spot.x, 0.5f, 0.16f);
                        if (raw.y < 0.55f)
                            spot.y = home ? Mathf.Min(spot.y, 0.42f) : Mathf.Max(spot.y, 0.58f);
                    }
                    else if (!followedOpponent && rank < pressers && gap < 0.46f && gap > 0.06f)
                        spot = Vector2.MoveTowards(spot, carrier, (marker ? 0.1f : 0.045f) + press * 0.09f);
                }
            }

            if (attacking && index > 0 && index != _match.BallHolder)
                KeepOnside(ref spot, home);
            spot.x = Mathf.Clamp(spot.x, 0.05f, 0.95f);
            spot.y = Mathf.Clamp(spot.y, 0.04f, 0.96f);
            return spot;
        }

        void PeelAway(bool home, ref Vector2 spot, int index)
        {
            var oppCount = home ? _match.AwayOnField.Count : _match.HomeOnField.Count;
            var best = 99f;
            var marker = spot;
            for (var i = 1; i < oppCount && i < 11; i++)
            {
                var mark = LiveSpot(!home, i);
                var distance = Vector2.Distance(spot, mark);
                if (distance < best)
                {
                    best = distance;
                    marker = mark;
                }
            }

            if (best > 0.22f)
                return;
            var away = spot - marker;
            if (away.sqrMagnitude < 0.0004f)
                away = new Vector2(index % 2 == 0 ? -0.8f : 0.8f, home ? 0.35f : -0.35f);
            away.Normalize();
            spot += away * (0.22f - best) * 1.45f;
            spot.y += home ? 0.035f : -0.035f;
        }

        float MarkerGap(bool home, int index)
        {
            var spot = LiveSpot(home, Mathf.Clamp(index, 0, 10));
            var oppCount = home ? _match.AwayOnField.Count : _match.HomeOnField.Count;
            var best = 99f;
            for (var i = 1; i < oppCount && i < 11; i++)
            {
                var distance = Vector2.Distance(spot, LiveSpot(!home, i));
                if (distance < best)
                    best = distance;
            }

            return best > 90f ? 0.3f : best;
        }

        Vector2 LiveSpot(bool home, int index)
        {
            return _motionReady ? MotionOf(home, index) : FormationSpot(home, index);
        }

        int ClosestRank(bool home, int index, Vector2 point)
        {
            var mine = Vector2.Distance(FormationSpot(home, index), point);
            var closer = 0;
            var count = home ? _match.HomeOnField.Count : _match.AwayOnField.Count;
            for (var i = 1; i < count && i < 11; i++)
            {
                if (i == index)
                    continue;
                if (Vector2.Distance(FormationSpot(home, i), point) < mine)
                    closer++;
            }

            return closer;
        }

        void KeepOnside(ref Vector2 spot, bool home)
        {
            var line = OffsideLine(home);
            var ballDepth = home ? Ball.y : 1f - Ball.y;
            var safeDepth = Mathf.Min(line, ballDepth) - 0.035f;
            if (home)
            {
                if (spot.y > 0.5f)
                    spot.y = Mathf.Min(spot.y, safeDepth);
            }
            else if (spot.y < 0.5f)
                spot.y = Mathf.Max(spot.y, 1f - safeDepth);
        }

        float OffsideLine(bool attackingHome)
        {
            var defenders = attackingHome ? _match.AwayOnField : _match.HomeOnField;
            var deepest = float.NegativeInfinity;
            var secondDeepest = float.NegativeInfinity;
            var count = 0;
            for (var i = 0; i < defenders.Count && i < 11; i++)
            {
                var point = MotionOf(!attackingHome, i);
                var depth = attackingHome ? point.y : 1f - point.y;
                count++;
                if (depth >= deepest)
                {
                    secondDeepest = deepest;
                    deepest = depth;
                }
                else if (depth > secondDeepest)
                    secondDeepest = depth;
            }

            if (count < 2)
                return BackLine(!attackingHome);
            return secondDeepest;
        }

        float BackLine(bool defendingHome)
        {
            var tactics = defendingHome ? _match.HomeTactics : _match.AwayTactics;
            var formation = tactics != null && !string.IsNullOrEmpty(tactics.Formation) ? tactics.Formation : "4-4-2";
            var text = tactics != null ? tactics.DefensiveLine : "";
            var high = text != null && text.Contains("alta");
            var very = high && text.Contains("muito");
            var low = text != null && text.Contains("baixa");
            var found = false;
            var edge = defendingHome ? 0f : 1f;
            for (var i = 1; i <= 5; i++)
            {
                var raw = RawSlot(i, formation);
                if (raw.y > 0.42f)
                    continue;
                if (very)
                    raw.y = 0.56f;
                else if (high)
                    raw.y = Mathf.Max(raw.y, 0.44f);
                else if (low && text.Contains("muito"))
                    raw.y = 0.14f;
                else if (low)
                    raw.y = Mathf.Min(raw.y, 0.2f);
                var world = defendingHome ? raw.y : 1f - raw.y;
                if (!found)
                {
                    edge = world;
                    found = true;
                }
                else
                    edge = defendingHome ? Mathf.Max(edge, world) : Mathf.Min(edge, world);
            }

            return found ? edge : defendingHome ? 0.28f : 0.72f;
        }

        static void PackBlock(ref Vector2 raw, bool attacking, TacticalIdentity mine)
        {
            var text = mine != null ? mine.DefensiveLine : "";
            var very = text != null && text.Contains("muito") && text.Contains("alta");
            var veryLow = text != null && text.Contains("muito") && text.Contains("baixa");
            var high = text != null && text.Contains("alta");
            var low = text != null && text.Contains("baixa");
            if (raw.y < 0.46f)
            {
                if (very)
                    raw.y = Mathf.Lerp(raw.y, attacking ? 0.6f : 0.54f, 0.9f);
                else if (high)
                    raw.y = Mathf.Lerp(raw.y, attacking ? 0.5f : 0.44f, 0.88f);
                else if (veryLow && !attacking)
                    raw.y = Mathf.Lerp(raw.y, 0.14f, 0.7f);
                else if (low && !attacking)
                    raw.y = Mathf.Lerp(raw.y, 0.2f, 0.6f);
            }

            if (high)
                raw.x = Mathf.Lerp(raw.x, 0.5f, 0.08f);
        }

        public Vector2 MotionOf(bool home, int index)
        {
            index = Mathf.Clamp(index, 0, 10);
            if (!_motionReady)
                return PlayerSpot(home, index);
            return home ? _homeMotion[index] : _awayMotion[index];
        }

        public Vector2 ShownMotionOf(bool home, int index)
        {
            index = Mathf.Clamp(index, 0, 10);
            if (!_motionReady || _match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime)
                return MotionOf(home, index);

            var current = home ? _homeMotion[index] : _awayMotion[index];
            var previous = home ? _homePreviousMotion[index] : _awayPreviousMotion[index];
            return Vector2.Lerp(previous, current, Mathf.Clamp01(_stepAccumulator / FixedStep));
        }

        public Vector2 ShownBall()
        {
            return _drawnReady ? _drawn : Ball;
        }

        void PresentBall(float dt)
        {
            var logical = Ball;
            if (Mathf.Abs(_curl) > 0.05f && (ActionKind == "shot" || ActionKind == "goal" || ActionKind == "pass"))
            {
                var path = BallTarget - _flightFrom;
                var len = path.sqrMagnitude;
                if (len > 0.002f)
                {
                    var along = Mathf.Clamp01(Vector2.Dot(Ball - _flightFrom, path) / len);
                    var bend = new Vector2(-path.y, path.x).normalized;
                    logical += bend * Mathf.Sin(along * Mathf.PI) * _curl * 0.09f;
                }
            }

            if (!_drawnReady)
            {
                _drawn = logical;
                _drawnReady = true;
                return;
            }

            var hop = Mathf.Min(dt * Mathf.Max(Travel, 0.15f) + 0.006f, 0.09f);
            _drawn = Vector2.MoveTowards(_drawn, logical, hop);
        }

        public float KeeperDive(bool home)
        {
            if (ActionKind != "shot" && ActionKind != "goal")
                return 0f;
            var atHomeGoal = BallTarget.y < 0.2f;
            if (atHomeGoal != home)
                return 0f;
            return Mathf.Clamp((BallTarget.x - 0.5f) * 12f, -1f, 1f);
        }

        Vector2 Feet(bool home, int index)
        {
            var spot = MotionOf(home, index);
            var aim = _playAim;
            if (aim.sqrMagnitude < 0.0001f)
                aim = home ? new Vector2(spot.x, 1f) : new Vector2(spot.x, 0f);
            var dir = aim - spot;
            if (dir.sqrMagnitude < 0.0004f)
                dir = home ? Vector2.up : Vector2.down;
            dir.Normalize();
            var placed = spot + dir * Mathf.Lerp(0.022f, 0.008f, ControlOf(home, index));
            return new Vector2(Mathf.Clamp(placed.x, 0.04f, 0.96f), Mathf.Clamp(placed.y, 0.04f, 0.96f));
        }

        Vector2 ReceivePoint(bool home, int index)
        {
            var now = MotionOf(home, index);
            var lead = Mathf.Lerp(0.012f, 0.004f, ControlOf(home, index));
            var y = home ? now.y + lead : now.y - lead;
            return new Vector2(Mathf.Clamp(now.x, 0.06f, 0.94f), Mathf.Clamp(y, 0.05f, 0.95f));
        }

        float ControlOf(bool home, int index)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (index < 0 || index >= squad.Count || !_match.People.TryGetValue(squad[index], out var player) || player.Attributes == null)
                return 0.45f;
            var skill = (PlayerAttributes.CurrentValue(player, "Drible") + PlayerAttributes.CurrentValue(player, "Frieza") + PlayerAttributes.CurrentValue(player, "Decisão")) / 3f;
            if (player.HasTrait(PlayerTrait.Driblador))
                skill += 2f;
            if (player.HasTrait(PlayerTrait.Frieza))
                skill += 1f;
            if (player.Fatigue > 80)
                skill -= 3f;
            return Mathf.Clamp01((skill - 7f) / 10f);
        }

        void Glide(float dt)
        {
            var step = Mathf.Max(0f, dt);
            if (!_motionReady)
            {
                for (var i = 0; i < 11; i++)
                {
                    _homeMotion[i] = PlayerSpot(true, i);
                    _awayMotion[i] = PlayerSpot(false, i);
                    _homePreviousMotion[i] = _homeMotion[i];
                    _awayPreviousMotion[i] = _awayMotion[i];
                }

                _motionReady = true;
            }

            _homeRunner = _match.HomeHasBall ? FindRunner(true, _match.BallHolder) : -1;
            _awayRunner = !_match.HomeHasBall ? FindRunner(false, _match.BallHolder) : -1;

            GlideSide(true, _homeMotion, step);
            GlideSide(false, _awayMotion, step);
        }

        void GlideSide(bool home, Vector2[] motion, float dt)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var previousMotion = home ? _homePreviousMotion : _awayPreviousMotion;
            for (var i = 0; i < motion.Length; i++)
            {
                previousMotion[i] = motion[i];
                if (i < squad.Count && IsPlayerInjured(squad[i]))
                    continue;
                var goal = i < squad.Count ? PlayerSpot(home, i) : motion[i];
                var carrying = home == _match.HomeHasBall && i == _match.BallHolder;
                var chasingLooseBall = i > 0 && IsLooseBallChaser(home, i);
                if (carrying && (ActionKind == "pass" || ActionKind == "cross"))
                    goal = BallTarget;
                else if (carrying && ActionKind == "solo")
                    goal = Vector2.Lerp(goal, Ball, 0.72f);
                else if (carrying && (ActionKind == "touch" || _settle > 0f))
                    goal = DribblePoint(home, i, goal);
                if (chasingLooseBall)
                    goal = Ball;
                var pace = 11;
                var quick = false;
                var tired = false;
                if (i < squad.Count && _match.People.TryGetValue(squad[i], out var player))
                {
                    if (player.Attributes != null)
                        pace = PlayerAttributes.CurrentValue(player, "Velocidade");
                    quick = player.HasTrait(PlayerTrait.Rapido);
                    tired = player.Fatigue > 78;
                    if (player.HasTrait(PlayerTrait.Trabalhador))
                        pace += 1;
                }

                var speed = 0.15f + Mathf.Max(0, pace - 11) * 0.004f;
                if (quick)
                    speed += 0.02f;
                if (carrying)
                    speed += 0.01f;
                if (tired)
                    speed *= 0.8f;
                if (chasingLooseBall)
                    speed = Mathf.Max(speed, 0.24f);
                if (_pieceLive && home == _pieceHome && i == _pieceTaker)
                    speed = Mathf.Max(speed, 0.24f);
                if (i == 0)
                    speed *= 0.18f;
                var previous = motion[i];
                if (!IsFinite(goal) || goal.x < 0.01f || goal.x > 0.99f || goal.y < 0.01f || goal.y > 0.99f)
                {
                    Diagnose("jogador " + (i < squad.Count ? squad[i] : i.ToString()), "alvo de movimento inválido", previous, goal, dt);
                    goal = IsFinite(previous) ? previous : home ? new Vector2(0.5f, 0.06f) : new Vector2(0.5f, 0.94f);
                }
                var next = Vector2.MoveTowards(previous, goal, speed * dt);
                if (!IsFinite(next) || Vector2.Distance(previous, next) > speed * dt + 0.001f)
                {
                    Diagnose("jogador " + (i < squad.Count ? squad[i] : i.ToString()), "deslocamento inválido", previous, next, dt);
                    next = IsFinite(previous) ? previous : goal;
                }
                motion[i] = next;
                motion[i].x = Mathf.Clamp(motion[i].x, 0.03f, 0.97f);
                motion[i].y = Mathf.Clamp(motion[i].y, 0.03f, 0.97f);
            }
        }

        Vector2 DribblePoint(bool home, int index, Vector2 spot)
        {
            var weave = 0.016f;
            var push = 0.01f;
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (index >= 0 && index < squad.Count && _match.People.TryGetValue(squad[index], out var player))
            {
                if (player.HasTrait(PlayerTrait.Driblador) || (player.Attributes != null && PlayerAttributes.CurrentValue(player, "Drible") >= 15))
                    weave = 0.034f;
                if (player.HasTrait(PlayerTrait.Rapido) || (player.Attributes != null && PlayerAttributes.CurrentValue(player, "Velocidade") >= 16))
                    push = 0.02f;
                if (player.HasTrait(PlayerTrait.Cruzador))
                    weave += spot.x < 0.5f ? -0.01f : 0.01f;
            }

            var sway = Mathf.Sin(_match.Minute * 5.5f + index) * weave;
            var forward = home ? push : -push;
            return new Vector2(Mathf.Clamp(spot.x + sway, 0.08f, 0.92f), Mathf.Clamp(spot.y + forward, 0.08f, 0.9f));
        }

        bool IsLooseBallChaser(bool home, int index)
        {
            if (ActionKind != "loose" && ActionKind != "turnover")
                return false;
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (index < 1 || index >= squad.Count || IsPlayerInjured(squad[index]))
                return false;
            var nearest = -1;
            var second = -1;
            var nearestDistance = float.MaxValue;
            var secondDistance = float.MaxValue;
            for (var i = 1; i < squad.Count && i < 11; i++)
            {
                if (IsPlayerInjured(squad[i]))
                    continue;
                var point = _motionReady ? MotionOf(home, i) : FormationSpot(home, i);
                var distance = Vector2.Distance(point, Ball);
                if (distance < nearestDistance)
                {
                    second = nearest;
                    secondDistance = nearestDistance;
                    nearest = i;
                    nearestDistance = distance;
                }
                else if (distance < secondDistance)
                {
                    second = i;
                    secondDistance = distance;
                }
            }

            return index == nearest || (index == second && secondDistance < nearestDistance + 0.12f);
        }

        Vector2 Goalkeeper(bool home)
        {
            var ownY = home ? 0.06f : 0.94f;
            var shotAtGoal = (ActionKind == "shot" || ActionKind == "goal")
                && (home ? BallTarget.y < 0.2f : BallTarget.y > 0.8f);
            var watchX = ActionKind == "cross" || shotAtGoal ? BallTarget.x : Ball.x;
            var tracking = shotAtGoal ? 0.62f : ActionKind == "cross" ? 0.24f : 0.18f;
            var watch = Mathf.Lerp(0.5f, Mathf.Clamp(watchX, 0.3f, 0.7f), tracking);
            var lineX = Mathf.Clamp(watch, 0.42f, 0.58f);
            var y = ownY;
            var crossInsideBox = ActionKind == "cross" && BallTarget.x > 0.18f && BallTarget.x < 0.82f
                && (home ? BallTarget.y > 0.02f && BallTarget.y < 0.18f : BallTarget.y > 0.82f && BallTarget.y < 0.98f);
            if (crossInsideBox)
            {
                var keeper = Person(home, 0);
                var duty = keeper == null ? "" : keeper.Duty ?? "";
                var stepOut = duty == "Ficar na linha" ? 0f
                    : duty == "Sair nos cruzamentos" ? 0.065f
                    : keeper != null && keeper.HasTrait(PlayerTrait.GuardaRedesSaiDaBaliza) ? 0.065f
                    : keeper != null && keeper.HasTrait(PlayerTrait.GuardaRedesSaiAosCruzamentos) ? 0.05f : 0.025f;
                y = home ? Mathf.Min(BallTarget.y, ownY + stepOut) : Mathf.Max(BallTarget.y, ownY - stepOut);
            }
            var goingOut = BallTarget.x < 0.02f || BallTarget.x > 0.98f || (home ? BallTarget.y < 0.01f : BallTarget.y > 0.99f);
            var inBox = home
                ? Ball.y > 0.02f && Ball.y < 0.18f && Ball.x > 0.18f && Ball.x < 0.82f
                : Ball.y < 0.98f && Ball.y > 0.82f && Ball.x > 0.18f && Ball.x < 0.82f;
            var loose = ActionKind == "loose" || ActionKind == "turnover";
            var claim = inBox && loose && !goingOut && !RivalNear(home, Ball, 0.12f) && Vector2.Distance(new Vector2(lineX, ownY), Ball) < 0.1f;
            if (claim)
                y = home ? Mathf.Clamp(Ball.y, 0.05f, 0.12f) : Mathf.Clamp(Ball.y, 0.88f, 0.95f);
            if (_match.HomeHasBall == home && _match.BallHolder == 0)
                y = home ? 0.08f : 0.92f;
            return new Vector2(lineX, y);
        }

        bool RivalNear(bool homeKeeper, Vector2 point, float reach)
        {
            var oppHome = !homeKeeper;
            var count = oppHome ? _match.HomeOnField.Count : _match.AwayOnField.Count;
            for (var i = 1; i < count && i < 11; i++)
            {
                if (Vector2.Distance(MotionOf(oppHome, i), point) < reach)
                    return true;
            }

            return false;
        }

        static Vector2 ClampPitch(Vector2 spot)
        {
            return new Vector2(Mathf.Clamp(spot.x, 0.06f, 0.94f), Mathf.Clamp(spot.y, 0.03f, 0.97f));
        }

        void Sweep()
        {
            if (ActionKind == "shot" || ActionKind == "goal" || DeadBall || _pieceLive)
                return;
            for (var side = 0; side < 2; side++)
            {
                var home = side == 0;
                var spot = MotionOf(home, 0);
                if (Vector2.Distance(spot, Ball) > 0.055f)
                    continue;
                var inBox = home ? Ball.y < 0.26f && Ball.y > 0.01f : Ball.y > 0.74f && Ball.y < 0.99f;
                if (!inBox || RivalNear(home, Ball, 0.12f))
                    continue;
                if (_match.HomeHasBall == home && _match.BallHolder == 0 && _settle > 0f)
                    continue;
                var loose = ActionKind == "loose" || ActionKind == "turnover" || ActionKind == "offside";
                if (!loose)
                    continue;
                _match.HomeHasBall = home;
                _match.BallHolder = 0;
                _intended = -1;
                _playAim = home ? new Vector2(0.5f, 0.42f) : new Vector2(0.5f, 0.58f);
                BallTarget = Feet(home, 0);
                Travel = 0.4f;
                _settle = 0.35f;
                ActionKind = "touch";
                return;
            }
        }

        public void StartSecondHalf()
        {
            _match.Phase = MatchPhase.SecondHalf;
            if (_match.Minute < 45f)
                _match.Minute = 45f;
            Kickoff(false);
            _match.AddNote("Segunda parte. O jogo recomeça no meio-campo.", "half");
        }

        void Kickoff(bool home)
        {
            ResolvePendingPass(false, false, null);
            _pendingGoal = false;
            _goalCelebration = false;
            _goalCelebrationRemaining = 0f;
            _pendingScorerId = null;
            _pendingAssistId = null;
            _pendingGoalDescription = null;
            _pieceLive = false;
            _pieceKind = "";
            _curl = 0f;
            _finishNext = false;
            _throughBall = false;
            _headerNext = false;
            _lastPasser = null;
            _lastPasserHome = false;
            _settle = 0f;
            _intended = -1;
            BallHeight = 0f;
            _flightApex = 0f;
            _match.HomeHasBall = home;
            _match.BallHolder = CentreTaker(home);
            Ball = new Vector2(0.5f, 0.5f);
            BallTarget = Ball;
            SnapBallVisual();
            Travel = 0.15f;
            Hold = 1.15f;
            ActionTime = 0.8f;
            ActionKind = "kickoff";
        }

        int CentreTaker(bool home)
        {
            var tactics = home ? _match.HomeTactics : _match.AwayTactics;
            if (tactics == null)
                return 8;
            var best = 8;
            var bestDist = 99f;
            for (var i = 1; i < 11; i++)
            {
                var spot = FormationSpot(home, i);
                var dist = Mathf.Abs(spot.y - 0.5f) + Mathf.Abs(spot.x - 0.5f) * 0.35f;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }

            return best;
        }

        public bool TrySub(bool home, int fieldIndex, int benchIndex)
        {
            var field = home ? _match.HomeOnField : _match.AwayOnField;
            var bench = home ? _match.HomeBench : _match.AwayBench;
            var left = home ? _match.HomeSubsLeft : _match.AwaySubsLeft;
            if (left <= 0 || fieldIndex < 0 || benchIndex < 0 || fieldIndex >= field.Count || benchIndex >= bench.Count)
                return false;
            var outId = field[fieldIndex];
            if (_match.SentOffPlayers.Contains(outId))
                return false;
            if (_match.People.TryGetValue(bench[benchIndex], out var incoming) && incoming != null
                && incoming.Injury != null && incoming.Injury.IsInjured)
                return false;
            var captainId = home ? _match.HomeCaptainId : _match.AwayCaptainId;
            _match.Appeared.Add(bench[benchIndex]);
            field[fieldIndex] = bench[benchIndex];
            bench[benchIndex] = outId;
            if (captainId == outId)
                _match.ReplaceCaptain(home);
            if (home) _match.HomeSubsLeft--;
            else _match.AwaySubsLeft--;
            _match.AddNote($"Substituição: sai {Name(outId)}, entra {Name(field[fieldIndex])}.", "sub");
            return true;
        }

        public bool ApplyTeamTalk(string talk, bool home, System.Collections.Generic.Dictionary<string, int> managerTrust = null, string detail = null)
        {
            if (_match == null || _match.TeamTalkApplied || _match.Phase != MatchPhase.HalfTime)
                return false;

            _match.TeamTalkApplied = true;
            _match.TeamTalk = talk ?? "calm";
            var field = home ? _match.HomeOnField : _match.AwayOnField;
            var ours = home ? _match.HomeGoals : _match.AwayGoals;
            var theirs = home ? _match.AwayGoals : _match.HomeGoals;
            for (var i = 0; i < field.Count; i++)
            {
                if (!_match.People.TryGetValue(field[i], out var player) || player == null)
                    continue;
                var rating = _match.RatingOf(player.Id);
                var moraleChange = 0;
                var trust = managerTrust != null && managerTrust.TryGetValue(player.Id, out var knownTrust) ? knownTrust : 50;
                var sensitive = player.Personality == PersonalityTrait.Insecure || player.Personality == PersonalityTrait.Temperamental;
                var composed = player.Personality == PersonalityTrait.Professional || player.Personality == PersonalityTrait.Leader;
                switch (_match.TeamTalk.ToLowerInvariant())
                {
                    case "motivational":
                    case "motivar":
                        moraleChange = (player.Morale < 72 ? 1 : 0) + (rating < 6.2f && (composed || player.Morale < 45) ? 1 : 0);
                        if (trust < 30 && sensitive) moraleChange--;
                        break;
                    case "demanding":
                    case "exigir":
                        moraleChange = rating < 6.2f ? composed ? 1 : sensitive ? -1 : 0 : 0;
                        if (ours < theirs && player.Morale < 45 && !sensitive) moraleChange++;
                        break;
                    case "angry":
                    case "repreender":
                        moraleChange = rating < 6.1f ? composed ? 1 : sensitive ? -2 : -1 : sensitive ? -1 : 0;
                        if (trust < 40 && moraleChange > 0) moraleChange--;
                        break;
                    case "tactical":
                    case "tática":
                    case "tactica":
                        moraleChange = trust >= 75 && rating < 6.3f ? 1 : 0;
                        break;
                    default:
                        moraleChange = sensitive && player.Morale < 65 ? 2 : 0;
                        if (rating >= 7.4f && player.Morale >= 75) moraleChange--;
                        break;
                }

                if (player.Fatigue > 82 && (_match.TeamTalk == "demanding" || _match.TeamTalk == "exigir"))
                    moraleChange = Mathf.Min(moraleChange, 0);
                player.Morale = Mathf.Clamp(player.Morale + moraleChange, 1, 99);
            }

            _match.LastEvent = string.IsNullOrEmpty(detail) ? TeamTalkLine(_match.TeamTalk, ours, theirs) : detail;
            return true;
        }

        static string TeamTalkLine(string talk, int ours, int theirs)
        {
            switch ((talk ?? "").ToLowerInvariant())
            {
                case "motivational": return "Dás confiança ao grupo e pedes que mantenha a coragem.";
                case "demanding": return "Exiges mais concentração e intensidade na segunda parte.";
                case "angry": return "Confrontas o que correu mal, sabendo que alguns podem reagir mal.";
                case "tactical":
                case "tática":
                case "tactica": return "Explicas uma correção para a segunda parte com base no que aconteceu.";
                default: return ours < theirs ? "Pedes calma e clareza para recuperar sem precipitação." : "Pedes calma e controlo para gerir o jogo.";
            }
        }

        void NextAction()
        {
            if (_pieceLive)
            {
                StrikePiece();
                return;
            }

            var currentSide = _match.HomeHasBall;
            var currentSquad = currentSide ? _match.HomeOnField : _match.AwayOnField;
            var holder = _match.BallHolder;
            if (holder >= 0 && holder < currentSquad.Count && IsPlayerInjured(currentSquad[holder]))
            {
                var recipient = -1;
                var nearest = float.MaxValue;
                for (var i = 0; i < currentSquad.Count && i < 11; i++)
                {
                    if (i == holder || IsPlayerInjured(currentSquad[i]))
                        continue;
                    var distance = Vector2.Distance(Ball, MotionOf(currentSide, i));
                    if (distance < nearest)
                    {
                        nearest = distance;
                        recipient = i;
                    }
                }
                if (recipient >= 0)
                {
                    _match.BallHolder = recipient;
                    BallTarget = ReceivePoint(currentSide, recipient);
                    ActionKind = "pass";
                    Travel = 0.12f;
                    Hold = 0f;
                    Launch(0f);
                    _match.LastEvent = Name(currentSquad[holder]) + " não consegue continuar; a equipa tenta sair com a bola.";
                    return;
                }
                var opponents = currentSide ? _match.AwayOnField : _match.HomeOnField;
                var defender = -1;
                nearest = float.MaxValue;
                for (var i = 0; i < opponents.Count && i < 11; i++)
                {
                    if (IsPlayerInjured(opponents[i])) continue;
                    var distance = Vector2.Distance(Ball, MotionOf(!currentSide, i));
                    if (distance < nearest) { nearest = distance; defender = i; }
                }
                if (defender >= 0)
                    Turnover(currentSide, defender);
                return;
            }

            if (BallLeftPlay(_match.HomeHasBall))
                return;

            if (_finishNext)
            {
                var side = _finishHome;
                _finishNext = false;
                if (_match.HomeHasBall == side)
                {
                    Shoot(side);
                    return;
                }
            }

            var attack = _match.HomeHasBall;
            var tactics = attack ? _match.HomeTactics : _match.AwayTactics;
            var depth = attack ? Ball.y : 1f - Ball.y;
            var carrier = Person(attack, _match.BallHolder);
            var choice = Decide(attack, carrier, tactics, depth);
            if (choice == "cross" && TryCross(attack))
                return;
            if (choice == "dribble" && TrySolo(attack, carrier, depth))
                return;
            if (choice == "carry" && (TryCarryLine(attack) || TryProgress(attack)))
                return;
            if (choice == "shoot")
            {
                Shoot(attack);
                return;
            }

            if (BallLeftPlay(attack))
                return;
            var room = MarkerGap(attack, _match.BallHolder);
            var pressed = room < 0.055f || choice == "dribble";
            if (pressed && Duel(attack))
                return;
            if (choice == "through")
                _throughBall = true;
            Pass(attack);
        }

        string Decide(bool attack, Player carrier, TacticalIdentity tactics, float depth)
        {
            var attr = PlayerAttributes.Current(carrier);
            var room = MarkerGap(attack, _match.BallHolder);
            var wide = Ball.x < 0.28f || Ball.x > 0.72f;
            var runner = FindRunner(attack, _match.BallHolder);
            var box = BoxSupport(attack, _match.BallHolder);
            var cross = CrossPlan(tactics);
            var tiki = TikiPlan(tactics);
            var direct = DirectPlan(tactics);
            var press = PressAmount(tactics);
            var minute = _match.Minute;
            var ours = attack ? _match.HomeGoals : _match.AwayGoals;
            var theirs = attack ? _match.AwayGoals : _match.HomeGoals;
            var chasing = ours < theirs;
            var leading = ours > theirs;
            var pass = 1.2f;
            var through = runner >= 0 ? 1.05f : -0.4f;
            var dribble = 0.15f;
            var wing = wide ? 0.2f : -0.15f;
            var shoot = -0.8f;
            var carry = 0.35f;
            if (attr != null)
            {
                pass += (attr.Passing - 10) * 0.035f + (attr.Decisions - 10) * 0.04f;
                through += (attr.Passing - 10) * 0.045f + (attr.Decisions - 10) * 0.02f;
                dribble += (attr.Dribbling - 10) * 0.07f;
                wing += (attr.Crossing - 10) * 0.06f;
                carry += (attr.Pace - 10) * 0.03f;
                if (carrier.HasTrait(PlayerTrait.Visionario))
                    through += 0.38f;
                if (carrier.HasTrait(PlayerTrait.Driblador))
                    dribble += 0.32f;
                if (carrier.HasTrait(PlayerTrait.Cruzador))
                    wing += 0.34f;
                if (carrier.HasTrait(PlayerTrait.Rapido))
                    carry += 0.16f;
                if (carrier.HasTrait(PlayerTrait.DribladorTecnico))
                    dribble += 0.22f;
                if (carrier.HasTrait(PlayerTrait.DribladorRapido))
                    carry += 0.12f;
                if (carrier.HasTrait(PlayerTrait.CriadorDeJogo))
                {
                    pass += 0.22f;
                    through += 0.14f;
                }
                if (carrier.Fatigue > 78 || attr.Stamina <= 8 && carrier.Fatigue > 60)
                {
                    dribble -= 0.22f;
                    through -= 0.12f;
                    carry -= 0.1f;
                    pass += 0.18f;
                }

                if (attr.Decisions >= 15 && room < 0.08f)
                {
                    dribble -= 0.28f;
                    pass += 0.22f;
                }
                else if (attr.Decisions <= 8)
                {
                    dribble += 0.12f;
                    shoot += 0.12f;
                }
            }

            var tempo = tactics == null ? "" : tactics.Tempo ?? "";
            if (tempo.IndexOf("rápido", StringComparison.OrdinalIgnoreCase) >= 0
                || tempo.IndexOf("fast", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pass += 0.05f;
                through += 0.16f;
                carry += 0.11f;
            }
            else if (tempo.IndexOf("lento", StringComparison.OrdinalIgnoreCase) >= 0
                || tempo.IndexOf("slow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pass += 0.14f;
                through -= 0.08f;
                carry -= 0.1f;
                dribble -= 0.06f;
            }

            if (depth > 0.84f && Mathf.Abs(Ball.x - 0.5f) < 0.2f)
                shoot += 0.95f + (attr == null ? 0f : (attr.Finishing - 10) * 0.05f) + (attr == null ? 0f : (attr.Composure - 10) * 0.02f);
            else if (depth > 0.78f)
                shoot += 0.42f + (attr == null ? 0f : (attr.Finishing - 12) * 0.03f);
            else if (depth > 0.62f && carrier != null && (carrier.HasTrait(PlayerTrait.RemateDeLonge) || carrier.HasTrait(PlayerTrait.RematadorDeLonge)))
                shoot += 0.34f;
            else if (depth > 0.7f && carrier != null && (carrier.HasTrait(PlayerTrait.RemateEmJeito) || carrier.HasTrait(PlayerTrait.Potente) || (tactics != null && tactics.AttackIdea != null && tactics.AttackIdea.IndexOf("Remate", StringComparison.OrdinalIgnoreCase) >= 0)))
                shoot += 0.28f;
            if (room > 0.14f && depth > 0.8f)
                shoot += 0.22f;
            if (room < 0.07f && depth > 0.8f)
                shoot -= 0.18f;
            if (carrier != null && carrier.HasTrait(PlayerTrait.Finalizador) && depth > 0.8f)
                shoot += 0.2f;
            if (carrier != null && attr != null && attr.Finishing <= 8)
                shoot -= 0.35f;
            if (wide && depth > 0.52f)
                wing += 0.48f;
            if (box >= 2 && wide)
                wing += 0.28f;
            if (box == 0)
                wing -= 0.35f;
            if (cross)
            {
                wing += 0.55f;
                carry += 0.18f;
                pass -= 0.08f;
            }
            else if (tiki)
            {
                pass += 0.48f;
                through -= 0.12f;
                wing -= 0.4f;
                dribble -= 0.08f;
                shoot -= 0.12f;
            }
            else if (direct)
            {
                through += 0.42f;
                carry += 0.12f;
                pass -= 0.18f;
            }

            if (press > 0.28f && depth < 0.45f)
                pass += 0.08f;
            if (room > 0.16f && depth < 0.75f)
                carry += 0.22f;
            if (room < 0.08f && attr != null && attr.Dribbling >= 15)
                dribble += 0.2f;
            if (chasing && minute > 70f)
            {
                through += 0.12f;
                shoot += 0.1f;
            }

            if (leading && minute > 78f)
            {
                pass += 0.28f;
                shoot -= 0.08f;
                dribble -= 0.1f;
            }

            if (depth < 0.4f)
            {
                shoot = -2f;
                wing -= 0.2f;
                through += direct ? 0.15f : 0.05f;
            }

            var angle = 1f - Mathf.Abs(Ball.x - 0.5f) * 1.7f;
            if (angle < 0f)
                angle = 0f;
            if (depth >= 0.64f)
            {
                var finish = attr == null ? 10f : attr.Finishing;
                var cool = attr == null ? 10f : attr.Composure;
                var place = attr == null ? 10f : attr.Positioning;
                var close = Mathf.Clamp01((depth - 0.64f) / 0.22f);
                var urge = -0.55f + close * 0.7f + angle * 0.36f;
                urge += (finish - 10f) * 0.045f + (cool - 10f) * 0.02f + (place - 10f) * 0.015f;
                if (room < 0.055f)
                    urge -= cool >= 14f ? 0.12f : 0.28f;
                else if (room < 0.09f)
                    urge -= 0.08f;
                else if (room > 0.16f)
                    urge += 0.16f;
                if (angle < 0.28f)
                    urge -= 0.4f;
                if (finish <= 8f)
                    urge -= 0.35f;
                var distanceShooter = carrier != null && (carrier.HasTrait(PlayerTrait.RemateDeLonge) || carrier.HasTrait(PlayerTrait.Potente));
                if (depth < 0.72f && finish < 14f && (carrier == null || (!carrier.HasTrait(PlayerTrait.RemateEmJeito) && !distanceShooter)))
                    urge -= 0.28f;
                if (carrier != null && (carrier.HasTrait(PlayerTrait.RemateDeLonge) || carrier.HasTrait(PlayerTrait.RematadorDeLonge)) && depth >= 0.62f && depth < 0.8f)
                    urge += 0.22f;
                if (carrier != null && carrier.HasTrait(PlayerTrait.RemateRasteiro) && depth > 0.8f)
                    urge += 0.08f;
                if (carrier != null && carrier.HasTrait(PlayerTrait.Finalizador))
                    urge += 0.16f;
                if (carrier != null && carrier.HasTrait(PlayerTrait.RemateEmJeito))
                    urge += 0.12f;
                if (tiki)
                    urge -= 0.2f;
                if (tactics != null && tactics.AttackIdea != null && tactics.AttackIdea.IndexOf("Remate", StringComparison.OrdinalIgnoreCase) >= 0)
                    urge += 0.22f;
                shoot = pass + urge;
            }

            var noise = (float)_rng.NextDouble() * 0.18f;
            var best = "pass";
            var score = pass;
            if (through + noise * 0.3f > score)
            {
                best = "through";
                score = through;
            }

            if (dribble + noise * 0.4f > score)
            {
                best = "dribble";
                score = dribble;
            }

            if (wing + noise * 0.3f > score)
            {
                best = "cross";
                score = wing;
            }

            if (carry + noise * 0.2f > score)
            {
                best = "carry";
                score = carry;
            }

            if (shoot + noise * 0.15f > score)
                best = "shoot";
            return best;
        }

        int BoxSupport(bool home, int from)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var count = 0;
            for (var i = 1; i < squad.Count && i < 11; i++)
            {
                if (i == from)
                    continue;
                var mark = LiveSpot(home, i);
                var depth = home ? mark.y : 1f - mark.y;
                if (depth > 0.68f && Mathf.Abs(mark.x - 0.5f) < 0.28f)
                    count++;
            }

            return count;
        }

        bool BallLeftPlay(bool homeTouched)
        {
            var wide = Ball.x < 0f || Ball.x > 1f;
            var over = Ball.y < 0f || Ball.y > 1f;
            if (!wide && !over)
                return false;
            ResolvePendingPass(false, false, null);
            if (wide && !over)
            {
                ThrowIn(!homeTouched, Ball.y, Ball.x < 0.5f);
                return true;
            }

            var top = Ball.y > 0.5f;
            var missedOpponentGoal = homeTouched == top;
            if (missedOpponentGoal)
                GoalKick(!homeTouched);
            else
                Corner(!homeTouched);
            return true;
        }

        bool TrySolo(bool home, Player carrier, float depth)
        {
            if (carrier == null || carrier.Position == PlayerPosition.Goalkeeper || carrier.Attributes == null)
                return false;
            if (_match.BallHolder <= 0)
                return false;
            var attr = PlayerAttributes.Current(carrier);
            var appetite = 0.02f + attr.Dribbling / 20f * 0.07f + attr.Pace / 20f * 0.04f + attr.Finishing / 20f * 0.04f;
            var overall = CurrentRoleOverall(carrier);
            if (overall >= 84)
                appetite += 0.16f;
            else if (overall >= 78)
                appetite += 0.1f;
            else if (overall >= 72)
                appetite += 0.04f;
            if (carrier.HasTrait(PlayerTrait.Driblador))
                appetite += 0.09f;
            if (carrier.HasTrait(PlayerTrait.DribladorTecnico))
                appetite += 0.11f;
            if (carrier.HasTrait(PlayerTrait.DribladorRapido))
                appetite += 0.08f;
            if (carrier.HasTrait(PlayerTrait.Flair))
                appetite += 0.035f;
            if (carrier.HasTrait(PlayerTrait.Rapido))
                appetite += 0.05f;
            if (carrier.HasTrait(PlayerTrait.Finalizador))
                appetite += 0.06f;
            if (carrier.HasTrait(PlayerTrait.ImuneAPressao))
                appetite += 0.04f;
            if (carrier.HasTrait(PlayerTrait.RemateEmJeito))
                appetite += 0.03f;
            if (attr.Dribbling < 11 && !carrier.HasTrait(PlayerTrait.Driblador))
                appetite *= 0.22f;
            if (depth < 0.42f)
                return false;
            if (depth < 0.7f && attr.Dribbling < 14 && !carrier.HasTrait(PlayerTrait.Driblador))
                return false;
            if (depth > 0.82f)
                appetite *= 1.15f;
            if (appetite > 0.58f)
                appetite = 0.58f;
            if (_rng.NextDouble() > appetite)
                return false;

            var carry = 0.035f + attr.Pace / 20f * 0.04f;
            if (carrier.HasTrait(PlayerTrait.Rapido))
                carry += 0.02f;
            if (carrier.HasTrait(PlayerTrait.DribladorRapido))
                carry += 0.012f;
            var zigzag = carrier.HasTrait(PlayerTrait.DribladorTecnico) || carrier.HasTrait(PlayerTrait.Flair);
            var side = ((float)_rng.NextDouble() - 0.5f) * (carrier.HasTrait(PlayerTrait.Driblador) || zigzag ? 0.1f : 0.04f);
            var y = home ? Mathf.Min(0.9f, Ball.y + carry) : Mathf.Max(0.1f, Ball.y - carry);
            BallTarget = new Vector2(Mathf.Clamp(Ball.x + side, 0.08f, 0.92f), y);
            ActionKind = "solo";
            Hold = 0f;
            Travel = 0.28f + attr.Pace / 20f * 0.14f;
            Launch(0f);
            var name = string.IsNullOrEmpty(carrier.FullName) ? "O avançado" : carrier.FullName;
            _match.LastEvent = depth > 0.84f ? name + " parte para o golo sozinho." : name + " conduz para a área.";
            _match.AddNote(depth > 0.84f ? name + " tenta o golo em solitário." : name + " conduz.", "solo");
            var finish = depth > 0.88f;
            if (finish)
            {
                _finishNext = true;
                _finishHome = home;
            }

            return true;
        }

        bool Duel(bool home)
        {
            var defenders = home ? _match.AwayOnField : _match.HomeOnField;
            var attackers = home ? _match.HomeOnField : _match.AwayOnField;
            if (defenders.Count == 0 || attackers.Count == 0)
                return false;
            var carrier = Mathf.Clamp(_match.BallHolder, 0, attackers.Count - 1);
            var best = 99f;
            var who = 1;
            for (var i = 1; i < defenders.Count && i < 11; i++)
            {
                if (IsPlayerInjured(defenders[i]))
                    continue;
                var distance = Vector2.Distance(Ball, PlayerSpot(!home, i));
                if (distance < best)
                {
                    best = distance;
                    who = i;
                }
            }

            if (best > 0.075f)
                return false;
            var tack = 10;
            var keep = 10;
            var divingTackle = false;
            if (_match.People.TryGetValue(defenders[who], out var defender) && defender.Attributes != null)
            {
                tack = PlayerAttributes.CurrentValue(defender, "Desarme");
                if (defender.HasTrait(PlayerTrait.Marcador))
                    tack += 3;
                if (defender.HasTrait(PlayerTrait.Trabalhador))
                    tack += 2;
                if (defender.HasTrait(PlayerTrait.CarrinhoArriscado))
                {
                    tack += 2;
                    divingTackle = true;
                }
            }
            if (_match.People.TryGetValue(attackers[carrier], out var attacker) && attacker.Attributes != null)
            {
                keep = PlayerAttributes.CurrentValue(attacker, "Drible") + PlayerAttributes.CurrentValue(attacker, "Força") / 4 + PlayerAttributes.CurrentValue(attacker, "Frieza") / 8;
                if (attacker.HasTrait(PlayerTrait.Driblador))
                    keep += 3;
                if (attacker.HasTrait(PlayerTrait.ImuneAPressao))
                    keep += 4;
                if (attacker.HasTrait(PlayerTrait.Rapido))
                    keep += 1;
            }
            var foulChance = 0.06 + Mathf.Max(0, tack - keep) * 0.008f;
            if (divingTackle)
                foulChance *= 1.45f;
            if (_match.Minute < _quietCards)
                foulChance *= 0.25f;
            var roll = _rng.NextDouble();
            if (roll < foulChance)
            {
                Foul(home, who);
                return true;
            }

            if (roll < 0.22 + (tack - keep) * 0.018)
            {
                Turnover(home, who);
                Bump(defenders[who], 0.08f);
                return true;
            }

            return false;
        }

        void Pass(bool home, bool allowOffsideExemption = false)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var from = Mathf.Clamp(_match.BallHolder, 0, squad.Count - 1);
            var passOrigin = Ball;
            var fromName = Name(squad[from]);
            var runner = _throughBall ? FindRunner(home, from) : -1;
            _throughBall = false;
            var next = runner >= 0 ? runner : _intended >= 0 && _intended < squad.Count && _intended != from ? _intended : PickPassTarget(home, squad, from);
            _intended = -1;
            if (next < 0 || next >= squad.Count || next == from || IsPlayerInjured(squad[next]))
            {
                next = -1;
                var nearest = float.MaxValue;
                for (var i = 0; i < squad.Count && i < 11; i++)
                {
                    if (i == from || IsPlayerInjured(squad[i])) continue;
                    var distance = Vector2.Distance(MotionOf(home, i), Ball);
                    if (distance < nearest) { nearest = distance; next = i; }
                }
                if (next < 0)
                {
                    var opponents = home ? _match.AwayOnField : _match.HomeOnField;
                    var defender = -1;
                    nearest = float.MaxValue;
                    for (var i = 0; i < opponents.Count && i < 11; i++)
                    {
                        if (IsPlayerInjured(opponents[i])) continue;
                        var distance = Vector2.Distance(MotionOf(!home, i), Ball);
                        if (distance < nearest) { nearest = distance; defender = i; }
                    }
                    if (defender >= 0)
                        Turnover(home, defender);
                    return;
                }
            }
            if (!allowOffsideExemption && Offside(home, next))
            {
                FlagOffside(home, next);
                return;
            }
            var toName = Name(squad[next]);
            _match.BallHolder = next;
            _match.HomeHasBall = home;
            BallTarget = ReceivePoint(home, next);
            var hop = Vector2.Distance(Ball, BallTarget);
            var oneTouch = ActionKind == "touch";
            var passer = 10;
            var fail = 0.22f;
            Player passerPlayer = null;
            if (_match.People.TryGetValue(squad[from], out passerPlayer) && passerPlayer.Attributes != null)
            {
                passer = PlayerAttributes.CurrentValue(passerPlayer, "Passe");
                if (passerPlayer.HasTrait(PlayerTrait.Visionario))
                    passer += 4;
                if (passerPlayer.HasTrait(PlayerTrait.ImuneAPressao))
                    passer += 2;
                if (PlayerAttributes.CurrentValue(passerPlayer, "Decisão") <= 8)
                    passer -= 2;
                var overall = CurrentRoleOverall(passerPlayer);
                fail = Mathf.Clamp((16f - passer) / 32f, 0.02f, 0.22f);
                if (hop > 0.32f)
                    fail += 0.06f;
                if (hop < 0.16f)
                    fail *= 0.3f;
                if (overall >= 80)
                    fail *= 0.65f;
                if (overall <= 64)
                    fail *= 1.15f;
                if (fail > 0.28f)
                    fail = 0.28f;
            }
            var attackShape = home ? _match.HomeTactics : _match.AwayTactics;
            var forward = home ? BallTarget.y - Ball.y : Ball.y - BallTarget.y;
            var visionary = passerPlayer != null && (passerPlayer.HasTrait(PlayerTrait.Visionario) || passer >= 15);
            var through = runner >= 0 || (forward > 0.1f && hop > 0.2f && !TikiPlan(attackShape) && (DirectPlan(attackShape) || (visionary && _rng.NextDouble() < 0.45)));
            if (through && passerPlayer != null && passerPlayer.Attributes != null)
                fail += Mathf.Clamp((14f - passer) * 0.012f, 0f, 0.12f);
            if (through)
            {
                var extra = (home ? 1f : -1f) * Mathf.Lerp(0.09f, 0.025f, ControlOf(home, next));
                BallTarget = new Vector2(Mathf.Clamp(BallTarget.x, 0.14f, 0.86f), Mathf.Clamp(BallTarget.y + extra, 0.12f, 0.88f));
                hop = Vector2.Distance(Ball, BallTarget);
            }
            var laneBlocked = PassLaneBlocked(home, Ball, BallTarget);
            var flairPass = !through && laneBlocked && passerPlayer != null && passerPlayer.HasTrait(PlayerTrait.Flair) && _rng.NextDouble() < 0.34;
            if (passerPlayer != null)
            {
                if (oneTouch && hop < 0.24f && passerPlayer.HasTrait(PlayerTrait.PasseDePrimeira))
                    fail *= 0.48f;
                if (flairPass)
                    fail *= 0.68f;
                if (passerPlayer.HasTrait(PlayerTrait.PassadorLongo) && forward > 0.06f && hop > 0.22f)
                    fail *= 0.9f;
            }
            ResolvePendingPass(false, false, null);
            var passEvent = _match.RecordPass(home, squad[from], squad[next], passOrigin, BallTarget, through);
            var miss = _rng.NextDouble() < fail;
            if (miss)
            {
                var loose = Vector2.Lerp(Ball, BallTarget, 0.65f);
                loose.x += (float)(_rng.NextDouble() - 0.5f) * 0.1f;
                loose.y += (float)(_rng.NextDouble() - 0.5f) * 0.06f;
                var nearTouch = BallTarget.x < 0.1f || BallTarget.x > 0.9f || BallTarget.y < 0.08f || BallTarget.y > 0.92f;
                if (nearTouch && _rng.NextDouble() < 0.28)
                {
                    if (BallTarget.y > 0.9f || BallTarget.y < 0.1f)
                        BallTarget = new Vector2(Mathf.Clamp(BallTarget.x, 0.2f, 0.8f), home ? 1.02f : -0.02f);
                    else
                        BallTarget = new Vector2(BallTarget.x < 0.5f ? -0.02f : 1.02f, Mathf.Clamp(BallTarget.y, 0.15f, 0.85f));
                }
                else
                    BallTarget = new Vector2(Mathf.Clamp(loose.x, 0.08f, 0.92f), Mathf.Clamp(loose.y, 0.08f, 0.92f));
                Travel = Mathf.Lerp(0.35f, 0.7f, Mathf.Clamp01(hop / 0.4f));
                ActionKind = "loose";
                _match.BallHolder = from;
                _match.LastEvent = "Passe de " + fromName + " sai desviado.";
                Launch(through ? 0.45f : hop > 0.42f ? 0.22f : 0f);
                Bump(squad[from], -0.06f);
                _lastPasser = null;
                _match.ResolvePass(passEvent, false, false, null);
                return;
            }
            _pendingPass = passEvent;
            _lastPasser = squad[from];
            _lastPasserHome = home;
            NotePass(home, next);
            Travel = Mathf.Lerp(0.1f, 0.22f, Mathf.Clamp01(hop / 0.45f));
            ActionKind = "pass";
            _curl = passerPlayer != null && passerPlayer.HasTrait(PlayerTrait.Efeito) && !through && hop > 0.08f && hop < 0.38f
                ? (_rng.NextDouble() < 0.5 ? -0.55f : 0.55f) : 0f;
            Launch(through ? 0.82f : flairPass ? 0.38f : hop > 0.48f ? 0.18f : 0f);
            Bump(squad[from], 0.05f);
            Bump(squad[next], 0.04f);
            if (through)
                _match.LastEvent = "Passe em profundidade de " + fromName + " para " + toName + ".";
            else if (flairPass)
                _match.LastEvent = "Passe criativo de " + fromName + " para " + toName + ".";
            else
                _match.LastEvent = fromName == toName
                    ? "Passe de " + Tag(squad[from]) + " para " + Tag(squad[next]) + "."
                    : "Passe de " + fromName + " para " + toName + ".";
            var receiveDepth = home ? BallTarget.y : 1f - BallTarget.y;
            var receiveWide = Mathf.Abs(BallTarget.x - 0.5f) > 0.24f;
            if (receiveDepth > 0.7f && !receiveWide)
            {
                var fin = 10;
                if (_match.People.TryGetValue(squad[next], out var finisher) && finisher.Attributes != null)
                    fin = PlayerAttributes.CurrentValue(finisher, "Finalização");
                var gap = MarkerGap(home, next);
                var will = 0.28f + (fin - 10) * 0.035f + (gap > 0.1f ? 0.22f : 0f);
                if (TikiPlan(attackShape))
                    will -= 0.16f;
                if (finisher != null && finisher.HasTrait(PlayerTrait.Finalizador))
                    will += 0.1f;
                if (_rng.NextDouble() < will)
                {
                    _finishNext = true;
                    _finishHome = home;
                }
            }
        }

        bool PassLaneBlocked(bool home, Vector2 from, Vector2 to)
        {
            var defending = home ? _match.AwayOnField : _match.HomeOnField;
            var path = to - from;
            var length = path.sqrMagnitude;
            if (length < 0.002f)
                return false;
            for (var i = 1; i < defending.Count && i < 11; i++)
            {
                var point = MotionOf(!home, i);
                var along = Mathf.Clamp01(Vector2.Dot(point - from, path) / length);
                if (along > 0.06f && along < 0.94f && Vector2.Distance(point, from + path * along) < 0.045f)
                    return true;
            }
            return false;
        }

        int FindRunner(bool home, int from)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (squad.Count == 0)
                return -1;
            from = Mathf.Clamp(from, 0, squad.Count - 1);
            var fromSpot = LiveSpot(home, from);
            var best = -1;
            var bestScore = 1.15f;
            for (var i = 1; i < squad.Count && i < 11; i++)
            {
                if (i == from || IsPlayerInjured(squad[i]))
                    continue;
                var spot = LiveSpot(home, i);
                var forward = home ? spot.y - fromSpot.y : fromSpot.y - spot.y;
                if (forward < 0.06f)
                    continue;
                var space = MarkerGap(home, i);
                var pace = 10;
                if (_match.People.TryGetValue(squad[i], out var mate) && mate.Attributes != null)
                {
                    pace = PlayerAttributes.CurrentValue(mate, "Velocidade");
                    if (mate.HasTrait(PlayerTrait.Rapido))
                        pace += 3;
                }

                var score = forward * 2.2f + space * 3.1f + pace / 20f;
                if (space < 0.08f)
                    score -= 1.8f;
                if (pace >= 15 && forward > 0.1f)
                    score += 0.45f;
                var line = BackLine(!home);
                var past = home ? spot.y > line - 0.04f : spot.y < line + 0.04f;
                var offsideLine = OffsideLine(home);
                var attackingDepth = home ? spot.y : 1f - spot.y;
                if (attackingDepth > Mathf.Min(offsideLine, home ? Ball.y : 1f - Ball.y) - 0.015f)
                    score -= 2f;
                if (past && pace >= 13 && attackingDepth <= offsideLine - 0.06f)
                    score += 0.28f;
                if (score <= bestScore)
                    continue;
                bestScore = score;
                best = i;
            }

            return best;
        }

        bool TryProgress(bool home)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var from = Mathf.Clamp(_match.BallHolder, 0, Mathf.Max(0, squad.Count - 1));
            if (from == 0)
                return false;
            var carrier = Person(home, from);
            if (carrier == null || carrier.Position == PlayerPosition.Goalkeeper)
                return false;
            var tactics = home ? _match.HomeTactics : _match.AwayTactics;
            var widePlan = CrossPlan(tactics);
            var x = Ball.x;
            if (widePlan)
                x = Mathf.MoveTowards(x, x < 0.5f ? 0.14f : 0.86f, 0.08f);
            else
                x = Mathf.Clamp(x + (float)(_rng.NextDouble() - 0.5f) * 0.06f, 0.12f, 0.88f);
            var y = home ? Mathf.Min(0.9f, Ball.y + 0.09f) : Mathf.Max(0.1f, Ball.y - 0.09f);
            BallTarget = new Vector2(x, y);
            ActionKind = "solo";
            Hold = 0f;
            Travel = 0.32f;
            Launch(0f);
            var name = string.IsNullOrEmpty(carrier.FullName) ? "O portador" : carrier.FullName;
            _match.LastEvent = name + " conduz para a frente.";
            _match.AddNote(name + " conduz.", "solo");
            return true;
        }

        int PickPassTarget(bool home, List<string> squad, int from)
        {
            if (squad.Count == 0)
                return 0;
            var fromSpot = PlayerSpot(home, from);
            var depth = home ? Ball.y : 1f - Ball.y;
            var best = -1;
            var bestScore = -99f;
            var tactics = home ? _match.HomeTactics : _match.AwayTactics;
            var safe = tactics.PassingStyle;
            var tiki = TikiPlan(tactics);
            var cross = CrossPlan(tactics);
            var direct = DirectPlan(tactics);
            for (var i = 0; i < squad.Count && i < 11; i++)
            {
                if (i != from && IsPlayerInjured(squad[i]))
                    continue;
                var spot = PlayerSpot(home, i);
                var forward = home ? spot.y - fromSpot.y : fromSpot.y - spot.y;
                var distance = Vector2.Distance(fromSpot, spot);
                var score = forward * 1.1f - distance * (safe != null && safe.Contains("arriscado") ? 4.2f : 7.4f) + (float)(_rng.NextDouble() * 0.08);
                var chasing = (home ? _match.HomeGoals : _match.AwayGoals) <= (home ? _match.AwayGoals : _match.HomeGoals);
                if (chasing && forward > 0.04f)
                    score += 1.1f;
                if (tiki)
                {
                    if (distance > 0.22f)
                        score -= 7f;
                    if (distance < 0.18f && forward > -0.02f)
                        score += 1.8f;
                    if (forward > 0.02f && forward < 0.16f)
                        score += 1.3f;
                }
                else if (cross)
                {
                    var wing = Mathf.Abs(spot.x - 0.5f);
                    score += wing * 3.4f;
                    if (wing > 0.2f && forward > -0.04f)
                        score += 1.4f;
                    if (wing < 0.08f)
                        score -= 1.2f;
                }
                else if (direct && forward > 0.12f)
                {
                    score += 2.2f;
                    if (distance > 0.22f)
                        score += 2.4f;
                }
                if (distance > 0.2f)
                    score -= 1.2f;
                if (distance < 0.16f)
                    score += 0.8f;
                if (distance > 0.38f && !(direct && forward > 0.08f))
                    score -= 2.2f;
                else if (direct && forward > 0.1f && distance > 0.24f)
                    score += 1.6f;
                if (depth < 0.55f && forward > 0.04f && distance < 0.28f)
                    score += 0.45f;
                Player passer = null;
                if (_match.People.TryGetValue(squad[from], out passer))
                {
                    var passing = passer.Attributes == null ? 10 : PlayerAttributes.CurrentValue(passer, "Passe");
                    var dribbling = passer.Attributes == null ? 10 : PlayerAttributes.CurrentValue(passer, "Drible");
                    if (passer.HasTrait(PlayerTrait.Visionario) && forward > 0.02f)
                        score += 0.7f;
                    if ((passer.HasTrait(PlayerTrait.Visionario) || passing >= 15) && distance > 0.26f && forward > 0.06f)
                        score += 1.7f;
                    if (passer.HasTrait(PlayerTrait.Cruzador) && Mathf.Abs(spot.x - 0.5f) > 0.18f)
                        score += 1.1f;
                    if ((passer.HasTrait(PlayerTrait.Driblador) || dribbling >= 16) && distance > 0.18f)
                        score -= 1.1f;
                    if (passer.HasTrait(PlayerTrait.Rapido) && forward > 0.08f)
                        score += 0.45f;
                    if (passer.HasTrait(PlayerTrait.PassadorLongo) && forward > 0.06f && distance > 0.22f && distance < 0.43f)
                        score += 1.05f;
                }
                if (_match.People.TryGetValue(squad[i], out var mate) && mate.HasTrait(PlayerTrait.Rapido) && forward > 0.04f)
                    score += 0.4f;
                if (i == from)
                    continue;
                var space = MarkerGap(home, i);
                if (_match.People.TryGetValue(squad[i], out var receiver) && receiver.HasTrait(PlayerTrait.CriadorDeJogo) && space > 0.12f)
                    score += 1.15f;
                if (space > 0.16f)
                    score += (space - 0.1f) * 16f;
                if (space > 0.2f && forward > 0f)
                    score += 2.8f;
                if (space < 0.09f)
                    score -= 2.4f;
                var recent = 0;
                if (_passSide == (home ? 1 : 2))
                {
                    for (var m = 0; m < _passChain.Length; m++)
                    {
                        if (_passChain[m] == i)
                            recent++;
                    }
                }

                if (recent > 0)
                    score -= 2.6f * recent;
                if (!string.IsNullOrEmpty(_lastPasser) && squad[i] == _lastPasser)
                    score -= 3.4f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }

            if (best < 0)
            {
                var nearest = from;
                var nearestDistance = 99f;
                for (var i = 0; i < squad.Count && i < 11; i++)
                {
                    if (i == from)
                        continue;
                    var distance = Vector2.Distance(fromSpot, PlayerSpot(home, i));
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = i;
                    }
                }

                return nearest == from ? (from + 1) % Mathf.Min(11, squad.Count) : nearest;
            }

            return best;
        }

        void NotePass(bool home, int index)
        {
            var side = home ? 1 : 2;
            if (_passSide != side)
            {
                for (var i = 0; i < _passChain.Length; i++)
                    _passChain[i] = -1;
                _passSide = side;
            }

            for (var i = _passChain.Length - 1; i > 0; i--)
                _passChain[i] = _passChain[i - 1];
            _passChain[0] = index;
        }

        void ResolvePendingPass(bool completed, bool intercepted, string receiverId)
        {
            if (_pendingPass == null)
                return;
            _match.ResolvePass(_pendingPass, completed, intercepted, receiverId);
            _pendingPass = null;
        }

        bool Offside(bool home, int index)
        {
            var attackers = home ? _match.HomeOnField : _match.AwayOnField;
            if (index < 0 || index >= attackers.Count || IsPlayerInjured(attackers[index]))
                return false;
            var spot = MotionOf(home, index);
            var attackerDepth = home ? spot.y : 1f - spot.y;
            var ballDepth = home ? Ball.y : 1f - Ball.y;
            const float positionEpsilon = 0.0025f;

            // A player level with the halfway line, ball, or second-last opponent is onside.
            // Keep tolerance to a few pixels so the model does not erase real offside calls.
            if (attackerDepth <= 0.5f + positionEpsilon || attackerDepth <= ballDepth + positionEpsilon)
                return false;
            var opp = home ? _match.AwayOnField : _match.HomeOnField;
            var deepest = float.NegativeInfinity;
            var secondDeepest = float.NegativeInfinity;
            var count = 0;
            for (var i = 0; i < opp.Count && i < 11; i++)
            {
                if (IsPlayerInjured(opp[i]))
                    continue;
                var mark = MotionOf(!home, i);
                var depth = home ? mark.y : 1f - mark.y;
                count++;
                if (depth >= deepest)
                {
                    secondDeepest = deepest;
                    deepest = depth;
                }
                else if (depth > secondDeepest)
                    secondDeepest = depth;
            }

            if (count < 2)
                return false;
            var secondLastDefender = secondDeepest;
            var line = Mathf.Max(ballDepth, secondLastDefender);
            return attackerDepth > line + positionEpsilon;
        }

        void FlagOffside(bool home, int index)
        {
            _lastPasser = null;
            _settle = 0f;
            _finishNext = false;
            _headerNext = false;
            _throughBall = false;
            _intended = -1;
            _pieceLive = false;
            var spot = PlayerSpot(home, index);
            BallTarget = new Vector2(Mathf.Clamp(spot.x, 0.12f, 0.88f), Mathf.Clamp(spot.y, 0.12f, 0.88f));
            Travel = 0.4f;
            _match.HomeHasBall = !home;
            _match.BallHolder = 4;
            ActionKind = "offside";
            Hold = 1.15f;
            SnapBallVisual();
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var name = squad.Count == 0 ? "um avançado" : Name(squad[Mathf.Clamp(index, 0, squad.Count - 1)]);
            _match.LastEvent = "Fora de jogo de " + name + ".";
            _match.AddNote("Fora de jogo.", "offside");
        }

        void Shoot(bool home)
        {
            Shoot(home, false);
        }

        void Shoot(bool home, bool penalty)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var index = Mathf.Clamp(_match.BallHolder, 0, squad.Count - 1);
            var shooterId = squad[index];
            var shotOrigin = Ball;
            var shooter = Name(shooterId);
            var depth = home ? Ball.y : 1f - Ball.y;
            Player shooterPlayer = null;
            _match.People.TryGetValue(shooterId, out shooterPlayer);
            var fromCross = ActionKind == "cross";
            var bicycle = !penalty && fromCross && depth > 0.7f && shooterPlayer != null
                && shooterPlayer.HasTrait(PlayerTrait.Chilenas)
                && _rng.NextDouble() < 0.24;
            var header = _headerNext && !bicycle;
            _headerNext = false;
            var longVolley = shooterPlayer != null && (shooterPlayer.HasTrait(PlayerTrait.RemateDeLonge) || shooterPlayer.HasTrait(PlayerTrait.RematadorDeLonge));
            if (!penalty && header && depth < 0.68f && !(longVolley && depth >= 0.6f))
            {
                Pass(home);
                return;
            }

            var fromFar = longVolley;
            var powerfulFreeKick = !penalty && _pieceKind == "freekick" && shooterPlayer != null && shooterPlayer.HasTrait(PlayerTrait.LivrePotente);
            var outsideFoot = !header && !bicycle && !penalty && shooterPlayer != null && shooterPlayer.HasTrait(PlayerTrait.RemateDeTrivela)
                && _rng.NextDouble() < (shooterPlayer.HasTrait(PlayerTrait.Efeito) ? 0.58 : 0.36);
            if (depth < 0.58f && !penalty && !header && !(fromFar && depth >= 0.6f))
            {
                Pass(home);
                return;
            }

            var quality = AttackStrength(home ? _match.HomeTactics : _match.AwayTactics);
            var save = DefenseStrength(home ? _match.AwayTactics : _match.HomeTactics);
            var boost = home ? _match.AttackBoost : 0f;
            var wall = home ? _match.DefenseBoost : 0f;
            var personal = 0.0;
            var aim = 0.55;
            if (shooterPlayer != null && shooterPlayer.Attributes != null)
            {
                personal += (PlayerAttributes.CurrentValue(shooterPlayer, "Finalização") - 10) * 0.016;
                personal += (CurrentRoleOverall(shooterPlayer) - 74) * 0.004;
                personal += (PlayerAttributes.CurrentValue(shooterPlayer, "Frieza") - 10) * 0.004;
                if (PlayerAttributes.CurrentValue(shooterPlayer, "Finalização") <= 8)
                    aim -= 0.2;
                if (shooterPlayer.HasTrait(PlayerTrait.RemateEmJeito))
                {
                    personal += 0.05;
                    aim = 0.8;
                }
                else if (PlayerAttributes.CurrentValue(shooterPlayer, "Finalização") >= 15)
                    aim = 0.66;
                if (shooterPlayer.HasTrait(PlayerTrait.Potente))
                    personal += 0.04;
                if (shooterPlayer.HasTrait(PlayerTrait.Frieza) && depth > 0.82f)
                    personal += 0.05;
                if (shooterPlayer.HasTrait(PlayerTrait.Cabeceador) && header)
                    personal += 0.06;
                if (shooterPlayer.HasTrait(PlayerTrait.Finalizador))
                    personal += 0.02;
                if (shooterPlayer.HasTrait(PlayerTrait.Potente) && !shooterPlayer.HasTrait(PlayerTrait.RemateEmJeito))
                    aim -= 0.08;
                if (outsideFoot)
                    aim -= shooterPlayer.HasTrait(PlayerTrait.Efeito) ? 0.015 : 0.04;
                if (powerfulFreeKick)
                {
                    personal += 0.025;
                    aim -= 0.025;
                }
            }

            var keeper = Person(!home, 0);
            var keeperFootSave = keeper != null && keeper.HasTrait(PlayerTrait.GuardaRedesDefendeComOsPes) && depth > 0.78f && _rng.NextDouble() < 0.62;
            if (keeper != null && keeper.Attributes != null)
            {
                personal -= (PlayerAttributes.CurrentValue(keeper, "Reflexos") - 10) * 0.007;
                if (keeper.HasTrait(PlayerTrait.Frieza))
                    personal -= 0.04;
                if (keeper.HasTrait(PlayerTrait.ImuneAPressao))
                    aim -= 0.06;
                if (keeperFootSave)
                    personal -= 0.045;
            }

            if (home) _match.HomeShots++;
            else _match.AwayShots++;
            var flairShot = !header && !bicycle && !penalty && shooterPlayer != null && shooterPlayer.HasTrait(PlayerTrait.Flair)
                && depth > 0.66f && _rng.NextDouble() < 0.14;
            if (flairShot)
                aim += 0.035;
            var bent = !header && !bicycle && shooterPlayer != null && (outsideFoot || shooterPlayer.HasTrait(PlayerTrait.RemateEmJeito)
                || (shooterPlayer.Attributes != null && PlayerAttributes.CurrentValue(shooterPlayer, "Finalização") >= 16 && _rng.NextDouble() < 0.3));
            _curl = bent ? (float)(_rng.NextDouble() < 0.5 ? -0.9 : 0.9) : 0f;
            var central = 1f - Mathf.Abs(Ball.x - 0.5f) * 1.5f;
            if (central < 0.18f)
                central = 0.18f;
            var closeness = depth >= 0.88f ? 1f : depth >= 0.8f ? 0.72f : depth >= 0.74f ? 0.4f : 0.22f;
            if (header)
                closeness *= shooterPlayer != null && shooterPlayer.HasTrait(PlayerTrait.Cabeceador) ? 0.9f : 0.62f;
            var chance = 0.06 + closeness * central * 0.28 + personal + boost * 0.5 - wall * 0.5 - save * 0.04 + quality * 0.04;
            if (chance < 0.04)
                chance = 0.04;
            if (chance > 0.62)
                chance = 0.62;
            if (penalty)
            {
                var finishing = shooterPlayer == null || shooterPlayer.Attributes == null ? 10 : PlayerAttributes.CurrentValue(shooterPlayer, "Finalização");
                var composure = shooterPlayer == null || shooterPlayer.Attributes == null ? 10 : PlayerAttributes.CurrentValue(shooterPlayer, "Frieza");
                var reflexes = keeper == null || keeper.Attributes == null ? 10 : PlayerAttributes.CurrentValue(keeper, "Reflexos");
                chance = 0.74 + (finishing - 10) * 0.006 + (composure - 10) * 0.008 - (reflexes - 10) * 0.006;
                if (shooterPlayer != null && shooterPlayer.HasTrait(PlayerTrait.Finalizador))
                    chance += 0.01;
                if (shooterPlayer != null && shooterPlayer.HasTrait(PlayerTrait.Frieza))
                    chance += 0.015;
                chance = Mathf.Clamp((float)chance, 0.62f, 0.86f);
            }

            var shotRoll = _rng.NextDouble();
            var goal = shotRoll < chance;
            if (penalty && !goal)
                _match.AddNote("Penálti falhado por " + shooter + ".", "penalty-miss", shooterId);
            var assistId = _lastPasserHome == home && !string.IsNullOrEmpty(_lastPasser) && _lastPasser != shooterId
                ? _lastPasser : null;
            _lastPasser = null;
            var post = !goal && !penalty && shotRoll < chance + 0.08 && central > 0.5f && depth > 0.74f;
            var onTarget = goal || (!post && (penalty
                ? shotRoll < chance + 0.15
                : shotRoll < chance + 0.22 + aim * 0.25));
            var placement = Mathf.Clamp01(((float)aim - 0.35f) / 0.65f);
            var lateralSpread = Mathf.Lerp(0.065f, 0.025f, placement);
            var oppositeSideBias = Mathf.Clamp((0.5f - Ball.x) * 0.08f, -0.012f, 0.012f);
            var shotTargetX = Mathf.Clamp(0.5f + oppositeSideBias
                + ((float)_rng.NextDouble() * 2f - 1f) * lateralSpread, 0.405f, 0.595f);
            var bigSave = onTarget && !goal && keeper != null && keeper.Attributes != null && (PlayerAttributes.CurrentValue(keeper, "Reflexos") >= 14 || (!penalty && chance > 0.3));
            var narrow = !goal && !onTarget && !post && central > 0.42f && depth > 0.76f;
            var driven = shooterPlayer != null && (shooterPlayer.HasTrait(PlayerTrait.Potente) || powerfulFreeKick);
            var low = shooterPlayer != null && shooterPlayer.HasTrait(PlayerTrait.RemateRasteiro);
            var placed = shooterPlayer != null && shooterPlayer.HasTrait(PlayerTrait.RemateEmJeito);
            var how = bicycle ? "Chilena de " + shooter + "."
                : powerfulFreeKick ? "Livre direto potente de " + shooter + "."
                : outsideFoot ? "Remate de trivela de " + shooter + "."
                : flairShot ? "Remate com flair de " + shooter + "."
                : header ? "Cabeceamento de " + shooter + "."
                : low ? "Remate rasteiro de " + shooter + "."
                : fromFar && depth < 0.78f ? "Remate de longe de " + shooter + "."
                : driven ? "Remate potente de " + shooter + "."
                : placed ? "Remate colocado de " + shooter + "."
                : "Remate de " + shooter + ".";
            var shotApex = header ? 0.34f : low ? 0.03f : powerfulFreeKick ? 0.2f : flairShot ? 0.4f : driven ? 0.62f : fromFar ? 0.5f : placed ? 0.2f : 0.28f;
            Travel = powerfulFreeKick ? 1.05f : depth > 0.86f ? 0.55f : 0.9f;
            if (goal)
            {
                BallTarget = home ? new Vector2(shotTargetX, 0.985f) : new Vector2(shotTargetX, 0.015f);
                _match.RecordShot(home, shooterId, shotOrigin, BallTarget, (float)chance, "goal", header);
                ActionKind = "goal";
                Hold = 1.8f;
                Launch(shotApex);
                _pendingGoal = true;
                _pendingGoalHome = home;
                _pendingPenalty = penalty;
                _pendingScorerId = shooterId;
                _pendingAssistId = assistId;
                var englishGoal = LocalizationManager.Current == GameLanguage.English;
                _pendingGoalDescription = bicycle ? (englishGoal ? "BICYCLE KICK GOAL" : "GOLO DE CHILENA")
                    : penalty ? (englishGoal ? "PENALTY GOAL" : "GOLO DE PENÁLTI")
                    : powerfulFreeKick ? (englishGoal ? "FREE-KICK GOAL" : "GOLO DE LIVRE DIRETO")
                    : outsideFoot ? (englishGoal ? "OUTSIDE-OF-THE-BOOT GOAL" : "GOLO DE TRIVELA")
                    : header ? (englishGoal ? "HEADER GOAL" : "GOLO DE CABEÇA")
                    : placed ? (englishGoal ? "CURLED FINISH" : "GOLO EM JEITO")
                    : flairShot ? (englishGoal ? "FLAIR FINISH" : "GOLO COM EFEITO")
                    : "";
                _match.HomeHasBall = !home;
                _match.BallHolder = 0;
                return;
            }

            if (onTarget)
            {
                if (home) _match.HomeChances++;
                else _match.AwayChances++;
                BallTarget = home ? new Vector2(shotTargetX, 0.86f) : new Vector2(shotTargetX, 0.14f);
                _match.RecordShot(home, shooterId, shotOrigin, BallTarget, (float)chance, "saved", header);
                ActionKind = "shot";
                Hold = 0.45f;
                Launch(Mathf.Max(0.08f, shotApex * 0.7f));
                Bump(shooterId, 0.12f);
                _match.LastEvent = keeperFootSave ? how + " O guarda-redes defende com os pés."
                    : bigSave ? "Grande defesa. " + how : how + " O guarda-redes defende.";
                if (bigSave)
                    GameAudio.BigSave();
                _match.HomeHasBall = !home;
                _match.BallHolder = 0;
                return;
            }

            if (post)
            {
                var side = shotTargetX < 0.5f ? 0.38f : 0.62f;
                BallTarget = home ? new Vector2(side, 0.97f) : new Vector2(side, 0.03f);
                _match.RecordShot(home, shooterId, shotOrigin, BallTarget, (float)chance, "post", header);
                _match.LastEvent = how + " Bate na trave.";
                if (home) _match.HomeChances++;
                else _match.AwayChances++;
                GameAudio.ShotPost();
            }
            else
            {
                BallTarget = MissWide(home);
                _match.RecordShot(home, shooterId, shotOrigin, BallTarget, (float)chance, "miss", header);
                _match.LastEvent = narrow ? how + " Passa ao lado da baliza." : how + " Falha a baliza.";
                if (narrow)
                    GameAudio.ShotWide();
            }
            ActionKind = "shot";
            Hold = 0.2f;
            Launch(post ? Mathf.Max(shotApex, 0.2f) : shotApex);
            Bump(shooterId, 0.04f);
            _match.HomeHasBall = home;
            _match.BallHolder = index;
        }

        void Bump(string id, float delta)
        {
            var current = _match.RatingOf(id);
            _match.Ratings[id] = Mathf.Clamp(current + delta, 3.5f, 10f);
        }

        void Turnover(bool home, int taker)
        {
            ResolvePendingPass(false, false, null);
            _lastPasser = null;
            _match.HomeHasBall = !home;
            var squad = home ? _match.AwayOnField : _match.HomeOnField;
            if (squad.Count == 0)
                return;
            taker = Mathf.Clamp(taker, 0, squad.Count - 1);
            _match.BallHolder = taker;
            var spot = MotionOf(!home, taker);
            BallTarget = Vector2.Distance(Ball, spot) <= 0.1f ? spot : Vector2.Lerp(Ball, spot, 0.35f);
            ActionKind = "turnover";
            Launch(0f);
            _match.LastEvent = "Corte. " + Name(squad[taker]) + " recupera a bola.";
        }

        void Corner(bool home)
        {
            var left = Ball.x < 0.5f;
            var spot = new Vector2(left ? 0.015f : 0.985f, home ? 0.985f : 0.015f);
            var club = home ? _match.Home : _match.Away;
            var taker = PickRoleTaker(home, club == null ? null : club.CornerTakerId);
            BeginSetPiece(home, "corner", spot, taker);
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var name = squad.Count == 0 ? "o cobrador" : Name(squad[Mathf.Clamp(taker, 0, squad.Count - 1)]);
            Announce("CANTO", squad.Count == 0 ? null : squad[Mathf.Clamp(taker, 0, squad.Count - 1)]);
            _match.LastEvent = "Canto. " + name + " vai bater o canto.";
            _match.AddNote("Canto.", "corner");
        }

        void GoalKick(bool home)
        {
            var spot = home ? new Vector2(0.5f, 0.07f) : new Vector2(0.5f, 0.93f);
            BeginSetPiece(home, "goalkick", spot, 0);
            _match.LastEvent = "Pontapé de baliza.";
        }

        void Foul(bool home, int defenderIndex)
        {
            ResolvePendingPass(false, false, null);
            _lastPasser = null;
            _settle = 0f;
            _finishNext = false;
            _headerNext = false;
            _throughBall = false;
            _intended = -1;
            var defendingHome = !home;
            var squad = defendingHome ? _match.HomeOnField : _match.AwayOnField;
            var attack = home ? _match.HomeOnField : _match.AwayOnField;
            if (squad.Count == 0 || attack.Count == 0)
                return;
            var index = Mathf.Clamp(defenderIndex, 0, squad.Count - 1);
            var fouler = Name(squad[index]);
            var side = defendingHome ? _match.Home.ShortName : _match.Away.ShortName;
            var depth = home ? Ball.y : 1f - Ball.y;
            _pieceHome = home;
            _piecePenalty = depth > 0.86f && Ball.x > 0.35f && Ball.x < 0.65f;
            _pieceSpot = new Vector2(Mathf.Clamp(Ball.x, 0.18f, 0.82f), Mathf.Clamp(Ball.y, 0.12f, 0.88f));
            if (_piecePenalty)
                _pieceSpot = home ? new Vector2(0.5f, 0.88f) : new Vector2(0.5f, 0.12f);
            Ball = _pieceSpot;
            BallTarget = _pieceSpot;
            SnapBallVisual();
            Travel = 0.05f;
            _pieceTaker = PickSetPieceTaker(home, _piecePenalty ? 8 : 5);
            _match.BallHolder = _pieceTaker;
            var wall = _piecePenalty ? 0 : 2 + _rng.Next(0, 3);
            _wall = new int[wall];
            for (var i = 0; i < wall; i++)
                _wall[i] = Mathf.Clamp(1 + i, 0, squad.Count - 1);
            _pieceLive = true;
            _pieceKind = _piecePenalty ? "penalty" : "freekick";
            ActionKind = _piecePenalty ? "penalty" : "freekick";
            var takerRun = Vector2.Distance(FormationSpot(home, _pieceTaker), _pieceSpot);
            Hold = Mathf.Clamp(1.25f + takerRun / 0.24f, 2f, 4.8f);
            if (!_piecePenalty && _match.People.TryGetValue(attack[_pieceTaker], out var freeKickTaker) && freeKickTaker.HasTrait(PlayerTrait.LivrePotente))
            {
                Hold += 0.65f;
                _match.LastEvent = freeKickTaker.FullName + " recua para ganhar balanço no livre.";
            }
            _match.HomeHasBall = home;
            Announce(_piecePenalty ? "PENÁLTI" : "FALTA", squad[index]);
            if (_piecePenalty)
                GameAudio.Penalty();
            _match.AddNote((_piecePenalty ? "Penálti. " : "Falta de ") + fouler + " (" + side + ").", "foul");
            if (_match.Minute >= _quietCards && _rng.NextDouble() < Mathf.Clamp(0.22f * _refereeMultiplier, 0.05f, 0.45f))
            {
                var red = Book(squad[index], false);
                Announce(red ? "CARTÃO VERMELHO" : "CARTÃO AMARELO", squad[index]);
                _match.AddNote((red ? "Segundo amarelo e expulsão para " : "Amarelo para ") + fouler + ".", red ? "red" : "yellow");
                _quietCards = _match.Minute + 12f;
            }
        }

        void StrikePiece()
        {
            var home = _pieceHome;
            var penalty = _piecePenalty;
            var kind = _pieceKind;
            var taker = _pieceTaker;
            var spot = _pieceSpot;
            var crossing = CrossingOf(home, taker);
            var target = kind == "corner" ? PickCrossTarget(home, taker) : -1;
            _pieceLive = false;
            _pieceKind = "";
            _wall = new int[0];
            _match.HomeHasBall = home;
            _match.BallHolder = taker;
            if (kind == "corner")
            {
                DeliverCross(home, taker, target, crossing, true);
                return;
            }

            if (kind == "throwin")
            {
                DeliverThrow(home, taker, spot);
                return;
            }

            if (kind == "goalkick")
            {
                var squad = home ? _match.HomeOnField : _match.AwayOnField;
                var next = squad.Count > 4 ? 4 + _rng.Next(0, Mathf.Min(4, squad.Count - 4)) : 0;
                _match.BallHolder = next;
                BallTarget = FormationSpot(home, next);
                var hop = Vector2.Distance(Ball, BallTarget);
                Travel = 0.7f;
                Launch(hop > 0.25f ? 0.7f : 0.3f);
                ActionKind = "pass";
                _match.LastEvent = "Pontapé de baliza.";
                return;
            }

            var depth = home ? spot.y : 1f - spot.y;
            if (penalty || depth > 0.7f)
                Shoot(home, penalty);
            else
                Pass(home);
        }

        static void AddStat(Dictionary<string, int> map, string id)
        {
            if (map == null || string.IsNullOrEmpty(id))
                return;
            if (!map.ContainsKey(id))
                map[id] = 0;
            map[id]++;
        }

        void ThrowIn(bool home, float along, bool left)
        {
            var spot = new Vector2(left ? 0.015f : 0.985f, Mathf.Clamp(along, 0.08f, 0.92f));
            var taker = ThrowTaker(home, spot);
            BeginSetPiece(home, "throwin", spot, taker);
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var name = squad.Count == 0 ? "o jogador" : Name(squad[Mathf.Clamp(taker, 0, squad.Count - 1)]);
            _match.LastEvent = "Lançamento lateral. " + name + " vai buscar a bola.";
            _match.AddNote("Lançamento.", "throwin");
        }

        void Card(bool home)
        {
            var squad = home ? _match.AwayOnField : _match.HomeOnField;
            if (squad.Count == 0)
                return;
            var index = _rng.Next(0, Mathf.Min(11, squad.Count));
            var id = squad[index];
            var name = Name(id);
            var red = _rng.NextDouble() < 0.12;
            ActionKind = "card";
            Hold = 1.3f;
            Book(id, red);
            Announce(red ? "CARTÃO VERMELHO" : "CARTÃO AMARELO", id);
            _match.AddNote((red ? "Vermelho para " : "Amarelo para ") + name + ".", red ? "red" : "yellow");
        }

        void Announce(string text, string playerId = null)
        {
            _match.Flash = text;
            _match.FlashPlayerId = playerId;
            _match.FlashSerial++;
        }

        void CompleteGoal()
        {
            if (!_pendingGoal)
                return;

            var kickoffForHome = !_pendingGoalHome;

            if (_pendingGoalHome)
            {
                _match.HomeChances++;
                _match.HomeGoals++;
            }
            else
            {
                _match.AwayChances++;
                _match.AwayGoals++;
            }

            AddStat(_match.GoalsScored, _pendingScorerId);
            if (_pendingPenalty)
                AddStat(_match.PenaltyGoals, _pendingScorerId);
            if (!string.IsNullOrEmpty(_pendingAssistId))
            {
                AddStat(_match.Assists, _pendingAssistId);
                Bump(_pendingAssistId, 0.45f);
            }

            Bump(_pendingScorerId, _pendingPenalty ? 1.1f : 0.85f);
            var goalCallout = string.IsNullOrEmpty(_pendingGoalDescription)
                ? LocalizationManager.Current == GameLanguage.English ? "GOAL" : "GOLO"
                : _pendingGoalDescription;
            _match.AddNote(goalCallout, "goal", _pendingScorerId);
            Announce(goalCallout, _pendingScorerId);
            GameAudio.CrowdGoal(_pendingGoalHome);
            _pendingGoal = false;
            _goalRestartHome = kickoffForHome;
            _goalCelebrationRemaining = Mathf.Max(0.55f, Hold);
            _goalCelebration = true;
            Hold = 0f;
            Ball = BallTarget;
            SnapBallVisual();
            _match.LastEvent = goalCallout;
        }

        bool Book(string id, bool red)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            var previous = _match.Bookings.TryGetValue(id, out var existing) ? existing : 0;
            red = red || previous >= 1;
            _match.Bookings[id] = red ? 2 : 1;
            if (red)
                _match.SentOffPlayers.Add(id);
            return red;
        }

        string Name(string id)
        {
            return _match.Names.TryGetValue(id, out var name) ? name : "Jogador";
        }

        string Tag(string id)
        {
            var name = Name(id);
            var parts = name.Split(' ');
            var last = parts.Length == 0 ? name : parts[parts.Length - 1];
            if (_match.People.TryGetValue(id, out var player) && player != null)
                return last + " (" + player.SquadNumber + ")";
            return last;
        }

        void Finish()
        {
            ResolvePendingPass(false, false, null);
            _match.Phase = MatchPhase.FullTime;
            _match.Finished = true;
            _match.Minute = 90f;
            _match.AddNote("Fim do jogo.", "full");
            _match.Fixture.HomeGoals = _match.HomeGoals;
            _match.Fixture.AwayGoals = _match.AwayGoals;
        }

        public static Vector2 Slot(bool home, int index, string formation)
        {
            Vector2[] spots;
            switch (formation)
            {
                case "4-4-2":
                    spots = Spots(0.08f, new[] { 0.82f, 0.62f, 0.38f, 0.18f }, 0.22f, new[] { 0.78f, 0.58f, 0.42f, 0.22f }, 0.48f, new[] { 0.62f, 0.38f }, 0.78f);
                    break;
                case "4-2-3-1":
                    spots = Spots(0.08f, new[] { 0.82f, 0.62f, 0.38f, 0.18f }, 0.24f, new[] { 0.62f, 0.38f }, 0.42f, new[] { 0.78f, 0.50f, 0.22f }, 0.62f, new[] { 0.50f }, 0.82f);
                    break;
                case "3-4-3":
                    spots = Spots(0.08f, new[] { 0.66f, 0.50f, 0.34f }, 0.22f, new[] { 0.82f, 0.58f, 0.42f, 0.18f }, 0.46f, new[] { 0.78f, 0.50f, 0.22f }, 0.74f);
                    break;
                case "5-4-1":
                    spots = Spots(0.08f, new[] { 0.86f, 0.68f, 0.50f, 0.32f, 0.14f }, 0.24f, new[] { 0.78f, 0.58f, 0.42f, 0.22f }, 0.5f, new[] { 0.50f }, 0.78f);
                    break;
                default:
                    spots = Spots(0.08f, new[] { 0.82f, 0.62f, 0.38f, 0.18f }, 0.26f, new[] { 0.66f, 0.50f, 0.34f }, 0.48f, new[] { 0.82f, 0.50f, 0.18f }, 0.74f);
                    break;
            }

            var p = spots[Mathf.Clamp(index, 0, spots.Length - 1)];
            return home ? p : new Vector2(1f - p.x, 1f - p.y);
        }

        static Vector2[] Spots(float gkY, float[] line1, float y1, float[] line2, float y2, float[] line3, float y3, float[] line4 = null, float y4 = 0f)
        {
            var list = new System.Collections.Generic.List<Vector2> { new Vector2(0.5f, gkY) };
            foreach (var x in line1) list.Add(new Vector2(x, y1));
            foreach (var x in line2) list.Add(new Vector2(x, y2));
            foreach (var x in line3) list.Add(new Vector2(x, y3));
            if (line4 != null)
                foreach (var x in line4) list.Add(new Vector2(x, y4));
            while (list.Count < 11)
                list.Add(list[list.Count - 1]);
            return list.ToArray();
        }

        static float PressAmount(TacticalIdentity tactics)
        {
            var pressing = tactics != null ? tactics.Pressing : "";
            var line = tactics != null ? tactics.DefensiveLine : "";
            if (pressing != null && pressing.Contains("muito") && pressing.Contains("alta"))
                return 0.34f;
            if (line != null && line.Contains("muito") && line.Contains("alta"))
                return 0.32f;
            if (pressing != null && pressing.Contains("alta"))
                return 0.22f;
            if (line != null && line.Contains("alta"))
                return 0.26f;
            if (pressing != null && pressing.Contains("muito") && pressing.Contains("baixa"))
                return 0.04f;
            if (pressing != null && pressing.Contains("baixa"))
                return 0.08f;
            return 0.14f;
        }

        bool TryIntercept(float dt)
        {
            if (ActionKind == "shot" || ActionKind == "goal" || ActionKind == "kickoff" || ActionKind == "solo")
                return false;
            if (BallHeight > 0.38f)
                return false;
            if (Vector2.Distance(Ball, BallTarget) < 0.04f)
                return false;
            var attackHome = _match.HomeHasBall;
            var count = attackHome ? _match.AwayOnField.Count : _match.HomeOnField.Count;
            var best = 99f;
            var who = -1;
            for (var i = 1; i < count && i < 11; i++)
            {
                var defending = attackHome ? _match.AwayOnField : _match.HomeOnField;
                if (IsPlayerInjured(defending[i]))
                    continue;
                var distance = Vector2.Distance(Ball, PlayerSpot(!attackHome, i));
                if (distance < best)
                {
                    best = distance;
                    who = i;
                }
            }

            var reach = Travel > 0.62f ? 0.07f : 0.04f;
            if (who < 0 || best > reach)
                return false;
            var chance = (Travel > 0.62f ? 1.6f : 0.35f) * dt;
            var attackSquadEarly = attackHome ? _match.HomeOnField : _match.AwayOnField;
            if (_match.BallHolder >= 0 && _match.BallHolder < attackSquadEarly.Count && _match.People.TryGetValue(attackSquadEarly[_match.BallHolder], out var passer))
            {
                if (passer.HasTrait(PlayerTrait.Visionario))
                    chance *= 0.62f;
                if (passer.HasTrait(PlayerTrait.ImuneAPressao))
                    chance *= 0.78f;
            }
            var defendSquad = attackHome ? _match.AwayOnField : _match.HomeOnField;
            if (who < defendSquad.Count && _match.People.TryGetValue(defendSquad[who], out var cutter))
            {
                if (cutter.HasTrait(PlayerTrait.Marcador) || cutter.HasTrait(PlayerTrait.Trabalhador))
                    chance *= 1.35f;
            }
            if (_rng.NextDouble() > chance)
                return false;
            ResolvePendingPass(false, true, null);
            Turnover(attackHome, who);
            _match.LastEvent = "Interceção. " + Name((attackHome ? _match.AwayOnField : _match.HomeOnField)[who]) + " corta o passe.";
            var attackSquad = attackHome ? _match.HomeOnField : _match.AwayOnField;
            if (_match.BallHolder >= 0 && _match.BallHolder < attackSquad.Count)
                Bump(attackSquad[_match.BallHolder], -0.08f);
            Bump((attackHome ? _match.AwayOnField : _match.HomeOnField)[who], 0.1f);
            return true;
        }

        bool Contest(float dt)
        {
            ActionTime -= dt;
            if (ActionTime > 0f || BallHeight > 0.32f)
                return false;
            var attackHome = _match.HomeHasBall;
            var count = attackHome ? _match.AwayOnField.Count : _match.HomeOnField.Count;
            var best = 99f;
            var who = 1;
            for (var i = 0; i < count && i < 11; i++)
            {
                var defending = attackHome ? _match.AwayOnField : _match.HomeOnField;
                if (IsPlayerInjured(defending[i]))
                    continue;
                var distance = Vector2.Distance(Ball, PlayerSpot(!attackHome, i));
                if (distance < best)
                {
                    best = distance;
                    who = i;
                }
            }

            if (best > 0.11f)
                return false;
            ActionTime = 0.55f;
            var press = PressAmount(attackHome ? _match.AwayTactics : _match.HomeTactics);
            var resist = 0f;
            var attackSquad = attackHome ? _match.HomeOnField : _match.AwayOnField;
            var holder = Mathf.Clamp(_match.BallHolder, 0, Mathf.Max(0, attackSquad.Count - 1));
            if (attackSquad.Count > 0 && _match.People.TryGetValue(attackSquad[holder], out var pressed))
            {
                if (pressed.HasTrait(PlayerTrait.ImuneAPressao))
                    resist += 0.2f;
                if (pressed.Attributes != null && PlayerAttributes.CurrentValue(pressed, "Frieza") >= 15)
                    resist += 0.06f;
                if (pressed.Attributes != null && PlayerAttributes.CurrentValue(pressed, "Frieza") <= 8)
                    resist -= 0.08f;
            }
            if (who >= 0 && who < (attackHome ? _match.AwayOnField.Count : _match.HomeOnField.Count))
            {
                var markerId = (attackHome ? _match.AwayOnField : _match.HomeOnField)[who];
                if (_match.People.TryGetValue(markerId, out var marker))
                {
                    if (marker.HasTrait(PlayerTrait.Marcador))
                        resist -= 0.08f;
                    if (marker.HasTrait(PlayerTrait.Trabalhador))
                        resist -= 0.05f;
                }
            }
            if (_rng.NextDouble() > 0.22f + press - resist)
                return false;
            var squad = attackHome ? _match.AwayOnField : _match.HomeOnField;
            if (who >= 0 && who < squad.Count && _match.People.TryGetValue(squad[who], out var tackler))
            {
                if (RegisterCollisionInjury(tackler, squad[who]))
                    return true;
            }

            var carrierSquad = attackHome ? _match.HomeOnField : _match.AwayOnField;
            if (holder >= 0 && holder < carrierSquad.Count && _match.People.TryGetValue(carrierSquad[holder], out var carrier)
                && RegisterCollisionInjury(carrier, carrierSquad[holder]))
                return true;

            Turnover(attackHome, who);
            return true;
        }

        bool RegisterCollisionInjury(Player player, string playerId)
        {
            if (player == null || player.Injury == null || player.Injury.IsInjured)
                return false;
            var prone = player.HasTrait(PlayerTrait.PropensoALesoes);
            if (player.Fatigue <= 82 && !prone)
                return false;
            var chance = (0.055f + Mathf.Max(0, player.Fatigue - 80) * 0.004f) * _injuryMultiplier;
            if (prone)
                chance *= 2.1f;
            if (player.HasTrait(PlayerTrait.ResistenteALesoes))
                chance *= 0.4f;
            if (_rng.NextDouble() >= Mathf.Clamp(chance, 0.015f, 0.24f))
                return false;
            player.Injury.IsInjured = true;
            player.Injury.Description = "Lesão muscular";
            player.Injury.DaysRemaining = 12;
            _match.AddNote(player.FullName + " fica no chão após o choque. Lesão muscular.", "injury", playerId);
            Announce("LESÃO", playerId);
            Hold = 1.4f;
            ActionKind = "injury";
            TryReplaceInjuredPlayer(playerId);
            return true;
        }

        void TryReplaceInjuredPlayer(string playerId)
        {
            for (var side = 0; side < 2; side++)
            {
                var home = side == 0;
                var field = home ? _match.HomeOnField : _match.AwayOnField;
                var bench = home ? _match.HomeBench : _match.AwayBench;
                if ((home ? _match.HomeSubsLeft : _match.AwaySubsLeft) <= 0)
                    continue;
                var fieldIndex = field.IndexOf(playerId);
                if (fieldIndex < 0)
                    continue;
                _match.People.TryGetValue(playerId, out var injured);
                var bestIndex = -1;
                var bestScore = float.MinValue;
                for (var i = 0; i < bench.Count; i++)
                {
                    if (!_match.People.TryGetValue(bench[i], out var candidate) || candidate == null
                        || candidate.Injury == null || candidate.Injury.IsInjured)
                        continue;
                    var score = (float)PlayerAttributes.Overall(candidate);
                    if (injured != null && candidate.Position == injured.Position)
                        score += 20f;
                    else if (injured != null && ((candidate.Position == PlayerPosition.LeftBack || candidate.Position == PlayerPosition.RightBack)
                        == (injured.Position == PlayerPosition.LeftBack || injured.Position == PlayerPosition.RightBack)))
                        score += 5f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }
                if (bestIndex >= 0)
                    TrySub(home, fieldIndex, bestIndex);
                return;
            }
        }

        bool IsPlayerInjured(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            if (_match.SentOffPlayers.Contains(id)) return true;
            return _match.People.TryGetValue(id, out var player) && player != null
                && player.Injury != null && player.Injury.IsInjured;
        }

        void Drain(float dt)
        {
            var squad = _match.HomeHasBall ? _match.HomeOnField : _match.AwayOnField;
            var index = Mathf.Clamp(_match.BallHolder, 0, Mathf.Max(0, squad.Count - 1));
            if (squad.Count > 0)
                Tire(squad[index], dt * 1.4f);
            var other = _match.HomeHasBall ? _match.AwayOnField : _match.HomeOnField;
            var best = 99f;
            string closest = null;
            for (var i = 0; i < other.Count && i < 11; i++)
            {
                var distance = Vector2.Distance(Ball, PlayerSpot(!_match.HomeHasBall, i));
                if (distance < best)
                {
                    best = distance;
                    closest = other[i];
                }
            }

            if (closest != null)
                Tire(closest, dt * 1.1f);
            var defending = !_match.HomeHasBall;
            var press = PressAmount(defending ? _match.HomeTactics : _match.AwayTactics);
            if (press > 0.2f)
            {
                var extra = press > 0.3f ? 3 : 2;
                for (var i = 1; i < other.Count && i < extra + 1; i++)
                    Tire(other[i], dt * press * 0.85f);
            }
        }

        void Tire(string id, float amount)
        {
            if (string.IsNullOrEmpty(id) || !_match.People.TryGetValue(id, out var player))
                return;
            if (!_load.ContainsKey(id))
                _load[id] = 0f;
            if (player.Attributes != null)
            {
                if (PlayerAttributes.CurrentValue(player, "Resistência") >= 16 || player.HasTrait(PlayerTrait.Trabalhador))
                    amount *= 0.72f;
                else if (PlayerAttributes.CurrentValue(player, "Resistência") <= 8)
                    amount *= 1.28f;
            }
            _load[id] += amount;
            while (_load[id] >= 1f)
            {
                _load[id] -= 1f;
                player.Fatigue++;
            }
            if (player.Fatigue > 99)
                player.Fatigue = 99;
            if (player.Fatigue < 1)
                player.Fatigue = 1;
        }

        static Vector2 RawSlot(int index, string formation)
        {
            return Slot(true, index, formation);
        }

        static double AttackStrength(TacticalIdentity tactics)
        {
            if (tactics.BuildUp != null && tactics.BuildUp.Contains("curta")) return 0.8;
            if (tactics.Pressing != null && tactics.Pressing.Contains("alta")) return 0.7;
            return 0.55;
        }

        static bool NamedIdea(TacticalIdentity tactics)
        {
            return tactics != null && !string.IsNullOrEmpty(tactics.AttackIdea) && tactics.AttackIdea.IndexOf("Equilibr", StringComparison.OrdinalIgnoreCase) < 0;
        }

        static bool CrossPlan(TacticalIdentity tactics)
        {
            if (tactics == null)
                return false;
            if (NamedIdea(tactics))
                return tactics.AttackIdea.IndexOf("Cruz", StringComparison.OrdinalIgnoreCase) >= 0;
            return tactics.Width != null && tactics.Width.Contains("larga");
        }

        static bool TikiPlan(TacticalIdentity tactics)
        {
            if (tactics == null)
                return false;
            if (NamedIdea(tactics))
                return tactics.AttackIdea.IndexOf("Tiki", StringComparison.OrdinalIgnoreCase) >= 0
                    || tactics.AttackIdea.IndexOf("Posse", StringComparison.OrdinalIgnoreCase) >= 0;
            return tactics.BuildUp != null && tactics.BuildUp.Contains("curta") && tactics.PassingStyle != null && tactics.PassingStyle.Contains("seguro");
        }

        static bool DirectPlan(TacticalIdentity tactics)
        {
            if (tactics == null)
                return false;
            if (NamedIdea(tactics))
                return tactics.AttackIdea.IndexOf("direto", StringComparison.OrdinalIgnoreCase) >= 0
                    || tactics.AttackIdea.IndexOf("Contra", StringComparison.OrdinalIgnoreCase) >= 0
                    || tactics.AttackIdea.IndexOf("longas", StringComparison.OrdinalIgnoreCase) >= 0;
            return tactics.BuildUp != null && tactics.BuildUp.Contains("longa");
        }

        Vector2 MissWide(bool home)
        {
            var left = _rng.NextDouble() < 0.5;
            var wide = new Vector2(left ? 0.08f : 0.92f, home ? 1.08f : -0.08f);
            if (!ThroughGoal(Ball, wide, home))
                return wide;
            return new Vector2(left ? -0.08f : 1.08f, home ? Mathf.Clamp(Ball.y, 0.62f, 0.86f) : Mathf.Clamp(Ball.y, 0.14f, 0.38f));
        }

        static bool ThroughGoal(Vector2 from, Vector2 to, bool home)
        {
            var line = home ? 0.97f : 0.03f;
            var span = to.y - from.y;
            if (Mathf.Abs(span) < 0.0001f)
                return false;
            var t = (line - from.y) / span;
            if (t <= 0f || t >= 1f)
                return false;
            var x = Mathf.Lerp(from.x, to.x, t);
            return x > 0.34f && x < 0.66f;
        }

        static double DefenseStrength(TacticalIdentity tactics)
        {
            if (tactics.DefensiveLine != null && tactics.DefensiveLine.Contains("baixa")) return 0.8;
            if (tactics.Pressing != null && tactics.Pressing.Contains("média")) return 0.6;
            return 0.5;
        }

        int PickSetPieceTaker(bool home, int minIndex = 4)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (squad.Count == 0)
                return 0;

            var start = Mathf.Clamp(minIndex, 0, squad.Count - 1);
            if (minIndex >= 8)
            {
                var club = home ? _match.Home : _match.Away;
                var selectedId = club == null ? null : club.PenaltyTakerId;
                var selectedIndex = string.IsNullOrEmpty(selectedId) ? -1 : squad.IndexOf(selectedId);
                if (selectedIndex >= 0)
                    return selectedIndex;

                var bestIndex = 0;
                var bestScore = float.MinValue;
                for (var i = 0; i < squad.Count; i++)
                {
                    Player candidate;
                    var score = 10f;
                    if (_match.People.TryGetValue(squad[i], out candidate) && candidate.Attributes != null)
                    {
                        score = PlayerAttributes.CurrentValue(candidate, "Finalização") * 0.7f + PlayerAttributes.CurrentValue(candidate, "Frieza") * 0.3f;
                        if (candidate.HasTrait(PlayerTrait.Finalizador))
                            score += 0.4f;
                        if (candidate.HasTrait(PlayerTrait.Frieza))
                            score += 0.6f;
                    }
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }
                return bestIndex;
            }

            if (minIndex == 5)
            {
                var club = home ? _match.Home : _match.Away;
                var selectedId = club == null ? null : club.FreeKickTakerId;
                var selectedIndex = string.IsNullOrEmpty(selectedId) ? -1 : squad.IndexOf(selectedId);
                if (selectedIndex >= 0)
                    return selectedIndex;
                var bestFreeKick = Mathf.Clamp(start, 0, squad.Count - 1);
                var bestFreeKickScore = float.MinValue;
                for (var i = 1; i < squad.Count; i++)
                {
                    var candidate = Person(home, i);
                    if (candidate == null || candidate.Attributes == null)
                        continue;
                    var score = PlayerAttributes.CurrentValue(candidate, "Passe") * 0.28f + PlayerAttributes.CurrentValue(candidate, "Finalização") * 0.3f + PlayerAttributes.CurrentValue(candidate, "Frieza") * 0.22f;
                    if (candidate.HasTrait(PlayerTrait.LivrePotente)) score += 4f;
                    if (candidate.HasTrait(PlayerTrait.RemateDeLonge) || candidate.HasTrait(PlayerTrait.RematadorDeLonge)) score += 1.1f;
                    if (score <= bestFreeKickScore)
                        continue;
                    bestFreeKickScore = score;
                    bestFreeKick = i;
                }
                return bestFreeKick;
            }

            var end = Mathf.Min(squad.Count, Mathf.Max(start + 1, 9));
            return _rng.Next(start, end);
        }

        void UpdateFlight()
        {
            if (_flightApex <= 0.08f || _flightSpan < 0.04f)
            {
                BallHeight = 0f;
                return;
            }

            var shown = _drawnReady ? _drawn : Ball;
            var along = Mathf.Clamp01(Vector2.Distance(_flightFrom, shown) / _flightSpan);
            var rise = Mathf.Sin(along * Mathf.PI);
            var full = Mathf.Clamp01((_flightApex - 0.08f) / 0.28f);
            BallHeight = rise * full;
        }

        void Launch(float apex)
        {
            if (ActionKind != "shot" && ActionKind != "goal" && ActionKind != "pass")
                _curl = 0f;
            _flightFrom = Ball;
            _flightSpan = Vector2.Distance(Ball, BallTarget);
            _flightApex = _flightSpan < 0.03f ? 0f : Mathf.Clamp01(apex);
            if (_flightApex <= 0f)
                BallHeight = 0f;
        }

        void SnapBallVisual()
        {
            _drawn = Ball;
            _drawnReady = true;
            _ballVelocity = Vector2.zero;
            _curl = 0f;
            _flightFrom = Ball;
            _flightSpan = 0f;
            _flightApex = 0f;
            BallHeight = 0f;
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        void Diagnose(string entity, string issue, Vector2 previous, Vector2 current, float dt)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Time.unscaledTime < _nextDiagnosticTime)
                return;
            _nextDiagnosticTime = Time.unscaledTime + 1f;
            var speed = dt > 0f && IsFinite(dt) ? Vector2.Distance(previous, current) / dt : 0f;
            var fixture = _match != null && _match.Fixture != null ? _match.Fixture.Id : "desconhecido";
            Debug.LogWarning("[MatchEngine] " + issue + " | jogo " + fixture + " | minuto "
                + _match.Minute.ToString("0.0") + " | " + entity + " | anterior " + previous
                + " | atual/alvo " + current + " | alvo da bola " + BallTarget
                + " | velocidade normalizada/s " + speed.ToString("0.00"));
#endif
        }

        void BeginSetPiece(bool home, string kind, Vector2 spot, int taker)
        {
            _lastPasser = null;
            _settle = 0f;
            _finishNext = false;
            _headerNext = false;
            _throughBall = false;
            _intended = -1;
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            taker = Mathf.Clamp(taker, 0, Mathf.Max(0, squad.Count - 1));
            _pieceHome = home;
            _piecePenalty = false;
            _pieceKind = kind;
            _pieceSpot = spot;
            _pieceTaker = taker;
            _wall = new int[0];
            Ball = spot;
            BallTarget = spot;
            SnapBallVisual();
            Travel = 0.05f;
            _pieceLive = true;
            _match.HomeHasBall = home;
            _match.BallHolder = taker;
            ActionKind = kind;
            var run = Vector2.Distance(FormationSpot(home, taker), spot);
            Hold = Mathf.Clamp(1.25f + run / 0.24f, 1.7f, 4.8f);
        }

        Vector2 CornerMark(bool home, int index)
        {
            if (index == 0)
                return Goalkeeper(home);
            var attacking = home == _pieceHome;
            if (attacking)
            {
                var x = 0.3f + (index % 5) * 0.1f;
                var y = _pieceHome ? 0.74f + (index % 3) * 0.045f : 0.26f - (index % 3) * 0.045f;
                return new Vector2(Mathf.Clamp(x, 0.28f, 0.74f), Mathf.Clamp(y, 0.12f, 0.9f));
            }

            var line = _pieceHome ? 0.9f : 0.1f;
            return new Vector2(Mathf.Clamp(0.28f + (index % 6) * 0.09f, 0.24f, 0.76f), line);
        }

        Vector2 ThrowMark(bool home, int index)
        {
            if (home == _pieceHome && index == _pieceTaker)
                return _pieceSpot;
            var spot = FormationSpot(home, index);
            if (Vector2.Distance(spot, _pieceSpot) > 0.36f)
                return spot;
            var inward = _pieceSpot.x < 0.5f ? 0.1f : -0.1f;
            var gather = _pieceSpot + new Vector2(inward, spot.y >= _pieceSpot.y ? 0.06f : -0.06f);
            return Vector2.Lerp(spot, new Vector2(Mathf.Clamp(gather.x, 0.08f, 0.92f), Mathf.Clamp(gather.y, 0.08f, 0.92f)), 0.55f);
        }

        Vector2 FormationSpot(bool home, int index)
        {
            if (index <= 0)
                return home ? new Vector2(0.5f, 0.06f) : new Vector2(0.5f, 0.94f);
            var formation = home ? _match.HomeTactics.Formation : _match.AwayTactics.Formation;
            var raw = RawSlot(index, formation);
            var spot = home ? raw : new Vector2(1f - raw.x, 1f - raw.y);
            spot.x = Mathf.Clamp(spot.x, 0.05f, 0.95f);
            spot.y = Mathf.Clamp(spot.y, 0.05f, 0.95f);
            return spot;
        }

        bool TryCarryLine(bool home)
        {
            var tactics = home ? _match.HomeTactics : _match.AwayTactics;
            if (!CrossPlan(tactics))
                return false;
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var from = Mathf.Clamp(_match.BallHolder, 0, squad.Count - 1);
            if (from == 0)
                return false;
            var spot = PlayerSpot(home, from);
            var depth = home ? spot.y : 1f - spot.y;
            var wing = spot.x < 0.4f || spot.x > 0.6f;
            if (!wing || depth < 0.4f || depth > 0.76f)
                return false;
            if (spot.x < 0.16f || spot.x > 0.84f)
                return false;
            if (_rng.NextDouble() > 0.62)
                return false;
            var x = spot.x < 0.5f ? Mathf.Max(0.06f, spot.x - 0.08f) : Mathf.Min(0.94f, spot.x + 0.08f);
            var y = home ? Mathf.Min(0.88f, spot.y + 0.07f) : Mathf.Max(0.12f, spot.y - 0.07f);
            BallTarget = new Vector2(x, y);
            ActionKind = "solo";
            Hold = 0f;
            Travel = 0.3f;
            Launch(0f);
            var name = Name(squad[from]);
            _match.LastEvent = name + " corre a linha.";
            _match.AddNote(name + " ataca a linha para cruzar.", "cross");
            return true;
        }

        bool TryCross(bool home)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (squad.Count < 2)
                return false;
            var from = Mathf.Clamp(_match.BallHolder, 0, squad.Count - 1);
            if (from == 0)
                return false;
            var spot = PlayerSpot(home, from);
            var depth = home ? spot.y : 1f - spot.y;
            var wide = spot.x < 0.3f || spot.x > 0.7f;
            if (!wide || depth < 0.5f)
                return false;
            var crossing = CrossingOf(home, from);
            var chance = 0.28f + crossing / 50f;
            if (_match.People.TryGetValue(squad[from], out var crosser))
            {
                if (crosser.HasTrait(PlayerTrait.Cruzador))
                    chance += 0.16f;
                if (crosser.HasTrait(PlayerTrait.Driblador) && !crosser.HasTrait(PlayerTrait.Cruzador))
                    chance -= 0.1f;
            }
            if (depth > 0.74f)
                chance += 0.16f;
            var crossTactics = home ? _match.HomeTactics : _match.AwayTactics;
            if (CrossPlan(crossTactics))
                chance += 0.45f;
            else if (TikiPlan(crossTactics))
                chance -= 0.24f;
            else if (crossTactics.Width != null && crossTactics.Width.Contains("larga"))
                chance += 0.1f;
            if (_rng.NextDouble() > chance)
                return false;
            var target = PickCrossTarget(home, from);
            if (target < 0)
                return false;
            if (Offside(home, target))
            {
                FlagOffside(home, target);
                return true;
            }

            DeliverCross(home, from, target, crossing, false);
            return true;
        }

        int PickCrossTarget(bool home, int from)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var best = -1;
            var bestScore = 0.35f;
            for (var i = 1; i < squad.Count && i < 11; i++)
            {
                if (i == from || IsPlayerInjured(squad[i]))
                    continue;
                var mark = PlayerSpot(home, i);
                var depth = home ? mark.y : 1f - mark.y;
                var central = 1f - Mathf.Abs(mark.x - 0.5f) * 2f;
                var score = depth * 1.6f + central * 0.45f + (float)_rng.NextDouble() * 0.12f;
                if (depth < 0.6f)
                    score -= 1.2f;
                if (score <= bestScore)
                    continue;
                bestScore = score;
                best = i;
            }

            return best;
        }

        void DeliverCross(bool home, int from, int target, int crossing, bool corner)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (squad.Count == 0)
                return;
            from = Mathf.Clamp(from, 0, squad.Count - 1);
            var fromName = Name(squad[from]);
            if (target < 0 || target >= squad.Count)
                target = PickCrossTarget(home, from);
            var quality = Mathf.Clamp01(crossing / 20f);
            if (_match.People.TryGetValue(squad[from], out var crosser) && crosser.HasTrait(PlayerTrait.Cruzador))
                quality = Mathf.Clamp01(quality + 0.18f);
            var miss = _rng.NextDouble() > 0.78 + quality * 0.16;
            _lastPasser = null;
            if (target < 0 || miss)
            {
                if (_rng.NextDouble() < 0.25)
                {
                    var over = _rng.NextDouble() < 0.5;
                    BallTarget = over
                        ? MissWide(home)
                        : new Vector2((float)_rng.NextDouble() < 0.5f ? 0.04f : 0.96f, home ? 0.9f : 0.1f);
                    _match.LastEvent = (corner ? "Canto de " : "Cruzamento de ") + fromName + " sai pela linha.";
                }
                else
                {
                    BallTarget = new Vector2(Mathf.Lerp(0.5f, Ball.x, 0.25f) + (float)(_rng.NextDouble() - 0.5f) * 0.08f, home ? 0.8f : 0.2f);
                    BallTarget = new Vector2(Mathf.Clamp(BallTarget.x, 0.32f, 0.68f), Mathf.Clamp(BallTarget.y, 0.12f, 0.88f));
                    _match.LastEvent = (corner ? "Canto de " : "Cruzamento de ") + fromName + " não encontra ninguém.";
                }

                _match.BallHolder = from;
                _match.HomeHasBall = home;
                Travel = 0.7f;
                Launch(0.6f);
                ActionKind = "cross";
                Bump(squad[from], -0.03f);
                return;
            }

            _lastPasser = squad[from];
            _lastPasserHome = home;
            var aim = PlayerSpot(home, target);
            if (corner)
                aim = new Vector2(Mathf.Lerp(0.5f, aim.x, 0.25f), home ? 0.82f : 0.18f);
            var boxY = home ? Mathf.Lerp(aim.y, 0.84f, 0.7f) : Mathf.Lerp(aim.y, 0.16f, 0.7f);
            var boxX = Mathf.Lerp(aim.x, 0.5f, 0.4f);
            var error = (1f - quality) * 0.08f;
            BallTarget = new Vector2(
                Mathf.Clamp(boxX + (float)(_rng.NextDouble() - 0.5) * error, 0.3f, 0.7f),
                Mathf.Clamp(boxY, 0.08f, 0.92f));
            ActionKind = "cross";
            if (TryKeeperClaim(home, target))
                return;
            _match.BallHolder = target;
            _match.HomeHasBall = home;
            Travel = 0.74f;
            Launch(1f);
            var toName = Name(squad[target]);
            _match.LastEvent = (corner ? "Canto de " : "Cruzamento de ") + fromName + " para " + toName + ".";
            Bump(squad[from], 0.07f);
            Bump(squad[target], 0.04f);
            if (_match.People.TryGetValue(squad[target], out var finisher))
            {
                var aerial = finisher.HasTrait(PlayerTrait.Cabeceador) || finisher.HasTrait(PlayerTrait.Finalizador);
                var finish = finisher.Attributes != null && PlayerAttributes.CurrentValue(finisher, "Finalização") >= 14;
                var inBox = home ? BallTarget.y > 0.72f : BallTarget.y < 0.28f;
                var attackAir = finisher.Attributes == null ? 10 : PlayerAttributes.CurrentValue(finisher, "Força") + PlayerAttributes.CurrentValue(finisher, "Posicionamento") / 4;
                if (finisher.HeightCm > 0)
                    attackAir += Mathf.Clamp(Mathf.RoundToInt((finisher.HeightCm - 180f) * 0.18f), -3, 4);
                if (finisher.HasTrait(PlayerTrait.Cabeceador))
                    attackAir += 4;
                var defendAir = AerialOf(!home, BallTarget);
                var win = _rng.NextDouble() < attackAir / (float)(attackAir + defendAir + 1);
                if (!win)
                {
                    _match.LastEvent = (corner ? "Canto de " : "Cruzamento de ") + fromName + " é afastado.";
                    _match.HomeHasBall = !home;
                    _match.BallHolder = 4;
                    ActionKind = "cross";
                    return;
                }

                if (inBox && (aerial || finish || _rng.NextDouble() < 0.42))
                {
                    _finishNext = true;
                    _finishHome = home;
                    _headerNext = aerial || _rng.NextDouble() < 0.65;
                }
            }
        }

        bool TryKeeperClaim(bool attackingHome, int receiverIndex)
        {
            var defendingHome = !attackingHome;
            var inBox = defendingHome
                ? BallTarget.y > 0.02f && BallTarget.y < 0.18f
                : BallTarget.y > 0.82f && BallTarget.y < 0.98f;
            if (!inBox || BallTarget.x < 0.18f || BallTarget.x > 0.82f)
                return false;
            var keeper = Person(defendingHome, 0);
            if (keeper == null || keeper.Attributes == null)
                return false;
            var rush = keeper.HasTrait(PlayerTrait.GuardaRedesSaiDaBaliza);
            var attacksCrosses = keeper.HasTrait(PlayerTrait.GuardaRedesSaiAosCruzamentos);
            var keeperDistance = Vector2.Distance(Goalkeeper(defendingHome), BallTarget);
            var receiverDistance = Vector2.Distance(PlayerSpot(attackingHome, receiverIndex), BallTarget);
            var reach = 0.15f + PlayerAttributes.CurrentValue(keeper, "Manejo") * 0.004f;
            if (rush) reach += 0.04f;
            if (attacksCrosses) reach += 0.025f;
            var lead = rush ? 0.075f : attacksCrosses ? 0.035f : 0.012f;
            if (keeperDistance > reach || keeperDistance + lead >= receiverDistance)
                return false;
            var attempt = rush ? 0.92f : attacksCrosses ? 0.82f : 0.55f;
            if (_rng.NextDouble() > attempt)
                return false;
            var claim = 0.53f + (PlayerAttributes.CurrentValue(keeper, "Manejo") - 10) * 0.018f + (PlayerAttributes.CurrentValue(keeper, "Decisão") - 10) * 0.012f;
            if (rush) claim += 0.035f;
            if (attacksCrosses) claim += 0.025f;
            if (_rng.NextDouble() > Mathf.Clamp(claim, 0.32f, 0.84f))
                return false;

            _match.BallHolder = 0;
            _match.HomeHasBall = defendingHome;
            _match.LastEvent = Name(keeper.Id) + " sai da baliza e agarra o cruzamento.";
            _match.AddNote(_match.LastEvent, "save");
            Travel = 0.74f;
            Launch(0.82f);
            Bump(keeper.Id, 0.12f);
            return true;
        }

        Player Person(bool home, int index)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (index < 0 || index >= squad.Count)
                return null;
            _match.People.TryGetValue(squad[index], out var player);
            return player;
        }

        void DeliverThrow(bool home, int from, Vector2 fromSpot)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (squad.Count == 0)
                return;
            from = Mathf.Clamp(from, 0, squad.Count - 1);
            var best = -1;
            var bestScore = -99f;
            var thrower = Person(home, from);
            var maxRange = thrower != null && thrower.HasTrait(PlayerTrait.LancamentoLongo) ? 0.62f : 0.46f;
            var attackingDepth = home ? fromSpot.y : 1f - fromSpot.y;
            for (var i = 1; i < squad.Count && i < 11; i++)
            {
                if (i == from)
                    continue;
                var mark = FormationSpot(home, i);
                var distance = Vector2.Distance(fromSpot, mark);
                if (distance < 0.05f || distance > maxRange)
                    continue;
                var forward = home ? mark.y - fromSpot.y : fromSpot.y - mark.y;
                var score = forward * 0.55f - distance + (float)_rng.NextDouble() * 0.1f;
                var targetDepth = home ? mark.y : 1f - mark.y;
                if (thrower != null && thrower.HasTrait(PlayerTrait.LancamentoLongo) && attackingDepth > 0.62f && targetDepth > 0.72f)
                {
                    score += 0.75f;
                    if (_match.People.TryGetValue(squad[i], out var aerialTarget))
                    {
                        if (aerialTarget.Position == PlayerPosition.Striker || aerialTarget.HasTrait(PlayerTrait.Cabeceador))
                            score += 0.5f;
                        if (aerialTarget.Attributes != null)
                            score += PlayerAttributes.CurrentValue(aerialTarget, "Força") * 0.025f;
                    }
                }
                if (score <= bestScore)
                    continue;
                bestScore = score;
                best = i;
            }

            var fromName = Name(squad[from]);
            if (best < 0)
            {
                // A player cannot be penalised for being offside directly from a throw-in.
                Pass(home, true);
                _match.LastEvent = "Lançamento de " + fromName + ".";
                return;
            }

            _match.BallHolder = best;
            _match.HomeHasBall = home;
            BallTarget = FormationSpot(home, best);
            var hop = Vector2.Distance(Ball, BallTarget);
            Travel = Mathf.Lerp(0.3f, 0.62f, Mathf.Clamp01(hop / 0.4f));
            var longThrow = thrower != null && thrower.HasTrait(PlayerTrait.LancamentoLongo);
            Launch(hop > 0.2f ? longThrow ? 0.82f : 0.58f : 0.22f);
            ActionKind = hop > 0.2f ? "cross" : "pass";
            _match.LastEvent = (hop > (longThrow ? 0.46f : 0.2f) ? "Lançamento longo de " : "Lançamento de ") + fromName + " para " + Name(squad[best]) + ".";
            Bump(squad[from], 0.03f);
        }

        int AerialOf(bool defendingHome, Vector2 spot)
        {
            var squad = defendingHome ? _match.HomeOnField : _match.AwayOnField;
            var best = 99f;
            var score = 11;
            for (var i = 1; i < squad.Count && i < 11; i++)
            {
                var distance = Vector2.Distance(spot, LiveSpot(defendingHome, i));
                if (distance >= best)
                    continue;
                best = distance;
                if (_match.People.TryGetValue(squad[i], out var player) && player.Attributes != null)
                {
                    score = PlayerAttributes.CurrentValue(player, "Força") + PlayerAttributes.CurrentValue(player, "Posicionamento") / 5;
                    if (player.HeightCm > 0)
                        score += Mathf.Clamp(Mathf.RoundToInt((player.HeightCm - 180f) * 0.18f), -3, 4);
                    if (player.HasTrait(PlayerTrait.Marcador) || player.HasTrait(PlayerTrait.Cabeceador))
                        score += 3;
                }
            }

            return score;
        }

        int CrossingOf(bool home, int index)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (index < 0 || index >= squad.Count)
                return 10;
            if (_match.People.TryGetValue(squad[index], out var player) && player.Attributes != null)
                return PlayerAttributes.CurrentValue(player, "Cruzamento");
            return 10;
        }

        int PickRoleTaker(bool home, string preferredId)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (squad.Count == 0)
                return 0;
            if (!string.IsNullOrEmpty(preferredId))
            {
                var chosen = squad.IndexOf(preferredId);
                if (chosen > 0)
                    return chosen;
            }

            var best = 1;
            var mark = -1;
            for (var i = 1; i < squad.Count && i < 11; i++)
            {
                var score = CrossingOf(home, i);
                if (score <= mark)
                    continue;
                mark = score;
                best = i;
            }

            return Mathf.Clamp(best, 0, squad.Count - 1);
        }

        int NearestOutfield(bool home, Vector2 spot)
        {
            var count = home ? _match.HomeOnField.Count : _match.AwayOnField.Count;
            if (count <= 1)
                return 0;
            var best = 1;
            var bestDistance = 99f;
            for (var i = 1; i < count && i < 11; i++)
            {
                var distance = Vector2.Distance(spot, FormationSpot(home, i));
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = i;
            }

            return best;
        }

        int ThrowTaker(bool home, Vector2 spot)
        {
            var nearest = NearestOutfield(home, spot);
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            if (nearest < 0 || nearest >= squad.Count)
                return nearest;
            var nearestDistance = Vector2.Distance(spot, FormationSpot(home, nearest));
            var selected = nearest;
            var bestScore = float.MinValue;
            for (var i = 1; i < squad.Count && i < 11; i++)
            {
                if (!_match.People.TryGetValue(squad[i], out var player) || !player.HasTrait(PlayerTrait.LancamentoLongo))
                    continue;
                var distance = Vector2.Distance(spot, FormationSpot(home, i));
                if (distance > nearestDistance + 0.16f)
                    continue;
                var score = 1f - distance + (player.Attributes == null ? 0f : PlayerAttributes.CurrentValue(player, "Força") * 0.025f);
                if (score > bestScore)
                {
                    bestScore = score;
                    selected = i;
                }
            }
            return selected;
        }

        int CurrentRoleOverall(Player player)
        {
            if (player == null)
                return 50;
            var index = _match.HomeOnField.IndexOf(player.Id);
            if (index >= 0)
            {
                var slots = StartingShape.SlotPositions(_match.HomeTactics == null ? _match.Home.Tactics.Formation : _match.HomeTactics.Formation);
                return index < slots.Length ? PlayerAttributes.CurrentOverallAtPosition(player, slots[index]) : PlayerAttributes.CurrentOverall(player);
            }

            index = _match.AwayOnField.IndexOf(player.Id);
            if (index >= 0)
            {
                var slots = StartingShape.SlotPositions(_match.AwayTactics == null ? _match.Away.Tactics.Formation : _match.AwayTactics.Formation);
                return index < slots.Length ? PlayerAttributes.CurrentOverallAtPosition(player, slots[index]) : PlayerAttributes.CurrentOverall(player);
            }
            return PlayerAttributes.CurrentOverall(player);
        }
    }
}
