using UnityEngine;
using SpineTool;

namespace SpineTool.Examples
{
    /// <summary>
    /// SpineIKControl 사용 예제
    ///
    /// 구조: 샘플 코드(설정) → SpineIKControl → 기능 작동
    ///
    /// 사용 예시:
    /// - 손으로 오브젝트 잡기
    /// - 발이 지면에 붙도록
    /// - 시선 추적
    /// </summary>
    public class SpineIKExample : MonoBehaviour
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 1단계: 샘플 코드 (설정)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("IK Module")]
        [SerializeField] private SpineIKControl ikModule; // ← 모듈

        [Header("IK Names")]
        [SerializeField] private string handIKName = "hand_IK";
        [SerializeField] private string footIKName = "foot_IK";
        [SerializeField] private string headIKName = "head_IK";

        [Header("Test Settings")]
        [SerializeField] private bool enableIKOnStart = true;

        void Awake()
        {
            // 모듈 가져오기
            if (ikModule == null)
            {
                ikModule = GetComponent<SpineIKControl>();
            }
        }

        void Start()
        {
            if (enableIKOnStart)
            {
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                // 샘플 코드: IK 초기 설정
                // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

                ikModule.SetIKActive(handIKName, true);
                ikModule.SetIKWeight(handIKName, 1.0f);

                // ↓ SpineIKControl이 처리
                // ↓ 손 IK가 활성화되고 가중치가 1.0으로 설정됨
            }
        }

        void Update()
        {
            // 키보드 입력으로 IK 테스트
            HandleInput();
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 샘플 코드 예제들
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        void HandleInput()
        {
            // 1 - 손 IK 토글
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ToggleHandIK();
            }
            // 2 - 발 IK 토글
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ToggleFootIK();
            }
            // 3 - 모든 IK 온
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                EnableAllIK();
            }
            // 4 - 모든 IK 오프
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                DisableAllIK();
            }
            // W - 가중치 올리기
            else if (Input.GetKey(KeyCode.W))
            {
                IncreaseIKWeight();
            }
            // S - 가중치 내리기
            else if (Input.GetKey(KeyCode.S))
            {
                DecreaseIKWeight();
            }
        }


        /// <summary>
        /// 예제 1: 손 IK 토글
        /// </summary>
        void ToggleHandIK()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            bool isActive = ikModule.IsIKActive(handIKName);
            ikModule.SetIKActive(handIKName, !isActive);

            // ↓ SpineIKControl이 처리
            // ↓ 손 IK가 켜지거나 꺼짐

            // ✅ 결과
            Debug.Log($"손 IK {(!isActive ? "활성화" : "비활성화")}");
        }

        /// <summary>
        /// 예제 2: 발 IK 토글
        /// </summary>
        void ToggleFootIK()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            bool isActive = ikModule.IsIKActive(footIKName);
            ikModule.SetIKActive(footIKName, !isActive);

            // ↓ SpineIKControl이 처리
            // ↓ 발 IK가 켜지거나 꺼짐

            // ✅ 결과
            Debug.Log($"발 IK {(!isActive ? "활성화" : "비활성화")}");
        }

        /// <summary>
        /// 예제 3: 모든 IK 활성화
        /// </summary>
        void EnableAllIK()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            ikModule.SetAllIKActive(true);

            // ↓ SpineIKControl이 처리
            // ↓ 모든 IK가 활성화됨

            // ✅ 결과
            Debug.Log("✅ 모든 IK 활성화!");
        }

        /// <summary>
        /// 예제 4: 모든 IK 비활성화
        /// </summary>
        void DisableAllIK()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            ikModule.SetAllIKActive(false);

            // ↓ SpineIKControl이 처리
            // ↓ 모든 IK가 비활성화됨

            // ✅ 결과
            Debug.Log("✅ 모든 IK 비활성화!");
        }

        /// <summary>
        /// 예제 5: 가중치 올리기
        /// </summary>
        void IncreaseIKWeight()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            float currentWeight = ikModule.GetIKWeight(handIKName);
            float newWeight = Mathf.Clamp01(currentWeight + Time.deltaTime);
            ikModule.SetIKWeight(handIKName, newWeight);

            // ↓ SpineIKControl이 처리
            // ↓ 손 IK 가중치가 증가됨
        }

        /// <summary>
        /// 예제 6: 가중치 내리기
        /// </summary>
        void DecreaseIKWeight()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            float currentWeight = ikModule.GetIKWeight(handIKName);
            float newWeight = Mathf.Clamp01(currentWeight - Time.deltaTime);
            ikModule.SetIKWeight(handIKName, newWeight);

            // ↓ SpineIKControl이 처리
            // ↓ 손 IK 가중치가 감소됨
        }

        /// <summary>
        /// 예제 7: 부드럽게 가중치 변경
        /// </summary>
        void SmoothlyEnableIK()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            ikModule.SetIKWeightSmooth(handIKName, 1.0f, 0.5f);

            // ↓ SpineIKControl이 처리
            // ↓ 0.5초 동안 부드럽게 가중치가 1.0으로 변경됨

            Debug.Log("IK 부드럽게 활성화 시작...");
        }

        /// <summary>
        /// 예제 8: 부드럽게 비활성화
        /// </summary>
        void SmoothlyDisableIK()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            ikModule.SetIKWeightSmooth(handIKName, 0.0f, 0.5f);

            // ↓ SpineIKControl이 처리
            // ↓ 0.5초 동안 부드럽게 가중치가 0.0으로 변경됨

            Debug.Log("IK 부드럽게 비활성화 시작...");
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 실제 사용 시나리오
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 시나리오 1: 오브젝트 잡기
        /// </summary>
        public void GrabObject()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            ikModule.SetIKActive(handIKName, true);
            ikModule.SetIKWeightSmooth(handIKName, 1.0f, 0.3f);

            // ↓ SpineIKControl이 처리
            // ↓ 손 IK가 켜지고 0.3초 동안 부드럽게 활성화됨

            // ✅ 결과: 손이 타겟을 향해 뻗어감
            Debug.Log("🤚 오브젝트 잡기 시작!");
        }

        /// <summary>
        /// 시나리오 2: 오브젝트 놓기
        /// </summary>
        public void ReleaseObject()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            ikModule.SetIKWeightSmooth(handIKName, 0.0f, 0.3f);

            // ↓ SpineIKControl이 처리
            // ↓ 0.3초 동안 부드럽게 IK가 꺼짐

            // ✅ 결과: 손이 원래 자세로 돌아감
            Debug.Log("👋 오브젝트 놓기!");
        }

        /// <summary>
        /// 시나리오 3: 지면에 발 고정
        /// </summary>
        public void StandOnGround()
        {
            // ━━━━━ 샘플 코드 ━━━━━
            ikModule.SetIKActive(footIKName, true);
            ikModule.SetIKWeight(footIKName, 1.0f);

            // ↓ SpineIKControl이 처리
            // ↓ 발 IK가 활성화되어 지면에 붙음

            // ✅ 결과: 발이 지면에 정확히 착지
            Debug.Log("👣 발이 지면에 고정됨!");
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // GUI (테스트용)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 500, 600));

            GUILayout.Box("SpineIKControl 사용 예제");
            GUILayout.Label("구조: 샘플코드(설정) → 모듈 → 기능작동");

            GUILayout.Space(10);
            GUILayout.Label("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // 예제 1: 개별 IK 제어
            GUILayout.Label("▼ 예제 1: 개별 IK 제어");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("Toggle Hand IK", GUILayout.Width(150)))
            {
                ToggleHandIK();
                Debug.Log("→ SpineIKControl이 처리 → 손 IK 토글됨");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("Toggle Foot IK", GUILayout.Width(150)))
            {
                ToggleFootIK();
                Debug.Log("→ SpineIKControl이 처리 → 발 IK 토글됨");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 예제 2: 전체 IK 제어
            GUILayout.Label("▼ 예제 2: 전체 IK 제어");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("Enable All IK", GUILayout.Width(150)))
            {
                EnableAllIK();
                Debug.Log("→ SpineIKControl이 처리 → 모든 IK 활성화");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("Disable All IK", GUILayout.Width(150)))
            {
                DisableAllIK();
                Debug.Log("→ SpineIKControl이 처리 → 모든 IK 비활성화");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 예제 3: 부드러운 전환
            GUILayout.Label("▼ 예제 3: 부드러운 전환");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("Smooth Enable", GUILayout.Width(150)))
            {
                SmoothlyEnableIK();
                Debug.Log("→ SpineIKControl이 처리 → 부드럽게 활성화");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("Smooth Disable", GUILayout.Width(150)))
            {
                SmoothlyDisableIK();
                Debug.Log("→ SpineIKControl이 처리 → 부드럽게 비활성화");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 예제 4: 실제 시나리오
            GUILayout.Label("▼ 예제 4: 실제 시나리오");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("🤚 Grab Object", GUILayout.Width(150)))
            {
                GrabObject();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("👋 Release Object", GUILayout.Width(150)))
            {
                ReleaseObject();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 현재 상태
            GUILayout.Label("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            if (ikModule != null)
            {
                GUILayout.Label($"Hand IK: {ikModule.IsIKActive(handIKName)} (Weight: {ikModule.GetIKWeight(handIKName):F2})");
                GUILayout.Label($"Foot IK: {ikModule.IsIKActive(footIKName)} (Weight: {ikModule.GetIKWeight(footIKName):F2})");
            }

            GUILayout.Space(10);
            GUILayout.Label("💡 사용 예시:");
            GUILayout.Label("   - 손으로 오브젝트 잡기");
            GUILayout.Label("   - 발이 지면에 붙도록");
            GUILayout.Label("   - 시선 추적");

            GUILayout.Space(10);
            GUILayout.Label("키보드 단축키:");
            GUILayout.Label("  1 - 손 IK 토글");
            GUILayout.Label("  2 - 발 IK 토글");
            GUILayout.Label("  3 - 모든 IK 온");
            GUILayout.Label("  4 - 모든 IK 오프");
            GUILayout.Label("  W - 가중치 올리기");
            GUILayout.Label("  S - 가중치 내리기");

            GUILayout.EndArea();
        }
    }
}
