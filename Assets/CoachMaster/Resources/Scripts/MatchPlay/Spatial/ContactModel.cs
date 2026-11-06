using System;
using UnityEngine;

namespace CoachMaster.MatchPlay.Spatial
{
    /// <summary>
    /// Pass error, shot chance and one duel, from ratings and where the ball is.
    /// The recorded xG is the chance used for that shot, so conversion follows the chance instead of a season target.
    /// </summary>
    public static class ContactModel
    {
        public const float PitchLength = 105f;
        public const float PitchWidth = 68f;
        public const float GoalWidth = 7.32f;

        public static float PassMissChance(AttributeScale passer, float distanceMeters, float pressure01, bool laneBlocked, bool throughBall, bool cross)
        {
            var skill = cross
                ? (0.60f * passer.Crossing + 0.20f * passer.Vision + 0.20f * passer.Composure) / 100f
                : (0.55f * passer.Passing + 0.25f * passer.Vision + 0.20f * passer.Composure) / 100f;
            var sigma = (0.8f + (1f - skill) * 4f) * Mathf.Deg2Rad;
            sigma *= 1f + distanceMeters / 70f;
            sigma *= 1f + 0.5f * Mathf.Clamp01(pressure01);
            if (laneBlocked)
                sigma *= 1.28f;
            if (throughBall)
                sigma *= 1.22f;
            var lateral = Mathf.Max(0.05f, distanceMeters * Mathf.Tan(sigma));
            var window = 1.15f + passer.FirstTouch / 100f * 0.7f;
            var hit = Erf(window / (lateral * 1.41421356f));
            // A miss is uncommon. The ball still finishes near the receiver instead of dying halfway.
            return Mathf.Clamp((1f - hit) * 0.45f, 0.02f, 0.16f);
        }

        public static float InterceptChance(AttributeScale cutter, AttributeScale passer, float laneMeters)
        {
            var reach = Mathf.Clamp01(1f - laneMeters / 2.4f);
            var read = (0.65f * cutter.Anticipation + 0.35f * cutter.Marking) / 100f;
            var hide = (0.7f * passer.Passing + 0.3f * passer.Vision) / 100f;
            return Mathf.Clamp(0.08f + reach * (0.22f + 0.45f * (read - hide)), 0.04f, 0.55f);
        }

        public static float ShotChance(Vector2 origin, bool attackingUp, int bodiesInLane, float keeperOffLineMeters, AttributeScale shooter, AttributeScale keeper, float pressure01)
        {
            var goal = new Vector2(0.5f, attackingUp ? 1f : 0f);
            var distance = Meters(origin, goal);
            var angle = ShotAngle(origin, attackingUp);
            var geometry = 2.6f * (angle / Mathf.PI) * Mathf.Exp(-distance / 14f);
            geometry *= Mathf.Pow(0.68f, Mathf.Max(0, bodiesInLane));
            geometry *= 1f + Mathf.Min(0.12f, keeperOffLineMeters / 50f);
            geometry *= 1.10f - 0.20f * (keeper.KeeperPositioning / 100f);
            geometry = Mathf.Clamp(geometry, 0.005f, 0.70f);

            var finish = (0.7f * shooter.Finishing + 0.3f * shooter.Composure) / 100f;
            var onTarget = Mathf.Clamp(0.28f + 0.35f * finish - 0.22f * Mathf.Clamp01(pressure01) + 0.35f * geometry, 0.10f, 0.85f);
            var stopper = (0.6f * keeper.KeeperStopping + 0.4f * keeper.Reflexes) / 100f;
            var givenOn = Mathf.Clamp(geometry * (0.85f + 0.45f * finish) * (2.75f - 1.15f * stopper), 0.03f, 0.88f);
            return Mathf.Clamp(onTarget * givenOn, 0.01f, 0.62f);
        }

        public static float OnTargetChance(float shotChance, AttributeScale shooter, float pressure01)
        {
            var finish = (0.7f * shooter.Finishing + 0.3f * shooter.Composure) / 100f;
            var onTarget = Mathf.Clamp(0.34f + 0.40f * finish - 0.18f * Mathf.Clamp01(pressure01), shotChance + 0.04f, 0.9f);
            return onTarget;
        }

        public static string Duel(System.Random rng, AttributeScale defender, AttributeScale carrier, bool ownBox)
        {
            var attack = (0.45f * carrier.Dribbling + 0.25f * carrier.Composure + 0.15f * carrier.Agility + 0.15f * carrier.Strength) / 100f;
            var defend = (0.50f * defender.Tackling + 0.20f * defender.Anticipation + 0.15f * defender.Strength + 0.15f * defender.Marking) / 100f;
            var win = Mathf.Clamp(0.34f + 0.55f * (defend - attack) + 0.12f, 0.08f, 0.72f);
            var aggression = defender.Aggression / 100f;
            var foul = Mathf.Clamp(0.10f + 0.14f * aggression + 0.09f * Mathf.Max(0f, attack - defend), 0.015f, 0.34f);
            if (ownBox)
                foul *= 0.15f;
            var roll = (float)rng.NextDouble();
            if (roll < foul)
                return "foul";
            if (roll < foul + win)
                return "won";
            return "none";
        }

        public static float DecisionNoise(System.Random rng, float decisions01, float freshness01)
        {
            var width = 0.22f * (1f - Mathf.Clamp01(decisions01)) + 0.08f * (1f - Mathf.Clamp01(freshness01));
            return Gaussian(rng) * width;
        }

        public static int BodiesOnShot(float markerGap)
        {
            if (markerGap < 0.055f)
                return 2;
            if (markerGap < 0.1f)
                return 1;
            return 0;
        }

        public static float Pressure(float markerGap)
        {
            return Mathf.Clamp01(1f - markerGap / 0.14f);
        }

        public static float Meters(Vector2 a, Vector2 b)
        {
            var dx = (a.x - b.x) * PitchWidth;
            var dy = (a.y - b.y) * PitchLength;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static float PointSegmentMeters(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var denom = ab.sqrMagnitude;
            if (denom < 0.000001f)
                return Meters(point, a);
            var t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / denom);
            return Meters(point, a + ab * t);
        }

        static float ShotAngle(Vector2 from, bool attackingUp)
        {
            var goalY = attackingUp ? 1f : 0f;
            var half = GoalWidth * 0.5f / PitchWidth;
            var left = new Vector2(0.5f - half, goalY) - from;
            var right = new Vector2(0.5f + half, goalY) - from;
            if (left.sqrMagnitude < 0.0001f || right.sqrMagnitude < 0.0001f)
                return Mathf.PI * 0.5f;
            var dot = Mathf.Clamp(Vector2.Dot(left.normalized, right.normalized), -1f, 1f);
            return Mathf.Acos(dot);
        }

        static float Gaussian(System.Random rng)
        {
            var u = Mathf.Max(1e-6f, (float)rng.NextDouble());
            var v = (float)rng.NextDouble();
            return Mathf.Sqrt(-2f * Mathf.Log(u)) * Mathf.Cos(2f * Mathf.PI * v);
        }

        static float Erf(float x)
        {
            var sign = x < 0f ? -1f : 1f;
            var ax = Mathf.Abs(x);
            var t = 1f / (1f + 0.3275911f * ax);
            var y = 1f - (((((1.061405429f * t - 1.453152027f) * t) + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-ax * ax);
            return sign * y;
        }
    }
}
