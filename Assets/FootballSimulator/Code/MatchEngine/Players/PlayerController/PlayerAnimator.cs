
using UnityEngine;
using System.Collections.Generic;
using FStudio.MatchEngine.Graphics;
using FStudio.MatchEngine.Utilities;
using FStudio.Utilities;
using FStudio.MatchEngine.Balls;
using System.Linq;

using FStudio.MatchEngine.Graphics.EventRenderer;

namespace FStudio.MatchEngine.Players.PlayerController {
    [RequireComponent (typeof (Animator))]
    [RequireComponent (typeof (PlayerGraphic))]
    public class PlayerAnimator : MonoBehaviour {       
        private const string BALL_DRIBBLE_SPEED_ANIMATOR_FLOAT_NAME = "Speed";

        [SerializeField] private Animator animator;

        private Dictionary<string, int> animatorVariableHashes;

        [SerializeField] private IKController gkIkController;

        public IKController GKIKController => gkIkController;

        [SerializeField] private float footShadowForwardOffset = 0.2f;

        [SerializeField] private HeadLook headLook;

        [SerializeField] private Animator ballDribbleAnimator;

        [SerializeField] private PlayerBallPoint ballPoint = default;

        private Transform footLShadow, footRShadow;

        private Material footLMaterial, footRMaterial;

        public Vector3 BallPosition(PlayerBallPoint.Situation situation) => ballPoint.GetPosition(situation);
        public Quaternion BallRotation(PlayerBallPoint.Situation situation) => ballPoint.GetRotation(situation);

        public Vector3 AnimatorDirection { set; private get; }

        [SerializeField] private PlayerGraphic playerGraphic;

        private void OnDestroy() {
            if (footLShadow != null) {
                footLShadow.gameObject.SetActive(false);
            }

            if (footRShadow != null) {
                footRShadow.gameObject.SetActive(false);
            }
        }

        private void OnValidate () {
            animator = GetComponent<Animator>();
            playerGraphic = GetComponent<PlayerGraphic>();
        }

        private void Awake () {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator != null)
                animatorVariableHashes = AnimatorEnumHasher.GetHashes<PlayerAnimatorVariable>(animator);

            if (FootShadowRenderer.Current == null)
                return;

            footLShadow = FootShadowRenderer.Current.Get();
            footRShadow = FootShadowRenderer.Current.Get();
            if (footLShadow != null) {
                var left = footLShadow.GetComponentInChildren<Renderer>();
                if (left != null)
                    footLMaterial = left.material;
            }
            if (footRShadow != null) {
                var right = footRShadow.GetComponentInChildren<Renderer>();
                if (right != null)
                    footRMaterial = right.material;
            }
        }

        public void SetAnimatorSpeed (float f) {
            if (!gameObject.activeSelf) {
                return;
            }

            animator.speed = f;
        }

        public void SetAnimator (bool value) {
            animator.enabled = value;
        }

        public bool IsCurrentClipBlocker (in string[] blockers) {
            foreach (var anim in blockers) {
                var hash = animatorVariableHashes[anim];

                if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == hash) {
                    return true;
                }

                if (animator.GetAnimatorTransitionInfo (0).IsName ($"{anim}Transition")) {
                    return true;
                }
            }

            return false;
        }

        private void Update() {
            const float MIN_MOVE_SPEED_TO_MOVE = 0.5f;
            if (ballDribbleAnimator == null || ballDribbleAnimator.runtimeAnimatorController == null)
                return;

            ballDribbleAnimator.SetFloat (
                BALL_DRIBBLE_SPEED_ANIMATOR_FLOAT_NAME, 
                GetFloat(PlayerAnimatorVariable.MoveSpeed) - MIN_MOVE_SPEED_TO_MOVE);
        }

        private void LateUpdate () {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            void setBone (HumanBodyBones boneId, Transform target, Material material) {
                const string SHADOW_POWER = "_Power";
                if (target == null || material == null)
                    return;

                var bone = animator.GetBoneTransform(boneId);
                if (bone == null)
                    return;

                var grounded = bone.position + bone.forward * footShadowForwardOffset;
                grounded.y = 0.04f;
                target.position = grounded;
                target.rotation = Quaternion.Euler(90f, 0f, 0f);

                float height = bone.position.y;
                float heightPow = Mathf.Max(0f, 0.15f - height);
                material.SetFloat(SHADOW_POWER, heightPow);
            }

            setBone(HumanBodyBones.LeftFoot, footLShadow, footLMaterial);
            setBone(HumanBodyBones.RightFoot, footRShadow, footRMaterial);
        }

        public void SetTrigger (PlayerAnimatorVariable animatorVariable) {
            if (!Ready())
                return;

            animator.SetTrigger(animatorVariableHashes[animatorVariable.ToString ()]);
        }

        public void SetBool (PlayerAnimatorVariable animatorVariable, bool value) {
            if (!Ready())
                return;
            animator.SetBool (animatorVariableHashes[animatorVariable.ToString()], value);
        }

        public void SetLayerWeight (int layerIndex, float value) {
            if (!gameObject.activeSelf || animator == null || animator.runtimeAnimatorController == null)
                return;
            animator.SetLayerWeight(layerIndex, value);
        }

        public void SetFloat (PlayerAnimatorVariable animatorVariable, float value) {
            if (!Ready())
                return;
            animator.SetFloat (animatorVariableHashes[animatorVariable.ToString()], value);

            switch (animatorVariable) {
                case PlayerAnimatorVariable.MoveSpeed:
                    const string FAKE_CLOTH_POWER = "_FakeClothPower";
                    if (playerGraphic != null && playerGraphic.mainRenderer != null)
                        playerGraphic.mainRenderer.material.SetFloat(FAKE_CLOTH_POWER, value + 1);
                    break;
            }
        }

        public float GetFloat(PlayerAnimatorVariable animatorVariable) {
            if (!Ready())
                return 0;
            if (!animatorVariableHashes.TryGetValue(animatorVariable.ToString(), out var hash))
                return 0;
            return animator.GetFloat(hash);
        }

        bool Ready() {
            return gameObject.activeSelf && animator != null && animator.runtimeAnimatorController != null && animatorVariableHashes != null;
        }

        private bool GetBool (PlayerAnimatorVariable animatorVariable) {
            if (!Ready())
                return false;
            if (!animatorVariableHashes.TryGetValue(animatorVariable.ToString(), out var hash))
                return false;
            return animator.GetBool(hash);
        }

        public void SetLook (in float deltaTime, Vector3 worldPosition, float weight) {
            if (headLook != null) {
                headLook.SetTarget(deltaTime, worldPosition, weight);
            }
        }

        /// <summary>
        /// Tries to play a ball hit animation.
        /// Returns false if its not possible.
        /// </summary>
        /// <param name="targetVelocity"></param>
        /// <param name="defaultAnim">Requested Animation</param>
        /// <param name="result">Resulted animation</param>
        /// <param name="disableVolley"></param>
        /// <returns></returns>
        public bool PlayBallHitAnimation (
            in Vector3 targetVelocity,
            PlayerAnimatorVariable defaultAnim,
            out PlayerAnimatorVariable result, 
            in float ballHoldTime,
            bool disableVolley = false) {

            result = defaultAnim;

            var animSettings = EngineOptions.EngineOptions_BallHitAnimations.Current;

            if (IsCurrentClipBlocker(in animSettings.BallHitActionBlockers)) {
                return false;
            }

            float ballHeight = Ball.Current.transform.position.y;

            float time = Time.time;

            var specialHit = time < ballHoldTime + animSettings.SPECIAL_BALL_HIT_TIME_OFFSET;

            bool isVolley = specialHit && ballHeight > animSettings.BALL_HEIGHT_VOLLEY_START && ballHeight < animSettings.BALL_HEIGHT_VOLLEY_END;
            bool isGroundHeader = specialHit && ballHeight > animSettings.BALL_HEIGHT_GROUND_HEADER_UNTIL && ballHeight < animSettings.BALL_HEIGHT_VOLLEY_START;
            bool isJumpHeader = specialHit && ballHeight > animSettings.BALL_HEIGHT_VOLLEY_END;
            bool isDefault = ballHeight < animSettings.BALL_HEIGHT_GROUND_HEADER_UNTIL;

            if (isVolley && disableVolley) {
                isJumpHeader = true;
            }

            if (!disableVolley && isVolley && animSettings.AnimSettings.Find (PlayerAnimatorVariable.Volley_R)) {
                result = PlayerAnimatorVariable.Volley_R;

            } else if (isGroundHeader && animSettings.AnimSettings.Find(PlayerAnimatorVariable.GroundHeader_R)) {
                result = PlayerAnimatorVariable.GroundHeader_R;

            } else if (isJumpHeader && animSettings.AnimSettings.Find(PlayerAnimatorVariable.Header_R)) {
                result = PlayerAnimatorVariable.Header_R;
            } else {
                if (animSettings.AnimSettings.Find(defaultAnim)) {
                    result = defaultAnim;
                } else {
                    // play the first enabled, or one of them.
                    var enabledEntry = animSettings.AnimSettings.Entries.OrderByDescending(x => x.Val).FirstOrDefault();
                    result = enabledEntry.Id;
                }
            }

            if (GetBool (PlayerAnimatorVariable.ThrowInIdle)){
                result = PlayerAnimatorVariable.Throw_R;

                SetBool(PlayerAnimatorVariable.ThrowInIdle, false);
            }

            SetTrigger(FootedAnim(result, targetVelocity));

            return true;
        }

        private PlayerAnimatorVariable FootedAnim (PlayerAnimatorVariable defaultAnim, Vector3 targetVelocity) {
            string anim = defaultAnim.ToString ().Split ('_')[0];

            var angle = Vector3.SignedAngle(targetVelocity, transform.forward, Vector3.up);

            if (angle > 0) {
                anim += "_R";
            } else {
                anim += "_L";
            }

            return (PlayerAnimatorVariable)System.Enum.Parse(typeof(PlayerAnimatorVariable), anim);
        }
    }
}