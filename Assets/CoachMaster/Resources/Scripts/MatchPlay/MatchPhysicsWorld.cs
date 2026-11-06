using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CoachMaster.MatchPlay
{
    /// <summary>
    /// Isolated 2D physics world for the match tokens. Tactical code supplies desired
    /// velocities; Rigidbody2D bodies own the final positions and physical contacts.
    /// </summary>
    internal sealed class MatchPhysicsWorld : IDisposable
    {
        const float PitchWidthMeters = 68f;
        const float PitchLengthMeters = 105f;
        const float PlayerRadiusMeters = 0.34f;
        const float BallRadiusMeters = 0.11f;
        const float PlayerInsetMeters = 1.7f;

        readonly Scene _scene;
        readonly PhysicsScene2D _physicsScene;
        readonly Rigidbody2D[] _players = new Rigidbody2D[22];
        readonly PhysicsMaterial2D _contactMaterial;
        readonly Rigidbody2D _ball;
        readonly CircleCollider2D _ballCollider;

        public MatchPhysicsWorld(Vector2[] home, Vector2[] away, Vector2 ball)
        {
            var sceneName = "CoachMasterMatchPhysics_" + Guid.NewGuid().ToString("N");
            _scene = SceneManager.CreateScene(sceneName, new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            _physicsScene = _scene.GetPhysicsScene2D();
            if (!_physicsScene.IsValid())
                throw new InvalidOperationException("Could not create the isolated 2D match physics scene.");

            _contactMaterial = new PhysicsMaterial2D("MatchContact")
            {
                friction = 0.16f,
                bounciness = 0f
            };

            for (var i = 0; i < 11; i++)
            {
                _players[i] = CreateBody("HomePlayer" + i, 75f, PlayerRadiusMeters, CollisionDetectionMode2D.Discrete, _contactMaterial, 0.65f);
                _players[i + 11] = CreateBody("AwayPlayer" + i, 75f, PlayerRadiusMeters, CollisionDetectionMode2D.Discrete, _contactMaterial, 0.65f);
                _players[i].position = ToMeters(home[i]);
                _players[i + 11].position = ToMeters(away[i]);
            }

            _ball = CreateBody("Ball", 0.43f, BallRadiusMeters, CollisionDetectionMode2D.Continuous, _contactMaterial, 0.02f);
            _ballCollider = _ball.GetComponent<CircleCollider2D>();
            _ball.position = ToMeters(ball);
        }

        Rigidbody2D CreateBody(string name, float mass, float radius, CollisionDetectionMode2D collisionMode,
            PhysicsMaterial2D material, float damping)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, _scene);
            var body = gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.mass = mass;
            body.gravityScale = 0f;
            body.linearDamping = damping;
            body.angularDamping = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.None;
            body.collisionDetectionMode = collisionMode;
            var collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = radius;
            collider.sharedMaterial = material;
            return body;
        }

        public void Simulate(float deltaTime, Vector2[] homeVelocity, Vector2[] awayVelocity,
            Vector2[] homePosition, Vector2[] awayPosition, Vector2 ball,
            out Vector2 ballPosition, out Vector2 resolvedBallVelocity)
        {
            _ballCollider.enabled = false;
            _ball.position = ToMeters(ball);
            _ball.linearVelocity = Vector2.zero;
            // Player steering velocities are stored in normalized pitch units.
            // The match moves the ball itself; a body contact was launching it across the pitch.
            for (var i = 0; i < 11; i++)
            {
                _players[i].linearVelocity = ToMeters(homeVelocity[i]);
                _players[i + 11].linearVelocity = ToMeters(awayVelocity[i]);
            }

            var substeps = Mathf.Clamp(Mathf.CeilToInt(deltaTime / (1f / 60f)), 1, 8);
            var step = deltaTime / substeps;
            for (var i = 0; i < substeps; i++)
                _physicsScene.Simulate(step);

            for (var i = 0; i < 11; i++)
            {
                ClampInsidePitch(_players[i]);
                ClampInsidePitch(_players[i + 11]);
                homePosition[i] = FromMeters(_players[i].position);
                awayPosition[i] = FromMeters(_players[i + 11].position);
                homeVelocity[i] = FromMeters(_players[i].linearVelocity);
                awayVelocity[i] = FromMeters(_players[i + 11].linearVelocity);
            }

            ballPosition = FromMeters(_ball.position);
            resolvedBallVelocity = _ball.linearVelocity;
        }

        void ClampInsidePitch(Rigidbody2D body)
        {
            var position = body.position;
            var velocity = body.linearVelocity;
            var minX = PlayerInsetMeters;
            var maxX = PitchWidthMeters - PlayerInsetMeters;
            var minY = PlayerInsetMeters;
            var maxY = PitchLengthMeters - PlayerInsetMeters;
            if (position.x < minX)
            {
                position.x = minX;
                if (velocity.x < 0f) velocity.x = 0f;
            }
            else if (position.x > maxX)
            {
                position.x = maxX;
                if (velocity.x > 0f) velocity.x = 0f;
            }
            if (position.y < minY)
            {
                position.y = minY;
                if (velocity.y < 0f) velocity.y = 0f;
            }
            else if (position.y > maxY)
            {
                position.y = maxY;
                if (velocity.y > 0f) velocity.y = 0f;
            }
            body.position = position;
            body.linearVelocity = velocity;
        }

        public void ResetPlayers(Vector2[] home, Vector2[] away)
        {
            for (var i = 0; i < 11; i++)
            {
                ResetBody(_players[i], home[i]);
                ResetBody(_players[i + 11], away[i]);
            }
        }

        public void ResetBall(Vector2 ball)
        {
            ResetBody(_ball, ball);
            _ballCollider.enabled = true;
        }

        static void ResetBody(Rigidbody2D body, Vector2 position)
        {
            body.position = ToMeters(position);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        static Vector2 ToMeters(Vector2 position)
        {
            return new Vector2(position.x * PitchWidthMeters, position.y * PitchLengthMeters);
        }

        static Vector2 FromMeters(Vector2 position)
        {
            return new Vector2(position.x / PitchWidthMeters, position.y / PitchLengthMeters);
        }

        public void Dispose()
        {
            if (_scene.IsValid() && _scene.isLoaded)
                SceneManager.UnloadSceneAsync(_scene);
            if (_contactMaterial != null)
                UnityEngine.Object.Destroy(_contactMaterial);
        }
    }
}
