using UnityEngine;
using SpineTool;

namespace SpineTool.Examples
{
    /// <summary>
    /// SpineAnimSyncModule 사용 예제
    ///
    /// 구조: 샘플 코드(설정) → SpineAnimSyncModule → 기능 작동
    ///
    /// 사용 예시:
    /// - 처형 모션 (공격자 + 피해자)
    /// - 보물상자 열기 (캐릭터 + 상자)
    /// - 그래플 기술 (캐릭터 + 적)
    /// </summary>
    public class SpineAnimSyncExample : MonoBehaviour
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 1단계: 샘플 코드 (설정)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("Sync Module")]
        [SerializeField] private SpineAnimSyncModule syncModule; // ← 모듈

        [Header("Animation Names")]
        [SerializeField] private string masterAnimationName = "execute_attack";
        [SerializeField] private string slaveAnimationName = "execute_victim";

        [Header("Test Target (Optional)")]
        [SerializeField] private SpineAnimModule targetCharacter; // 테스트용 타겟

        void Awake()
        {
            // 모듈 가져오기
            if (syncModule == null)
            {
                syncModule = GetComponent<SpineAnimSyncModule>();
            }
        }

        void Start()
        {
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // 샘플 코드: 이벤트 콜백 등록
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

            syncModule.OnSyncStarted += OnSyncStarted;
            syncModule.OnSyncCompleted += OnSyncCompleted;

            // ↓ SpineAnimSyncModule이 처리
            // ↓ 동기화 시작/완료 시 콜백 호출
        }

        void Update()
        {
            // Space 키로 동기화 테스트
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PerformSyncAnimation();
            }

            // E 키로 중지
            if (Input.GetKeyDown(KeyCode.E))
            {
                StopSyncAnimation();
            }
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 샘플 코드: 동기화 실행
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        void PerformSyncAnimation()
        {
            // 타겟 설정 (런타임)
            if (targetCharacter != null)
            {
                syncModule.SetSlave(targetCharacter);
            }

            // 동기화 시작
            syncModule.StartSync(
                masterAnimationName,  // Master 애니메이션
                slaveAnimationName,   // Slave 애니메이션
                false                 // 반복 여부
            );

            // ↓ SpineAnimSyncModule이 처리
            // ↓ 1. Slave를 Master에 부착
            // ↓ 2. 위치/방향 자동 매칭
            // ↓ 3. 두 애니메이션 동시 재생
            // ↓ 4. 완료 시 자동 분리

            Debug.Log("동기화 애니메이션 실행!");
        }

        void StopSyncAnimation()
        {
            syncModule.StopSync();

            // ↓ SpineAnimSyncModule이 처리
            // ↓ Slave 분리 및 애니메이션 정지

            Debug.Log("동기화 중지!");
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 3단계: 기능 작동 (콜백)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        void OnSyncStarted()
        {
            // ✅ 결과: 동기화 시작됨
            Debug.Log("✅ 동기화 시작!");

            // 실제 기능 구현
            // - 카메라 연출
            // - UI 표시
            // - 사운드 재생
        }

        void OnSyncCompleted()
        {
            // ✅ 결과: 동기화 완료됨
            Debug.Log("✅ 동기화 완료!");

            // 실제 기능 구현
            // - 데미지 적용
            // - 보상 지급
            // - 다음 상태로 전환
            ApplyDamageToTarget();
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 실제 기능 예시
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        void ApplyDamageToTarget()
        {
            Debug.Log("💥 타겟에게 데미지 적용!");
            // 실제 데미지 로직
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 고급 예제: 세밀한 제어
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 예제 1: 오프셋 조절
        /// </summary>
        void AdjustSlaveOffset()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            syncModule.SetSlaveOffset(new Vector3(0.5f, 0f, 0f));
            // ↓ SpineAnimSyncModule이 처리
            // ↓ Slave 위치가 Master 기준 0.5 오른쪽으로 이동
        }

        /// <summary>
        /// 예제 2: 특정 본에 부착
        /// </summary>
        void AttachToHand()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            syncModule.SetAttachBone("hand_R");
            // ↓ SpineAnimSyncModule이 처리
            // ↓ Slave가 Master의 오른손 본에 부착됨
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // GUI (테스트용)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 500, 400));

            GUILayout.Box("SpineAnimSyncModule 사용 예제");
            GUILayout.Label("구조: 샘플코드(설정) → 모듈 → 기능작동");

            GUILayout.Space(10);
            GUILayout.Label("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // 예제 1: 동기화 실행
            GUILayout.Label("▼ 예제 1: 동기화 애니메이션 실행");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("StartSync(master, slave)", GUILayout.Width(200)))
            {
                PerformSyncAnimation();
                Debug.Log("→ SpineAnimSyncModule이 처리");
                Debug.Log("→ 두 애니메이션 동기화됨");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("StopSync()", GUILayout.Width(200)))
            {
                StopSyncAnimation();
                Debug.Log("→ SpineAnimSyncModule이 처리");
                Debug.Log("→ 동기화 중지됨");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 예제 2: 오프셋 조절
            GUILayout.Label("▼ 예제 2: Slave 위치 조절");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("SetSlaveOffset(0.5, 0, 0)", GUILayout.Width(200)))
            {
                AdjustSlaveOffset();
                Debug.Log("→ SpineAnimSyncModule이 처리");
                Debug.Log("→ Slave 위치 변경됨");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 현재 상태
            GUILayout.Label("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            if (syncModule != null)
            {
                GUILayout.Label($"동기화 중: {syncModule.IsSyncing}");
                GUILayout.Label($"Master Anim: {syncModule.CurrentMasterAnimation}");
                GUILayout.Label($"Slave Anim: {syncModule.CurrentSlaveAnimation}");
            }

            GUILayout.Space(10);
            GUILayout.Label("💡 사용 예시:");
            GUILayout.Label("   - 처형 모션 (공격자 + 피해자)");
            GUILayout.Label("   - 보물상자 열기 (캐릭터 + 상자)");
            GUILayout.Label("   - 그래플 기술 (캐릭터 + 적)");

            GUILayout.Space(10);
            GUILayout.Label("키보드 단축키:");
            GUILayout.Label("  Space - 동기화 시작");
            GUILayout.Label("  E - 동기화 중지");

            GUILayout.EndArea();
        }
    }
}
