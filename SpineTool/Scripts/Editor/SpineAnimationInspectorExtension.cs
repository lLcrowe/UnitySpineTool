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
    /// SkeletonAnimation 인스펙터 확장
    /// Spine 기본 인스펙터는 유지하면서 애니메이션 프리뷰 기능 추가
    /// </summary>
#if SPINE_UNITY
    [CustomEditor(typeof(SkeletonAnimation))]
    [CanEditMultipleObjects]
    public class SpineAnimationInspectorExtension : SkeletonAnimationInspector
    {
        private static bool showPreviewControls = true;
        private GUIStyle activePlayButtonStyle;
        private GUIStyle idlePlayButtonStyle;

        private float editorDeltaTime;
        private double lastTimeSinceStartup;

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            lastTimeSinceStartup = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // deltaTime 계산
            double currentTime = EditorApplication.timeSinceStartup;
            editorDeltaTime = (float)(currentTime - lastTimeSinceStartup);
            lastTimeSinceStartup = currentTime;

            // 선택된 모든 타겟 업데이트
            bool needsRepaint = false;
            foreach (Object obj in targets)
            {
                SkeletonAnimation skeleton = obj as SkeletonAnimation;
                if (skeleton != null && skeleton.SkeletonDataAsset != null)
                {
                    skeleton.Update(editorDeltaTime);
                    EditorUtility.SetDirty(skeleton.transform);
                    needsRepaint = true;
                }
            }

            if (needsRepaint)
            {
                SceneView.RepaintAll();
            }
        }

        public override void OnInspectorGUI()
        {
            // Spine 기본 인스펙터 그리기
            base.OnInspectorGUI();

            // 구분선
            EditorGUILayout.Space(10);
            DrawSeparator();
            EditorGUILayout.Space(5);

            // 확장 기능: 애니메이션 프리뷰 컨트롤
            DrawAnimationPreviewControls();
        }

        private void DrawAnimationPreviewControls()
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showPreviewControls = EditorGUILayout.Foldout(
                showPreviewControls,
                "🎬 Animation Preview (Editor Mode)",
                true,
                EditorStyles.foldoutHeader
            );

            if (!showPreviewControls)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(5);

            // 선택된 첫 번째 타겟으로 애니메이션 리스트 표시
            SkeletonAnimation firstSkeleton = target as SkeletonAnimation;
            if (firstSkeleton != null && firstSkeleton.SkeletonDataAsset != null)
            {
                SkeletonData skeletonData = firstSkeleton.SkeletonDataAsset.GetSkeletonData(false);
                if (skeletonData != null)
                {
                    DrawAnimationControls(skeletonData);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("SkeletonDataAsset이 설정되지 않았습니다.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAnimationControls(SkeletonData skeletonData)
        {
            // Setup Pose 버튼
            if (GUILayout.Button("🔄 Setup Pose (모든 선택된 오브젝트)", GUILayout.Height(30)))
            {
                foreach (Object obj in targets)
                {
                    SkeletonAnimation skeleton = obj as SkeletonAnimation;
                    if (skeleton != null)
                    {
                        SetupPose(skeleton);
                    }
                }
            }

            EditorGUILayout.Space(5);

            // 애니메이션 리스트
            EditorGUILayout.LabelField($"Animations: {skeletonData.Animations.Count}개", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    $"{targets.Length}개의 오브젝트가 선택됨 - 애니메이션 재생 시 모두 동시 재생됩니다.",
                    MessageType.Info
                );
            }

            // 헤더
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(35);
            EditorGUILayout.LabelField("Name", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Duration", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Info", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // 첫 번째 타겟의 상태 확인
            SkeletonAnimation firstTarget = target as SkeletonAnimation;
            TrackEntry activeTrack = GetActiveTrack(firstTarget);

            // 애니메이션 목록
            foreach (Animation animation in skeletonData.Animations)
            {
                DrawAnimationButton(animation, activeTrack);
            }
        }

        private void DrawAnimationButton(Animation animation, TrackEntry activeTrack)
        {
            EditorGUILayout.BeginHorizontal();

            // 재생 상태 확인
            bool isActive = activeTrack != null && activeTrack.Animation == animation;
            bool isPlaying = isActive && activeTrack.TimeScale > 0f;

            // 재생 버튼
            GUIStyle buttonStyle = isPlaying ? activePlayButtonStyle : idlePlayButtonStyle;
            string buttonLabel = isPlaying ? "■" : "▶";

            Color originalColor = GUI.backgroundColor;
            if (isPlaying)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            }

            if (GUILayout.Button(buttonLabel, buttonStyle, GUILayout.Width(30), GUILayout.Height(20)))
            {
                // 선택된 모든 오브젝트에 애니메이션 적용
                foreach (Object obj in targets)
                {
                    SkeletonAnimation skeleton = obj as SkeletonAnimation;
                    if (skeleton != null)
                    {
                        PlayPauseAnimation(skeleton, animation.Name, true);
                    }
                }
            }

            GUI.backgroundColor = originalColor;

            // 애니메이션 이름
            GUIStyle labelStyle = isActive ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUILayout.LabelField(animation.Name, labelStyle, GUILayout.Width(120));

            // Duration
            EditorGUILayout.LabelField($"{animation.Duration:F2}s", GUILayout.Width(60));

            // 타임라인 정보
            EditorGUILayout.LabelField(
                $"{animation.Timelines.Count} timelines",
                EditorStyles.miniLabel
            );

            EditorGUILayout.EndHorizontal();
        }

        private void InitializeStyles()
        {
            if (idlePlayButtonStyle == null)
            {
                idlePlayButtonStyle = new GUIStyle(EditorStyles.miniButton);
                idlePlayButtonStyle.fontSize = 12;
            }

            if (activePlayButtonStyle == null)
            {
                activePlayButtonStyle = new GUIStyle(idlePlayButtonStyle);
                activePlayButtonStyle.normal.textColor = Color.white;
                activePlayButtonStyle.fontStyle = FontStyle.Bold;
                activePlayButtonStyle.fontSize = 12;
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
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 2);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }
#endif
}
#endif
