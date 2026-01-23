#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if SPINE_UNITY
using Spine;
using Spine.Unity;
using Spine.Unity.Editor;
using Animation = Spine.Animation;
using AnimationState = Spine.AnimationState;
#endif

namespace SpineTool.Editor
{
    /// <summary>
    /// Spine 애니메이션을 에디터 모드에서 미리보기하는 윈도우
    /// 플레이 모드 없이 애니메이션 재생 가능
    /// 여러 SkeletonAnimation 동시 제어 지원
    /// </summary>
    public class SpineAnimationPreviewWindow : EditorWindow
    {
        [MenuItem("Tools/SpineTool/Animation Preview Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpineAnimationPreviewWindow>("Spine Animation Preview");
            window.minSize = new Vector2(300, 200);
            window.Show();
        }

#if SPINE_UNITY
        private static bool showAnimationList = true;
        private GUIStyle activePlayButtonStyle;
        private GUIStyle idlePlayButtonStyle;

        private float editorDeltaTime;
        private double lastTimeSinceStartup;

        private List<SkeletonAnimation> selectedSkeletons = new List<SkeletonAnimation>();
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Selection.selectionChanged += OnSelectionChanged;
            lastTimeSinceStartup = EditorApplication.timeSinceStartup;

            // 현재 선택된 오브젝트 확인
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            selectedSkeletons.Clear();

            // 선택된 모든 GameObject에서 SkeletonAnimation 찾기
            foreach (GameObject obj in Selection.gameObjects)
            {
                if (obj != null)
                {
                    SkeletonAnimation skeleton = obj.GetComponent<SkeletonAnimation>();
                    if (skeleton != null && skeleton.SkeletonDataAsset != null)
                    {
                        selectedSkeletons.Add(skeleton);
                    }
                }
            }

            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (selectedSkeletons.Count == 0) return;

            // deltaTime 계산
            double currentTime = EditorApplication.timeSinceStartup;
            editorDeltaTime = (float)(currentTime - lastTimeSinceStartup);
            lastTimeSinceStartup = currentTime;

            // 선택된 모든 스켈레톤 업데이트
            bool needsRepaint = false;
            foreach (var skeleton in selectedSkeletons)
            {
                if (skeleton != null && skeleton.SkeletonDataAsset != null)
                {
                    skeleton.Update(editorDeltaTime);

                    // Transform을 Dirty로 마킹해서 씬 뷰 강제 업데이트
                    EditorUtility.SetDirty(skeleton.transform);
                    needsRepaint = true;
                }
            }

            if (needsRepaint)
            {
                // 씬 뷰 강제 갱신
                SceneView.RepaintAll();
            }
        }

        private void OnGUI()
        {
            // 스타일 초기화
            InitializeStyles();

            EditorGUILayout.LabelField("Spine Animation Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 선택된 오브젝트 확인
            if (selectedSkeletons.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "SkeletonAnimation 컴포넌트가 있는 GameObject를 선택하세요.\n" +
                    "여러 개를 동시에 선택하면 함께 제어할 수 있습니다.",
                    MessageType.Info
                );
                return;
            }

            // 선택된 스켈레톤 정보 표시
            EditorGUILayout.LabelField($"선택된 오브젝트: {selectedSkeletons.Count}개", EditorStyles.helpBox);
            EditorGUILayout.Space();

            // 스크롤 뷰 시작
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 각 스켈레톤별로 UI 표시
            for (int i = 0; i < selectedSkeletons.Count; i++)
            {
                var skeleton = selectedSkeletons[i];
                if (skeleton == null || skeleton.SkeletonDataAsset == null)
                {
                    continue;
                }

                DrawSkeletonUI(skeleton, i);

                if (i < selectedSkeletons.Count - 1)
                {
                    EditorGUILayout.Space(10);
                    DrawSeparator();
                    EditorGUILayout.Space(10);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSkeletonUI(SkeletonAnimation skeleton, int index)
        {
            SkeletonData skeletonData = skeleton.SkeletonDataAsset.GetSkeletonData(false);
            if (skeletonData == null) return;

            // 헤더
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"[{index + 1}] {skeleton.name}", EditorStyles.boldLabel);

            // Setup Pose 버튼
            if (GUILayout.Button("Setup Pose", GUILayout.Height(25)))
            {
                SetupPose(skeleton);
            }

            EditorGUILayout.Space();

            // 애니메이션 리스트
            DrawAnimationList(skeleton, skeletonData);

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationList(SkeletonAnimation skeleton, SkeletonData skeletonData)
        {
            showAnimationList = EditorGUILayout.Foldout(
                showAnimationList,
                $"Animations [{skeletonData.Animations.Count}]",
                true
            );

            if (!showAnimationList) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Duration", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            TrackEntry activeTrack = GetActiveTrack(skeleton);

            foreach (Animation animation in skeletonData.Animations)
            {
                EditorGUILayout.BeginHorizontal();

                // 재생 버튼
                bool isActive = activeTrack != null && activeTrack.Animation == animation;
                bool isPlaying = isActive && activeTrack.TimeScale > 0f;

                GUIStyle buttonStyle = isPlaying ? activePlayButtonStyle : idlePlayButtonStyle;
                string buttonLabel = isPlaying ? "■" : "▶";

                if (GUILayout.Button(buttonLabel, buttonStyle, GUILayout.Width(30)))
                {
                    PlayPauseAnimation(skeleton, animation.Name, true);
                }

                // 애니메이션 이름
                EditorGUILayout.LabelField(animation.Name, GUILayout.Width(150));

                // Duration
                EditorGUILayout.LabelField($"{animation.Duration:F2}s", GUILayout.Width(80));

                // 타임라인 수
                EditorGUILayout.LabelField($"({animation.Timelines.Count} timelines)", EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();
            }
        }

        private void InitializeStyles()
        {
            if (idlePlayButtonStyle == null)
            {
                idlePlayButtonStyle = new GUIStyle(EditorStyles.miniButton);
            }

            if (activePlayButtonStyle == null)
            {
                activePlayButtonStyle = new GUIStyle(idlePlayButtonStyle);
                activePlayButtonStyle.normal.textColor = Color.red;
                activePlayButtonStyle.fontStyle = FontStyle.Bold;
            }
        }

        private TrackEntry GetActiveTrack(SkeletonAnimation skeleton)
        {
            if (skeleton != null && skeleton.valid && skeleton.AnimationState != null)
            {
                return skeleton.AnimationState.GetCurrent(0);
            }
            return null;
        }

        private void SetupPose(SkeletonAnimation skeleton)
        {
            if (skeleton == null || !skeleton.valid) return;

            skeleton.AnimationState.ClearTracks();
            skeleton.Skeleton.SetToSetupPose();

            EditorUtility.SetDirty(skeleton);
            SceneView.RepaintAll();
        }

        private void PlayPauseAnimation(SkeletonAnimation skeleton, string animationName, bool loop)
        {
            if (skeleton == null || !skeleton.valid) return;

            SkeletonData skeletonData = skeleton.SkeletonDataAsset.GetSkeletonData(false);
            if (skeletonData == null) return;

            Animation targetAnimation = skeletonData.FindAnimation(animationName);
            if (targetAnimation == null)
            {
                Debug.LogWarning($"[SpineAnimationPreview] Animation '{animationName}' not found!");
                return;
            }

            TrackEntry currentTrack = GetActiveTrack(skeleton);
            AnimationState animationState = skeleton.AnimationState;
            Skeleton skeletonObj = skeleton.Skeleton;

            if (currentTrack == null)
            {
                // 트랙이 비어있으면 새로 재생
                skeletonObj.SetToSetupPose();
                animationState.SetAnimation(0, targetAnimation, loop);
            }
            else
            {
                bool isSameAnimation = (currentTrack.Animation == targetAnimation);

                if (isSameAnimation)
                {
                    // 같은 애니메이션이면 일시정지/재생 토글
                    currentTrack.TimeScale = (currentTrack.TimeScale == 0) ? 1f : 0f;
                }
                else
                {
                    // 다른 애니메이션이면 새로 재생
                    currentTrack.TimeScale = 1f;
                    animationState.SetAnimation(0, targetAnimation, loop);
                }
            }

            EditorUtility.SetDirty(skeleton);
            SceneView.RepaintAll();
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

#else
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Spine-Unity Runtime이 설치되지 않았거나 SPINE_UNITY 심볼이 정의되지 않았습니다.\n\n" +
                "Spine-Unity Runtime을 임포트한 후 사용하세요.",
                MessageType.Warning
            );
        }
#endif
    }
}
#endif
