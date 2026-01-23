using UnityEngine;
using SpineTool;

namespace SpineTool.Examples
{
    /// <summary>
    /// SpineAnimationController 사용 예제
    ///
    /// 간편한 애니메이션 재생 및 이벤트 등록 방법을 보여줍니다.
    ///
    /// 특징:
    /// - 코드로 애니메이션 재생 제어
    /// - 이벤트 리스너 등록/제거
    /// - SpineEventInjector와 함께 사용 가능
    /// </summary>
    public class SpineControllerExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpineAnimationController controller;

        [Header("Animation Names")]
        [SerializeField] private string idleAnimation = "idle";
        [SerializeField] private string walkAnimation = "walk";
        [SerializeField] private string runAnimation = "run";
        [SerializeField] private string attackAnimation = "attack";
        [SerializeField] private string jumpAnimation = "jump";

        private void Awake()
        {
            // SpineAnimationController 가져오기
            if (controller == null)
            {
                controller = GetComponent<SpineAnimationController>();
            }
        }

        private void Start()
        {
            // 이벤트 리스너 등록
            RegisterEventListeners();

            // 기본 애니메이션 재생
            controller.PlayAnimation(idleAnimation, true);
        }

        private void OnDestroy()
        {
            // 이벤트 리스너 해제
            UnregisterEventListeners();
        }

        #region Event Listeners

        private void RegisterEventListeners()
        {
            // Spine 툴에서 설정한 이벤트 리스너 등록
            controller.AddEventListener("footstep", OnFootstep);
            controller.AddEventListener("hit_impact", OnHitImpact);
            controller.AddEventListener("jump_land", OnJumpLand);
            controller.AddEventListener("weapon_swoosh", OnWeaponSwoosh);

            Debug.Log("[SpineControllerExample] Event listeners registered");
        }

        private void UnregisterEventListeners()
        {
            if (controller == null) return;

            controller.RemoveEventListener("footstep", OnFootstep);
            controller.RemoveEventListener("hit_impact", OnHitImpact);
            controller.RemoveEventListener("jump_land", OnJumpLand);
            controller.RemoveEventListener("weapon_swoosh", OnWeaponSwoosh);
        }

        // 이벤트 핸들러들
        private void OnFootstep(SpineEventData data)
        {
            Debug.Log($"👟 Footstep! (Animation: {data.AnimationName}, Time: {data.TrackTime:F2}s)");
            // PlayFootstepSound();
        }

        private void OnHitImpact(SpineEventData data)
        {
            int damage = data.IntParameter;
            Debug.Log($"💥 Hit Impact! Damage: {damage}");
            // SpawnHitEffect();
            // ApplyDamage(damage);
        }

        private void OnJumpLand(SpineEventData data)
        {
            Debug.Log("🎯 Landed!");
            // PlayLandSound();
            // SpawnDustEffect();
        }

        private void OnWeaponSwoosh(SpineEventData data)
        {
            Debug.Log("⚔️ Weapon Swoosh!");
            // PlayWeaponSound();
        }

        #endregion

        #region Input Handling (테스트용)

        private void Update()
        {
            // 키보드 입력으로 애니메이션 테스트
            HandleInput();
        }

        private void HandleInput()
        {
            // 1 - Idle
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                PlayIdle();
            }
            // 2 - Walk
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                PlayWalk();
            }
            // 3 - Run
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                PlayRun();
            }
            // Space - Attack
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayAttack();
            }
            // W - Jump
            else if (Input.GetKeyDown(KeyCode.W))
            {
                PlayJump();
            }
            // S - Stop
            else if (Input.GetKeyDown(KeyCode.S))
            {
                StopAnimation();
            }
            // P - Pause/Resume
            else if (Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }
        }

        #endregion

        #region Animation Control

        public void PlayIdle()
        {
            controller.PlayAnimation(idleAnimation, true);
            Debug.Log("Playing: Idle");
        }

        public void PlayWalk()
        {
            controller.PlayAnimation(walkAnimation, true);
            Debug.Log("Playing: Walk");
        }

        public void PlayRun()
        {
            controller.PlayAnimation(runAnimation, true);
            Debug.Log("Playing: Run");
        }

        public void PlayAttack()
        {
            // 공격은 한 번만 재생 (loop = false)
            controller.PlayAnimation(attackAnimation, false);

            // 공격 후 Idle로 자동 전환
            controller.AddAnimation(idleAnimation, true, 0f);

            Debug.Log("Playing: Attack → Idle");
        }

        public void PlayJump()
        {
            controller.PlayAnimation(jumpAnimation, false);

            // 점프 후 Idle로
            controller.AddAnimation(idleAnimation, true, 0f);

            Debug.Log("Playing: Jump → Idle");
        }

        public void StopAnimation()
        {
            controller.StopAllAnimations();
            controller.SetToSetupPose();
            Debug.Log("Animation stopped");
        }

        private bool isPaused = false;
        public void TogglePause()
        {
            if (isPaused)
            {
                controller.ResumeAnimation();
                Debug.Log("Animation resumed");
            }
            else
            {
                controller.PauseAnimation();
                Debug.Log("Animation paused");
            }
            isPaused = !isPaused;
        }

        #endregion

        #region Advanced Features

        /// <summary>
        /// 속도 변경 예제 (슬로우 모션 등)
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            controller.SetSpeed(speed);
            Debug.Log($"Animation speed: {speed}x");
        }

        /// <summary>
        /// 스킨 변경 예제
        /// </summary>
        public void ChangeSkin(string skinName)
        {
            controller.SetSkin(skinName);
            Debug.Log($"Skin changed to: {skinName}");
        }

        /// <summary>
        /// 블렌딩 시간 설정 예제
        /// </summary>
        public void SetupBlending()
        {
            // Walk <-> Run 빠르게 전환 (0.2초)
            controller.SetMixDuration(walkAnimation, runAnimation, 0.2f);
            controller.SetMixDuration(runAnimation, walkAnimation, 0.2f);

            // Attack -> Idle 부드럽게 전환 (0.3초)
            controller.SetMixDuration(attackAnimation, idleAnimation, 0.3f);

            Debug.Log("Animation blending configured");
        }

        #endregion

        #region GUI (테스트용)

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 500));
            GUILayout.Box("SpineAnimationController Example");

            GUILayout.Label("━━━━━ Animation Control ━━━━━");

            if (GUILayout.Button("1. Idle (Loop)"))
                PlayIdle();

            if (GUILayout.Button("2. Walk (Loop)"))
                PlayWalk();

            if (GUILayout.Button("3. Run (Loop)"))
                PlayRun();

            if (GUILayout.Button("Space. Attack (Once)"))
                PlayAttack();

            if (GUILayout.Button("W. Jump (Once)"))
                PlayJump();

            GUILayout.Space(10);
            GUILayout.Label("━━━━━ Playback Control ━━━━━");

            if (GUILayout.Button("S. Stop"))
                StopAnimation();

            if (GUILayout.Button($"P. {(isPaused ? "Resume" : "Pause")}"))
                TogglePause();

            GUILayout.Space(10);
            GUILayout.Label("━━━━━ Advanced ━━━━━");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:");
            if (GUILayout.Button("0.5x")) SetAnimationSpeed(0.5f);
            if (GUILayout.Button("1x")) SetAnimationSpeed(1f);
            if (GUILayout.Button("2x")) SetAnimationSpeed(2f);
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("━━━━━ Info ━━━━━");

            if (controller != null)
            {
                GUILayout.Label($"Current: {controller.CurrentAnimationName}");
                GUILayout.Label($"Playing: {controller.IsPlaying}");
            }

            GUILayout.Label("\nKeyboard Shortcuts:");
            GUILayout.Label("  1, 2, 3 - Idle/Walk/Run");
            GUILayout.Label("  Space - Attack");
            GUILayout.Label("  W - Jump");
            GUILayout.Label("  S - Stop");
            GUILayout.Label("  P - Pause/Resume");

            GUILayout.EndArea();
        }

        #endregion
    }
}
