using System;
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

        public MatchEngine(MatchSession match)
        {
            _match = match;
            _rng = new System.Random(match.Fixture.Id.GetHashCode());
            Ball = new Vector2(0.5f, 0.5f);
            BallTarget = Slot(true, 9, _match.HomeTactics.Formation);
        }

        public void Tick(float dt, float minutesPerSecond)
        {
            if (_match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime || _match.Finished)
                return;

            _match.Minute += dt * minutesPerSecond;
            ActionTime -= dt;
            Ball = Vector2.Lerp(Ball, BallTarget, 1f - Mathf.Exp(-dt * 6f));

            if (_match.Phase == MatchPhase.FirstHalf && _match.Minute >= 45f)
            {
                _match.Phase = MatchPhase.HalfTime;
                _match.LastEvent = "Intervalo. O plantel espera a tua conversa.";
                return;
            }

            if (_match.Phase == MatchPhase.SecondHalf && _match.Minute >= 90f)
            {
                Finish();
                return;
            }

            if (ActionTime > 0f)
                return;

            NextAction();
        }

        public void StartSecondHalf()
        {
            _match.Phase = MatchPhase.SecondHalf;
            if (_match.Minute < 45f)
                _match.Minute = 45f;
            _match.HomeHasBall = false;
            Ball = new Vector2(0.5f, 0.5f);
            BallTarget = Slot(false, 9, _match.AwayTactics.Formation);
            _match.LastEvent = "Segunda parte. O jogo recomeça.";
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
            _match.LastEvent = $"Substituição: sai {Name(outId)}, entra {Name(field[fieldIndex])}.";
            return true;
        }

        public void ApplyTeamTalk(string talk)
        {
            _match.TeamTalk = talk;
            _match.LastEvent = talk;
        }

        void NextAction()
        {
            var attack = _match.HomeHasBall;
            var tactics = attack ? _match.HomeTactics : _match.AwayTactics;
            var defense = attack ? _match.AwayTactics : _match.HomeTactics;
            var roll = _rng.NextDouble();
            var shotChance = ShotChance(tactics, defense);
            if (roll < shotChance)
            {
                Shoot(attack);
                return;
            }

            if (roll < shotChance + 0.18)
            {
                Turnover(attack);
                return;
            }

            Pass(attack);
        }

        void Pass(bool home)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var fromName = Name(squad[Mathf.Clamp(_match.BallHolder, 0, squad.Count - 1)]);
            var next = _rng.Next(0, Mathf.Min(11, squad.Count));
            var toName = Name(squad[next]);
            _match.BallHolder = next;
            _match.HomeHasBall = home;
            BallTarget = Slot(home, next, home ? _match.HomeTactics.Formation : _match.AwayTactics.Formation);
            ActionTime = 0.75f;
            ActionKind = "pass";
            _match.LastEvent = $"Passe de {fromName} para {toName}.";
        }

        void Shoot(bool home)
        {
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var shooter = Name(squad[Mathf.Clamp(_match.BallHolder, 0, squad.Count - 1)]);
            BallTarget = home ? new Vector2(0.5f, 0.97f) : new Vector2(0.5f, 0.03f);
            ActionTime = 1.1f;
            ActionKind = "shot";
            var quality = AttackStrength(home ? _match.HomeTactics : _match.AwayTactics);
            var save = DefenseStrength(home ? _match.AwayTactics : _match.HomeTactics);
            var boost = home ? _match.AttackBoost : 0f;
            var wall = home ? _match.DefenseBoost : 0f;
            if (_rng.NextDouble() < 0.18 + quality * 0.14 - save * 0.10 + boost - wall)
            {
                if (home) _match.HomeGoals++;
                else _match.AwayGoals++;
                _match.LastEvent = $"GOLO! {shooter} marca.";
                ActionKind = "goal";
            }
            else
            {
                _match.LastEvent = $"Remate de {shooter}. Defesa!";
            }

            _match.HomeHasBall = !home;
            _match.BallHolder = 0;
        }

        void Turnover(bool home)
        {
            _match.HomeHasBall = !home;
            var squad = home ? _match.AwayOnField : _match.HomeOnField;
            _match.BallHolder = _rng.Next(1, Mathf.Min(7, squad.Count));
            BallTarget = Slot(!home, _match.BallHolder, home ? _match.AwayTactics.Formation : _match.HomeTactics.Formation);
            ActionTime = 0.65f;
            ActionKind = "turnover";
            _match.LastEvent = $"Corte. {Name(squad[_match.BallHolder])} recupera a bola.";
        }

        string Name(string id)
        {
            return _match.Names.TryGetValue(id, out var name) ? name : "Jogador";
        }

        void Finish()
        {
            _match.Phase = MatchPhase.FullTime;
            _match.Finished = true;
            _match.Minute = 90f;
            _match.LastEvent = "Fim do jogo.";
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
            return 0.11 + pressing + lowBlock;
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
    }
}
