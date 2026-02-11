using UnityEngine;
using SpineVAT;

namespace SpineVAT.Examples
{
    /// <summary>
    /// VAT 기반 대규모 적 스폰 예제.
    ///
    /// 기존 방식:
    ///   Instantiate(enemyPrefab, pos, rot);  // 개당 SkeletonAnimation + MeshRenderer = CPU 폭탄
    ///
    /// VAT 방식:
    ///   SpineVatRenderer.Instance.AddUnit(pos);  // struct 추가 + GPU 인스턴싱 = 1 Draw Call
    /// </summary>
    public class VatEnemySpawnerExample : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int spawnCount = 100;
        [SerializeField] private float spawnRadius = 20f;
        [SerializeField] private int walkClipIndex = 0;
        [SerializeField] private int attackClipIndex = 1;

        private int[] unitIndices;

        void Start()
        {
            var renderer = SpineVatRenderer.Instance;
            if (renderer == null)
            {
                Debug.LogError("SpineVatRenderer가 씬에 없습니다!");
                return;
            }

            // 이벤트 구독 (사운드/이펙트 연결)
            renderer.OnVatEvent += HandleVatEvent;

            // 대량 스폰
            unitIndices = new int[spawnCount];
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
                pos.y = 0f;

                float randomSpeed = Random.Range(0.8f, 1.2f);
                unitIndices[i] = renderer.AddUnit(
                    pos,
                    Quaternion.identity,
                    Vector3.one,
                    walkClipIndex,
                    loop: true,
                    speed: randomSpeed
                );
            }

            Debug.Log($"[VatEnemySpawner] {spawnCount}마리 스폰 완료! (1 Draw Call)");
        }

        /// <summary>
        /// VAT 이벤트 핸들러.
        /// Baker가 구워넣은 Spine 이벤트(발소리, 공격 타이밍 등)가 정확한 시점에 도착한다.
        /// </summary>
        private void HandleVatEvent(int unitIndex, string eventName)
        {
            switch (eventName)
            {
                case "footstep":
                    // AudioManager.PlayAtPosition("footstep", GetUnitPosition(unitIndex));
                    break;
                case "attack_hit":
                    // DamageSystem.DealDamage(unitIndex, ...);
                    break;
            }
        }

        /// <summary>
        /// 전체 유닛의 애니메이션을 일괄 변경하는 예시.
        /// </summary>
        public void CommandAllAttack()
        {
            if (unitIndices == null) return;

            var renderer = SpineVatRenderer.Instance;
            if (renderer == null) return;

            for (int i = 0; i < unitIndices.Length; i++)
            {
                renderer.SetUnitClip(unitIndices[i], attackClipIndex);
            }

            Debug.Log($"[VatEnemySpawner] {unitIndices.Length}마리 공격 명령!");
        }

        private void OnDestroy()
        {
            var renderer = SpineVatRenderer.Instance;
            if (renderer == null) return;
            renderer.OnVatEvent -= HandleVatEvent;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // GUI (테스트용)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, 200));

            GUILayout.Box("VAT Enemy Spawner");

            var renderer = SpineVatRenderer.Instance;
            if (renderer != null)
            {
                GUILayout.Label($"Active Units: {renderer.ActiveUnitCount}");
                GUILayout.Label($"Total Units: {renderer.TotalUnitCount}");
            }

            GUILayout.Space(8);

            if (GUILayout.Button("Command: All Attack!", GUILayout.Height(30)))
            {
                CommandAllAttack();
            }

            GUILayout.EndArea();
        }
    }
}
