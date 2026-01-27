using UnityEngine;
using System.Collections.Generic;
using SpineTool;

#if SPINE_UNITY
using Spine.Unity;
#endif

namespace SpineTool.Examples
{
    /// <summary>
    /// SpineEventInjector 사용 예제 2: 콤보 시스템
    ///
    /// 여러 공격 애니메이션에 각각 다른 이벤트를 주입하는 예제
    /// </summary>
    [InjectSpineEvent("attack1", "OnComboHit", 0.6f, IntParameter = 10, StringParameter = "light")]
    [InjectSpineEvent("attack2", "OnComboHit", 0.5f, IntParameter = 15, StringParameter = "medium")]
    [InjectSpineEvent("attack3", "OnComboHit", 0.7f, IntParameter = 30, StringParameter = "heavy")]
    [InjectSpineEvent("attack3", "OnFinisherEffect", 0.8f)] // 피니셔 추가 이펙트
    public class SpineComboSystemExample : MonoBehaviour
    {
        [Header("Combo Settings")]
        [SerializeField] private float comboTimeWindow = 1.0f;

        [Header("VFX")]
        [SerializeField] private GameObject lightHitEffect;
        [SerializeField] private GameObject mediumHitEffect;
        [SerializeField] private GameObject heavyHitEffect;
        [SerializeField] private GameObject finisherEffect;

#if SPINE_UNITY
        private SkeletonAnimation skeletonAnimation;
        private int currentComboStep = 0;
        private float lastAttackTime = 0f;

        private Dictionary<string, string> comboAnimations = new Dictionary<string, string>
        {
            { "attack1", "attack1" },
            { "attack2", "attack2" },
            { "attack3", "attack3" }
        };

        private void Awake()
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        private void Update()
        {
            // 마우스 클릭으로 콤보 진행
            if (Input.GetMouseButtonDown(0))
            {
                PerformNextCombo();
            }

            // 콤보 타임 아웃 체크
            if (Time.time - lastAttackTime > comboTimeWindow)
            {
                ResetCombo();
            }
        }

        // ===== 콤보 시스템 =====

        private void PerformNextCombo()
        {
            // 콤보 스텝 진행
            currentComboStep = (currentComboStep % 3) + 1;
            lastAttackTime = Time.time;

            // 애니메이션 재생
            string animationName = $"attack{currentComboStep}";
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, animationName, false);
                Debug.Log($"<color=cyan>Combo Step {currentComboStep}: {animationName}</color>");
            }
        }

        private void ResetCombo()
        {
            if (currentComboStep != 0)
            {
                currentComboStep = 0;
                Debug.Log("<color=yellow>Combo reset</color>");
            }
        }

        // ===== 주입된 이벤트 핸들러 =====

        /// <summary>
        /// 모든 콤보 공격에서 호출됨
        /// StringParameter로 공격 타입 구분
        /// IntParameter로 데미지 값 전달
        /// </summary>
        private void OnComboHit(SpineEventData data)
        {
            int damage = data.IntParameter;
            string attackType = data.StringParameter;

            Debug.Log($"<color=red>Combo Hit!</color> Type: {attackType}, Damage: {damage}");

            // 공격 타입에 따른 이펙트
            GameObject effectPrefab = null;
            switch (attackType)
            {
                case "light":
                    effectPrefab = lightHitEffect;
                    break;
                case "medium":
                    effectPrefab = mediumHitEffect;
                    break;
                case "heavy":
                    effectPrefab = heavyHitEffect;
                    break;
            }

            if (effectPrefab != null)
            {
                SpawnEffect(effectPrefab);
            }

            // 카메라 쉐이크 강도도 데미지에 비례
            float shakeIntensity = damage * 0.01f;
            Debug.Log($"Camera shake intensity: {shakeIntensity}");
            // CameraShake.Instance?.Shake(shakeIntensity, 0.2f);
        }

        /// <summary>
        /// Attack3(피니셔)에서만 호출되는 추가 이펙트
        /// </summary>
        private void OnFinisherEffect(SpineEventData data)
        {
            Debug.Log("<color=magenta>★★★ FINISHER EFFECT! ★★★</color>");

            if (finisherEffect != null)
            {
                SpawnEffect(finisherEffect);
            }

            // 강력한 화면 효과
            // CameraShake.Instance?.Shake(0.5f, 0.3f);
            // TimeManager.Instance?.SlowMotion(0.3f, 0.5f);
        }

        // ===== Helper Methods =====

        private void SpawnEffect(GameObject effectPrefab)
        {
            Vector3 spawnPos = transform.position + Vector3.right * 1.0f + Vector3.up * 1.0f;
            GameObject effect = Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // ===== 디버그 GUI =====

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Box("Spine Combo System Example");

            GUILayout.Label($"Current Combo Step: {currentComboStep}/3");
            GUILayout.Label($"Time Left: {Mathf.Max(0, comboTimeWindow - (Time.time - lastAttackTime)):F1}s");

            if (GUILayout.Button("Perform Next Combo (or Click Mouse)"))
            {
                PerformNextCombo();
            }

            if (GUILayout.Button("Reset Combo"))
            {
                ResetCombo();
            }

            GUILayout.EndArea();
        }
#endif
    }
}
