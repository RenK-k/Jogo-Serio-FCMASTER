using System;
using System.Collections.Generic;
using CoachMaster.Domain;
using UnityEngine;

namespace CoachMaster.MatchPlay
{
    public sealed class MatchEngine
    {
        readonly MatchSession _match;
        readonly System.Random _rng;
        public Vector2 Ball { get; private set; } = new Vector2(0.5f, 0.5f);
        public Vector2 BallTarget { get; private set; } = new Vector2(0.5f, 0.5f);
        public float ActionTime { get; private set; }
        public string ActionKind { get; private set; } = "kickoff";
        public float Hold { get; private set; }
        public float Travel { get; private set; } = 0.34f;
        public bool DeadBall => Hold > 0f || ActionKind == "goal" || ActionKind == "corner" || ActionKind == "card" || ActionKind == "injury" || ActionKind == "kickoff" || ActionKind == "freekick" || ActionKind == "throwin" || ActionKind == "penalty" || ActionKind == "goalkick";
        readonly Dictionary<string, float> _load = new Dictionary<string, float>();
        bool _pieceLive;
        bool _pieceHome;
        bool _piecePenalty;
        float _quietCards;
        Vector2 _pieceSpot;
        int _pieceTaker;
        int[] _wall = new int[0];
        string _lastPasser;

        public MatchEngine(MatchSession match)
        {
            _match = match;
            _rng = new System.Random(match.Fixture.Id.GetHashCode());
            Ball = new Vector2(0.5f, 0.5f);
            BallTarget = PlayerSpot(true, 9);
        }

        public void Tick(float dt, float minutesPerSecond)
        {
            if (_match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime || _match.Finished)
                return;

            _match.Minute += dt * minutesPerSecond;
            Ball = Vector2.MoveTowards(Ball, BallTarget, dt * Travel);
            Drain(dt);
            if (_match.HomeHasBall)
                _match.HomePossession += dt;
            else
                _match.AwayPossession += dt;

            if (_match.Phase == MatchPhase.FirstHalf && _match.Minute >= 45f)
            {
                _match.Phase = MatchPhase.HalfTime;
                _match.AddNote("Intervalo. O plantel espera a tua conversa.", "half");
                return;
            }

            if (_match.Phase == MatchPhase.SecondHalf && _match.Minute >= 90f)
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

            if (TryIntercept(dt))
                return;

            if (Contest(dt))
                return;
            if (Vector2.Distance(Ball, BallTarget) > 0.035f)
                return;

            NextAction();
        }

        public Vector2 PlayerSpot(bool home, int index)
        {
            if (_pieceLive && home == _pieceHome && index == _pieceTaker)
                return _pieceSpot;
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

            if (index == 0)
                return Goalkeeper(home);

            var formation = home ? _match.HomeTactics.Formation : _match.AwayTactics.Formation;
            var raw = RawSlot(index, formation);
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var attacking = home == _match.HomeHasBall;
            if (index >= 0 && index < squad.Count && _match.People.TryGetValue(squad[index], out var player))
            {
                raw += IndividualOrders.Offset(player.Duty, attacking, raw);
                if (attacking && index != _match.BallHolder)
                    raw += RunOffset(player.Duty, raw, index);
                if (player.Fatigue > 75)
                    raw.y -= 0.04f;
            }

            var spot = home ? raw : new Vector2(1f - raw.x, 1f - raw.y);
            if (!attacking)
            {
                var press = PressAmount(home ? _match.HomeTactics : _match.AwayTactics);
                spot = Vector2.Lerp(spot, Ball, press);
            }

            spot.x = Mathf.Clamp(spot.x, 0.05f, 0.95f);
            spot.y = Mathf.Clamp(spot.y, 0.05f, 0.95f);
            return spot;
        }

        Vector2 Goalkeeper(bool home)
        {
            var near = home ? Ball.y < 0.32f : Ball.y > 0.68f;
            if (!near)
                return home ? new Vector2(0.5f, 0.05f) : new Vector2(0.5f, 0.95f);
            var x = Mathf.Lerp(0.5f, Ball.x, 0.4f);
            return home ? new Vector2(x, 0.06f) : new Vector2(x, 0.94f);
        }

        public void StartSecondHalf()
        {
            _match.Phase = MatchPhase.SecondHalf;
            if (_match.Minute < 45f)
                _match.Minute = 45f;
            _match.HomeHasBall = false;
            Ball = new Vector2(0.5f, 0.5f);
            BallTarget = PlayerSpot(false, 9);
            _match.AddNote("Segunda parte. O jogo recomeça.", "half");
            ActionTime = 0.8f;
            ActionKind = "kickoff";
        }

        public bool TrySub(bool home, int fieldIndex, int benchIndex)
        {
            var field = home ? _match.HomeOnField : _match.AwayOnField;
            var bench = home ? _match.HomeBench : _match.AwayBench;
            var left = home ? _match.HomeSubsLeft : _match.AwaySubsLeft;
            if (left <= 0 || fieldIndex < 0 || benchIndex < 0 || fieldIndex >= field.Count || benchIndex >= bench.Count)
                return false;
            var outId = field[fieldIndex];
            field[fieldIndex] = bench[benchIndex];
            bench[benchIndex] = outId;
            if (home) _match.HomeSubsLeft--;
            else _match.AwaySubsLeft--;
            _match.AddNote($"Substituição: sai {Name(outId)}, entra {Name(field[fieldIndex])}.", "sub");
            return true;
        }

        public void ApplyTeamTalk(string talk)
        {
            _match.TeamTalk = talk;
            _match.LastEvent = talk;
        }

        void NextAction()
        {
            if (_pieceLive)
            {
                StrikePiece();
                return;
            }

            if (BallLeftPlay(_match.HomeHasBall))
                return;

            var attack = _match.HomeHasBall;
            var tactics = attack ? _match.HomeTactics : _match.AwayTactics;
            var defense = attack ? _match.AwayTactics : _match.HomeTactics;
            var roll = _rng.NextDouble();
            var shotChance = ShotChance(tactics, defense);
            var depth = attack ? Ball.y : 1f - Ball.y;
            if (depth < 0.58f)
                shotChance = 0.03;
            else if (depth < 0.74f)
                shotChance = 0.16;
            else if (depth < 0.84f)
                shotChance = 0.34;
            else
                shotChance = 0.52;
            if (roll < shotChance)
            {
                Shoot(attack);
                return;
            }

            if (BallLeftPlay(attack))
                return;
            if (Duel(attack))
                return;
            Pass(attack);
        }

        bool BallLeftPlay(bool homeTouched)
        {
            var wide = Ball.x < 0f || Ball.x > 1f;
            var over = Ball.y < 0f || Ball.y > 1f;
            if (!wide && !over)
                return false;
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
            if (_match.People.TryGetValue(defenders[who], out var defender) && defender.Attributes != null)
                tack = defender.Attributes.Tackling;
            if (_match.People.TryGetValue(attackers[carrier], out var attacker) && attacker.Attributes != null)
                keep = attacker.Attributes.Dribbling + attacker.Attributes.Strength / 4;
            var foulChance = 0.06 + Mathf.Max(0, tack - keep) * 0.008f;
            if (_match.Minute < _quietCards)
                foulChance *= 0.25f;
            var roll = _rng.NextDouble();
            if (roll < foulChance)
            {
                Foul(home, who);
                return true;
            }

            if (roll < 0.28 + (tack - keep) * 0.015)
            {
                Turnover(home);
                Bump(defenders[who], 0.08f);
                return true;
            }

            return false;
        }

        void Pass(bool home)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var from = Mathf.Clamp(_match.BallHolder, 0, squad.Count - 1);
            var fromName = Name(squad[from]);
            var next = PickPassTarget(home, squad, from);
            if (Offside(home, next))
            {
                FlagOffside(home, next);
                return;
            }
            _lastPasser = squad[from];
            var toName = Name(squad[next]);
            _match.BallHolder = next;
            _match.HomeHasBall = home;
            BallTarget = PlayerSpot(home, next);
            var hop = Vector2.Distance(Ball, BallTarget);
            var passer = 10;
            if (_match.People.TryGetValue(squad[from], out var passerPlayer) && passerPlayer.Attributes != null)
                passer = passerPlayer.Attributes.Passing;
            var miss = hop > 0.16f && _rng.NextDouble() < (hop - 0.12f) * (1.15f - passer / 20f);
            if (miss)
            {
                var side = BallTarget.x < 0.5f ? -0.04f : 1.04f;
                if (hop > 0.34f && _rng.NextDouble() < 0.45)
                    BallTarget = new Vector2(BallTarget.x, home ? 1.04f : -0.04f);
                else
                    BallTarget = new Vector2(side, Mathf.Clamp(BallTarget.y, 0.08f, 0.92f));
                Travel = Mathf.Lerp(0.35f, 0.95f, Mathf.Clamp01(hop / 0.4f));
                ActionKind = "loose";
                _match.BallHolder = from;
                _match.LastEvent = "Passe de " + fromName + " sai desviado.";
                Bump(squad[from], -0.06f);
                return;
            }
            Travel = Mathf.Lerp(0.22f, 0.9f, Mathf.Clamp01(hop / 0.45f));
            ActionKind = "pass";
            Bump(squad[from], 0.05f);
            Bump(squad[next], 0.04f);
            _match.LastEvent = fromName == toName
                ? "Passe de " + Tag(squad[from]) + " para " + Tag(squad[next]) + "."
                : "Passe de " + fromName + " para " + toName + ".";
        }

        int PickPassTarget(bool home, List<string> squad, int from)
        {
            if (squad.Count == 0)
                return 0;
            var fromSpot = PlayerSpot(home, from);
            var depth = home ? Ball.y : 1f - Ball.y;
            var best = -1;
            var bestScore = -99f;
            var safe = (home ? _match.HomeTactics : _match.AwayTactics).PassingStyle;
            for (var i = 0; i < squad.Count && i < 11; i++)
            {
                var spot = PlayerSpot(home, i);
                var forward = home ? spot.y - fromSpot.y : fromSpot.y - spot.y;
                var distance = Vector2.Distance(fromSpot, spot);
                var score = forward * 1.1f - distance * (safe != null && safe.Contains("arriscado") ? 4.2f : 7.4f) + (float)(_rng.NextDouble() * 0.08);
                if (distance > 0.2f)
                    score -= 2.4f;
                if (distance < 0.16f)
                    score += 0.8f;
                if (distance > 0.32f)
                    score -= 4.5f;
                if (distance > 0.32f && _rng.NextDouble() > 0.08)
                    score -= 8f;
                if (depth < 0.55f && forward > 0.04f && distance < 0.28f)
                    score += 0.45f;
                if (i == from)
                    continue;
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

        bool Offside(bool home, int index)
        {
            var spot = PlayerSpot(home, index);
            var ahead = home ? spot.y > Ball.y + 0.02f && spot.y > 0.52f : spot.y < Ball.y - 0.02f && spot.y < 0.48f;
            if (!ahead)
                return false;
            var opp = home ? _match.AwayOnField : _match.HomeOnField;
            var depths = new System.Collections.Generic.List<float>();
            for (var i = 0; i < opp.Count && i < 11; i++)
            {
                var mark = PlayerSpot(!home, i);
                depths.Add(home ? mark.y : 1f - mark.y);
            }

            if (depths.Count < 2)
                return false;
            depths.Sort();
            var line = depths[depths.Count - 2];
            var attacker = home ? spot.y : 1f - spot.y;
            return attacker > line + 0.01f;
        }

        void FlagOffside(bool home, int index)
        {
            var spot = PlayerSpot(home, index);
            Ball = new Vector2(Mathf.Clamp(spot.x, 0.12f, 0.88f), Mathf.Clamp(spot.y, 0.12f, 0.88f));
            BallTarget = Ball;
            _match.HomeHasBall = !home;
            _match.BallHolder = 4;
            ActionKind = "offside";
            Hold = 1.15f;
            Travel = 0.05f;
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var name = squad.Count == 0 ? "um avançado" : Name(squad[Mathf.Clamp(index, 0, squad.Count - 1)]);
            _match.LastEvent = "Fora de jogo de " + name + ".";
            _match.AddNote("Fora de jogo.", "offside");
        }

        Vector2 RunOffset(string duty, Vector2 slot, int index)
        {
            var phase = _match.Minute * 0.9f + index * 0.7f;
            var wave = Mathf.Sin(phase);
            if (duty == "Mover-se" || duty == "Chegar à área")
                return new Vector2(wave * 0.05f, 0.06f);
            if (duty == "Ficar aberto" || duty == "Cortar para dentro")
                return new Vector2(wave * 0.04f, 0.02f);
            return new Vector2(wave * 0.02f, 0.015f);
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
            var shooter = Name(shooterId);
            if (home) _match.HomeShots++;
            else _match.AwayShots++;
            var quality = AttackStrength(home ? _match.HomeTactics : _match.AwayTactics);
            var save = DefenseStrength(home ? _match.AwayTactics : _match.HomeTactics);
            var boost = home ? _match.AttackBoost : 0f;
            var wall = home ? _match.DefenseBoost : 0f;
            var goal = _rng.NextDouble() < 0.28 + quality * 0.16 - save * 0.08 + boost - wall;
            var depth = home ? Ball.y : 1f - Ball.y;
            if (depth < 0.72f && !penalty)
            {
                Pass(home);
                return;
            }
            Travel = depth > 0.86f ? 0.34f : 0.78f;
            BallTarget = home ? new Vector2(0.5f, goal ? 0.985f : 0.91f) : new Vector2(0.5f, goal ? 0.015f : 0.09f);
            ActionKind = goal ? "goal" : "shot";
            Hold = goal ? 1.8f : 0.55f;
            var onTarget = goal || _rng.NextDouble() < 0.55;
            if (onTarget)
            {
                if (home) _match.HomeChances++;
                else _match.AwayChances++;
            }

            if (goal)
            {
                if (home) _match.HomeGoals++;
                else _match.AwayGoals++;
                AddStat(_match.GoalsScored, shooterId);
                if (penalty)
                    AddStat(_match.PenaltyGoals, shooterId);
                if (!string.IsNullOrEmpty(_lastPasser) && _lastPasser != shooterId)
                {
                    AddStat(_match.Assists, _lastPasser);
                    Bump(_lastPasser, 0.45f);
                }
                Bump(shooterId, penalty ? 1.1f : 0.85f);
                _match.AddNote($"GOLO  {shooter}", "goal");
                Announce("GOLO!", shooterId);
            }
            else
            {
                Bump(shooterId, onTarget ? 0.12f : 0.04f);
                _match.LastEvent = onTarget
                    ? $"Remate de {shooter}. A bola passa perto."
                    : $"Remate de {shooter}. Defesa!";
            }

            _match.HomeHasBall = !home;
            _match.BallHolder = 0;
        }

        void Bump(string id, float delta)
        {
            var current = _match.RatingOf(id);
            _match.Ratings[id] = Mathf.Clamp(current + delta, 3.5f, 10f);
        }

        void Turnover(bool home)
        {
            _match.HomeHasBall = !home;
            var squad = home ? _match.AwayOnField : _match.HomeOnField;
            _match.BallHolder = _rng.Next(1, Mathf.Min(7, squad.Count));
            BallTarget = PlayerSpot(!home, _match.BallHolder);
            ActionKind = "turnover";
            _match.LastEvent = $"Corte. {Name(squad[_match.BallHolder])} recupera a bola.";
        }

        void Corner(bool home)
        {
            var left = Ball.x < 0.5f;
            var top = home;
            BallTarget = new Vector2(left ? 0.03f : 0.97f, top ? 0.97f : 0.03f);
            Ball = BallTarget;
            ActionKind = "corner";
            Hold = 1.1f;
            _match.HomeHasBall = home;
            Announce("CANTO");
            _match.AddNote("Canto.", "corner");
        }

        void GoalKick(bool home)
        {
            Ball = home ? new Vector2(0.5f, 0.08f) : new Vector2(0.5f, 0.92f);
            BallTarget = Ball;
            ActionKind = "goalkick";
            Hold = 1.1f;
            _match.HomeHasBall = home;
            _match.BallHolder = 0;
            _match.LastEvent = "Pontapé de baliza.";
        }

        void Foul(bool home, int defenderIndex)
        {
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
            Travel = 0.05f;
            _pieceTaker = PickSetPieceTaker(home, _piecePenalty ? 8 : 5);
            _match.BallHolder = _pieceTaker;
            var wall = _piecePenalty ? 0 : 2 + _rng.Next(0, 3);
            _wall = new int[wall];
            for (var i = 0; i < wall; i++)
                _wall[i] = Mathf.Clamp(1 + i, 0, squad.Count - 1);
            _pieceLive = true;
            ActionKind = _piecePenalty ? "penalty" : "freekick";
            Hold = 2.2f;
            _match.HomeHasBall = home;
            Announce(_piecePenalty ? "PENÁLTI" : "FALTA", squad[index]);
            _match.AddNote((_piecePenalty ? "Penálti. " : "Falta de ") + fouler + " (" + side + ").", "foul");
            if (_match.Minute >= _quietCards && _rng.NextDouble() < 0.22)
            {
                Book(squad[index], false);
                Announce("CARTÃO AMARELO", squad[index]);
                _match.AddNote("Amarelo para " + fouler + ".", "yellow");
                _quietCards = _match.Minute + 12f;
            }
        }

        void StrikePiece()
        {
            var home = _pieceHome;
            var penalty = _piecePenalty;
            _pieceLive = false;
            _wall = new int[0];
            _match.HomeHasBall = home;
            _match.BallHolder = _pieceTaker;
            var depth = home ? _pieceSpot.y : 1f - _pieceSpot.y;
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
            Ball = new Vector2(left ? 0.02f : 0.98f, Mathf.Clamp(along, 0.1f, 0.9f));
            BallTarget = Ball;
            ActionKind = "throwin";
            Hold = 0.9f;
            _match.HomeHasBall = home;
            _match.BallHolder = PickSetPieceTaker(home, 1);
            _match.LastEvent = "Lançamento lateral.";
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

        void Book(string id, bool red)
        {
            if (string.IsNullOrEmpty(id))
                return;
            _match.Bookings[id] = red ? 2 : 1;
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

        static double ShotChance(TacticalIdentity attack, TacticalIdentity defense)
        {
            var pressing = attack.Pressing != null && attack.Pressing.Contains("alta") ? 0.04 : 0;
            var lowBlock = defense.DefensiveLine != null && defense.DefensiveLine.Contains("baixa") ? -0.03 : 0;
            return 0.16 + pressing + lowBlock;
        }

        static float PressAmount(TacticalIdentity tactics)
        {
            var pressing = tactics != null ? tactics.Pressing : "";
            if (pressing != null && pressing.Contains("alta"))
                return 0.22f;
            if (pressing != null && pressing.Contains("baixa"))
                return 0.08f;
            return 0.14f;
        }

        bool TryIntercept(float dt)
        {
            if (ActionKind == "shot" || ActionKind == "goal" || ActionKind == "kickoff")
                return false;
            if (Vector2.Distance(Ball, BallTarget) < 0.04f)
                return false;
            var attackHome = _match.HomeHasBall;
            var count = attackHome ? _match.AwayOnField.Count : _match.HomeOnField.Count;
            var best = 99f;
            var who = -1;
            for (var i = 1; i < count && i < 11; i++)
            {
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
            if (_rng.NextDouble() > chance)
                return false;
            Turnover(attackHome);
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
            if (ActionTime > 0f)
                return false;
            var attackHome = _match.HomeHasBall;
            var count = attackHome ? _match.AwayOnField.Count : _match.HomeOnField.Count;
            var best = 99f;
            var who = 1;
            for (var i = 0; i < count && i < 11; i++)
            {
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
            if (_rng.NextDouble() > 0.22f + press)
                return false;
            var squad = attackHome ? _match.AwayOnField : _match.HomeOnField;
            if (who >= 0 && who < squad.Count && _match.People.TryGetValue(squad[who], out var tackler) && tackler.Fatigue > 84 && _rng.NextDouble() < 0.18)
            {
                tackler.Injury.IsInjured = true;
                tackler.Injury.Description = "Lesão muscular";
                tackler.Injury.DaysRemaining = 12;
                _match.AddNote(tackler.FullName + " fica no chão. Lesão muscular.", "injury");
                Announce("LESÃO", squad[who]);
                Hold = 1.4f;
                ActionKind = "injury";
                return true;
            }

            Turnover(attackHome);
            return true;
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
        }

        void Tire(string id, float amount)
        {
            if (string.IsNullOrEmpty(id) || !_match.People.TryGetValue(id, out var player))
                return;
            if (!_load.ContainsKey(id))
                _load[id] = 0f;
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
            var end = Mathf.Min(squad.Count, Mathf.Max(start + 1, 9));
            return _rng.Next(start, end);
        }
    }
}
