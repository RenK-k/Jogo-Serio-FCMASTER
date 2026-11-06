using UnityEngine;

using FStudio.MatchEngine.Players;
using FStudio.MatchEngine.Balls;

using System.Linq;
using System.Collections.Generic;

namespace FStudio.MatchEngine {
    public class GoalNet : MonoBehaviour {
        [SerializeField] private Transform middlePoint = default;

        public GameObject GoalColliders, OutColliders;
        public Transform[] goalPoints;

        public Transform leftLimit;
        public Transform rightLimit;

        public Vector3 GetShootingVectorFromPoint (PlayerBase playerBase, 
            Transform point) {

            var playerPosition = playerBase.Position;

            var skill = playerBase.MatchPlayer.GetShooting();

            var dir = point.position - playerPosition;

            var dirErrorApplied = Ball.ApplyDirectionError(dir, skill);

            // restore by distance. Close shots used to lose every miss.
            var dist = Vector3.Distance(point.position, playerPosition);
            var remove = EngineSettings.Current.ShootErrorRemoveByDistance.Evaluate(dist);
            var cap = Mathf.Lerp(0.38f, 0.16f, Mathf.Clamp01((dist - 6f) / 18f));
            dirErrorApplied = Vector3.Lerp(dirErrorApplied, dir, Mathf.Clamp01(remove) * cap);

            var keeper = OpponentKeeper(playerBase);
            var keeping = keeper != null ? keeper.MatchPlayer.ActualReaction : 55;
            var pressure = Mathf.Clamp01((keeping + 18f - Mathf.Clamp(skill, 1f, 100f)) / 90f);
            var miss = Mathf.Lerp(4.6f, 9.2f, pressure);
            if (dist < 16f)
                miss += Mathf.Lerp(3.2f, 0f, dist / 16f);
            else
                miss += (dist - 16f) * 0.18f;
            if (MatchManager.Current != null && playerBase.GameTeam != null)
                miss *= CoachMaster.MatchPlay.PitchOrders.Miss(playerBase.GameTeam == MatchManager.Current.GameTeam1);
            var side = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            dirErrorApplied = Quaternion.Euler(UnityEngine.Random.Range(-0.4f, 1.1f) * miss * 0.25f, side * miss, 0f) * dirErrorApplied;
            if (keeper != null && UnityEngine.Random.value < 0.34f + pressure * 0.4f) {
                var atKeeper = keeper.Position - playerPosition;
                atKeeper.y = 0.55f;
                if (atKeeper.sqrMagnitude > 0.01f)
                    dirErrorApplied = Vector3.Lerp(dirErrorApplied, atKeeper.normalized * dirErrorApplied.magnitude, 0.32f + pressure * 0.22f);
            } 

            var errorAppliedAngle = Mathf.Abs(Vector3.SignedAngle(dir, dirErrorApplied, Vector3.up));

            //normalize it.
            dirErrorApplied = dirErrorApplied.normalized;

            var dir2D = dirErrorApplied;
            dir2D.y = 0;

            var dirUp = dirErrorApplied;
            dirUp.x = dirUp.z = 0;

            // add multipliers.
            dirErrorApplied += dir2D * EngineSettings.Current.ShootingForwardAxisMultiplier;
            //

            dirErrorApplied *= EngineSettings.Current.ShootPowerByDistanceCurve.Evaluate(dir.magnitude);

            dirErrorApplied *= EngineSettings.Current.ShootPowerBySkillCurve.
                Evaluate(playerBase.MatchPlayer.GetShooting() / 100f);

            Debug.Log($"[Shootpoint found] {dirErrorApplied}");

            dirErrorApplied += Vector3.up * dir.magnitude * EngineSettings.Current.ShootingUpAxisDistanceMultiplier;

            Debug.Log($"[Shooting point y fixed] {dirErrorApplied}");

            Debug.DrawRay(playerPosition, dir, Color.yellow, 1);
            Debug.DrawRay(playerPosition, dirErrorApplied, Color.green, 1);

            if (dirErrorApplied.y < 1) {
                dirErrorApplied.y = 1;
            }

            return dirErrorApplied;
        }

        static PlayerBase OpponentKeeper(PlayerBase shooter) {
            var players = MatchManager.AllPlayers;
            if (players == null || shooter == null)
                return null;
            foreach (var player in players) {
                if (player != null && player.IsGK && player.GameTeam != shooter.GameTeam)
                    return player;
            }
            return null;
        }

        /// <summary>
        /// Checks all goal points, 
        /// and return shooting velocities with direction error applied.
        /// </summary>
        /// <param name="playerBase">Shooter</param>
        /// <param name="colliders">Possible colliders</param>
        /// <returns>Velocity, and applied error.</returns>
        public (Transform shootPoint, float angleFree)
            GetShootingVector (PlayerBase playerBase, PlayerBase[] colliders) {
            
            if (goalPoints.Length == 0) {
                return default;
            }

            var fieldSizeY = MatchManager.Current.SizeOfField.y;

            var mPosition = playerBase.Position;

            float minAngle (Transform m_point) {
                var pointToPlayer = m_point.position - mPosition;

                float min = colliders.Select(x => Mathf.Min (Mathf.Abs (Vector3.SignedAngle(x.Position - mPosition, pointToPlayer, Vector3.up)), 45)).
                OrderBy (x=>x).FirstOrDefault ();
                return min;
            }

            var shootingVector =
                goalPoints.Select (x=>(x, minAngle (x))).OrderBy(x => 
                 Random.Range (-5, 5) +
                 Mathf.Abs(x.x.position.x - fieldSizeY / 2) + 
                 Random.Range (0, x.x.position.y) + 
                 (45-x.Item2)/2).
                 FirstOrDefault();

            return shootingVector;
        }

        /// <summary>
        /// Middle point of the goal net.
        /// </summary>
        public Vector3 Position => middlePoint.position;

        /// <summary>
        /// Direction of the goal net. System will use this direction for attacking & defending.
        /// </summary>
        public Vector3 Direction => middlePoint.forward;
    }
}
