#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if SPINE_UNITY
using Spine;
using Spine.Unity;
#endif

namespace SpineTool.Editor
{
    /// <summary>
    /// SkeletonAnimation의 모든 파라미터를 한눈에 볼 수 있는 Inspector 윈도우
    ///
    /// 표시 정보:
    /// - Bones (본 구조)
    /// - Slots (슬롯 정보)
    /// - Skins (스킨 목록)
    /// - Animations (애니메이션 목록)
    /// - Events (이벤트 목록)
    /// - IK Constraints (IK 제약 조건)
    /// - Transform Constraints
    /// - Path Constraints
    /// </summary>
    public class SpineSkeletonInspectorWindow : EditorWindow
    {
        [MenuItem("Tools/lLcrowe/SpineTool/Skeleton Inspector")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpineSkeletonInspectorWindow>("Skeleton Inspector");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

#if SPINE_UNITY
        private SkeletonAnimation selectedSkeleton;
        private Vector2 scrollPosition;

        // Foldout 상태
        private bool showBones = false;
        private bool showSlots = false;
        private bool showSkins = true;
        private bool showAnimations = true;
        private bool showEvents = false;
        private bool showIKConstraints = true;
        private bool showTransformConstraints = false;
        private bool showPathConstraints = false;

        // 검색
        private string searchFilter = "";

        // 실시간 업데이트
        private bool autoRefresh = true;
        private double lastRefreshTime = 0;
        private float refreshInterval = 0.1f; // 0.1초마다

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.update += OnEditorUpdate;
            OnSelectionChanged();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeGameObject != null)
            {
                SkeletonAnimation skeleton = Selection.activeGameObject.GetComponent<SkeletonAnimation>();
                if (skeleton != null)
                {
                    selectedSkeleton = skeleton;
                    Repaint();
                }
            }
        }

        private void OnEditorUpdate()
        {
            if (autoRefresh && selectedSkeleton != null)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                if (currentTime - lastRefreshTime > refreshInterval)
                {
                    lastRefreshTime = currentTime;
                    Repaint();
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spine Skeleton Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Skeleton 선택
            EditorGUI.BeginChangeCheck();
            selectedSkeleton = (SkeletonAnimation)EditorGUILayout.ObjectField(
                "SkeletonAnimation",
                selectedSkeleton,
                typeof(SkeletonAnimation),
                true
            );
            if (EditorGUI.EndChangeCheck())
            {
                Repaint();
            }

            // 자동 새로고침 토글
            EditorGUILayout.BeginHorizontal();
            autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
            if (!autoRefresh && GUILayout.Button("Refresh Now", GUILayout.Width(100)))
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (selectedSkeleton == null)
            {
                EditorGUILayout.HelpBox(
                    "SkeletonAnimation 컴포넌트를 선택하세요.\n" +
                    "Hierarchy에서 선택하거나 위 필드에 드래그하세요.",
                    MessageType.Info
                );
                return;
            }

            if (selectedSkeleton.Skeleton == null)
            {
                EditorGUILayout.HelpBox("Skeleton이 초기화되지 않았습니다.", MessageType.Warning);
                return;
            }

            // 검색 필터
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 스크롤 시작
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 각 섹션 표시
            DrawSkinsSection();
            DrawAnimationsSection();
            DrawIKConstraintsSection();
            DrawTransformConstraintsSection();
            DrawPathConstraintsSection();
            DrawEventsSection();
            DrawBonesSection();
            DrawSlotsSection();

            EditorGUILayout.EndScrollView();
        }

        #region Skins

        private void DrawSkinsSection()
        {
            var skeletonData = selectedSkeleton.Skeleton.Data;

            showSkins = EditorGUILayout.Foldout(showSkins, $"🎨 Skins ({skeletonData.Skins.Count})", true, EditorStyles.foldoutHeader);
            if (!showSkins) return;

            EditorGUI.indentLevel++;

            string currentSkin = selectedSkeleton.Skeleton.Skin?.Name ?? "default";
            EditorGUILayout.LabelField($"Current Skin: {currentSkin}", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            foreach (var skin in skeletonData.Skins)
            {
                if (!MatchesFilter(skin.Name)) continue;

                EditorGUILayout.BeginHorizontal();

                bool isCurrent = skin.Name == currentSkin;
                if (isCurrent)
                {
                    GUI.backgroundColor = Color.green;
                }

                if (GUILayout.Button(skin.Name, GUILayout.Height(25)))
                {
                    // 스킨 변경
                    selectedSkeleton.Skeleton.SetSkin(skin);
                    selectedSkeleton.Skeleton.SetSlotsToSetupPose();
                    EditorUtility.SetDirty(selectedSkeleton);
                }

                GUI.backgroundColor = Color.white;

                EditorGUILayout.LabelField($"({skin.Attachments.Count} attachments)", GUILayout.Width(150));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

        #region Animations

        private void DrawAnimationsSection()
        {
            var skeletonData = selectedSkeleton.Skeleton.Data;

            showAnimations = EditorGUILayout.Foldout(showAnimations, $"🎬 Animations ({skeletonData.Animations.Count})", true, EditorStyles.foldoutHeader);
            if (!showAnimations) return;

            EditorGUI.indentLevel++;

            // 현재 재생 중인 애니메이션
            TrackEntry currentTrack = selectedSkeleton.AnimationState?.GetCurrent(0);
            string currentAnim = currentTrack?.Animation.Name ?? "None";
            EditorGUILayout.LabelField($"Current: {currentAnim}", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name", EditorStyles.miniLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("Duration", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Timelines", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            foreach (var animation in skeletonData.Animations)
            {
                if (!MatchesFilter(animation.Name)) continue;

                EditorGUILayout.BeginHorizontal();

                bool isCurrent = animation.Name == currentAnim;
                if (isCurrent)
                {
                    GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                }

                if (GUILayout.Button("▶", GUILayout.Width(30)))
                {
                    // 애니메이션 재생
                    selectedSkeleton.AnimationState.SetAnimation(0, animation.Name, true);
                }

                GUI.backgroundColor = Color.white;

                EditorGUILayout.LabelField(animation.Name, GUILayout.Width(150));
                EditorGUILayout.LabelField($"{animation.Duration:F2}s", GUILayout.Width(70));
                EditorGUILayout.LabelField($"{animation.Timelines.Count}", GUILayout.Width(70));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

        #region IK Constraints

        private void DrawIKConstraintsSection()
        {
            var ikConstraints = selectedSkeleton.Skeleton.IkConstraints;

            showIKConstraints = EditorGUILayout.Foldout(showIKConstraints, $"🦴 IK Constraints ({ikConstraints.Count})", true, EditorStyles.foldoutHeader);
            if (!showIKConstraints) return;

            EditorGUI.indentLevel++;

            if (ikConstraints.Count == 0)
            {
                EditorGUILayout.HelpBox("No IK Constraints found.", MessageType.Info);
            }
            else
            {
                // 헤더
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name", EditorStyles.miniLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField("Active", EditorStyles.miniLabel, GUILayout.Width(50));
                EditorGUILayout.LabelField("Weight", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("Target", EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();

                foreach (var ik in ikConstraints)
                {
                    if (!MatchesFilter(ik.Data.Name)) continue;

                    EditorGUILayout.BeginHorizontal();

                    // 이름
                    EditorGUILayout.LabelField(ik.Data.Name, GUILayout.Width(120));

                    // Active
                    Color originalColor = GUI.contentColor;
                    GUI.contentColor = ik.Active ? Color.green : Color.red;
                    EditorGUILayout.LabelField(ik.Active ? "✓" : "✗", GUILayout.Width(50));
                    GUI.contentColor = originalColor;

                    // Weight
                    EditorGUILayout.LabelField($"{ik.Mix:F2}", GUILayout.Width(60));

                    // Target Bone
                    string targetName = ik.Target?.Data.Name ?? "None";
                    EditorGUILayout.LabelField(targetName, GUILayout.Width(100));

                    // 토글 버튼
                    if (GUILayout.Button("Toggle", GUILayout.Width(60)))
                    {
                        ik.Mix = ik.Mix > 0f ? 0f : 1f;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

        #region Transform Constraints

        private void DrawTransformConstraintsSection()
        {
            var constraints = selectedSkeleton.Skeleton.TransformConstraints;

            showTransformConstraints = EditorGUILayout.Foldout(showTransformConstraints, $"↔️ Transform Constraints ({constraints.Count})", true, EditorStyles.foldoutHeader);
            if (!showTransformConstraints) return;

            EditorGUI.indentLevel++;

            if (constraints.Count == 0)
            {
                EditorGUILayout.HelpBox("No Transform Constraints found.", MessageType.Info);
            }
            else
            {
                foreach (var constraint in constraints)
                {
                    if (!MatchesFilter(constraint.Data.Name)) continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(constraint.Data.Name, GUILayout.Width(150));

                    Color originalColor = GUI.contentColor;
                    GUI.contentColor = constraint.Active ? Color.green : Color.red;
                    EditorGUILayout.LabelField(constraint.Active ? "Active" : "Inactive", GUILayout.Width(60));
                    GUI.contentColor = originalColor;

                    EditorGUILayout.LabelField($"Mix: {constraint.MixRotate:F2} / {constraint.MixX:F2} / {constraint.MixY:F2}");

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

        #region Path Constraints

        private void DrawPathConstraintsSection()
        {
            var constraints = selectedSkeleton.Skeleton.PathConstraints;

            showPathConstraints = EditorGUILayout.Foldout(showPathConstraints, $"🛤️ Path Constraints ({constraints.Count})", true, EditorStyles.foldoutHeader);
            if (!showPathConstraints) return;

            EditorGUI.indentLevel++;

            if (constraints.Count == 0)
            {
                EditorGUILayout.HelpBox("No Path Constraints found.", MessageType.Info);
            }
            else
            {
                foreach (var constraint in constraints)
                {
                    if (!MatchesFilter(constraint.Data.Name)) continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(constraint.Data.Name, GUILayout.Width(150));

                    Color originalColor = GUI.contentColor;
                    GUI.contentColor = constraint.Active ? Color.green : Color.red;
                    EditorGUILayout.LabelField(constraint.Active ? "Active" : "Inactive", GUILayout.Width(60));
                    GUI.contentColor = originalColor;

                    EditorGUILayout.LabelField($"Position: {constraint.Position:F2}");

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

        #region Events

        private void DrawEventsSection()
        {
            var skeletonData = selectedSkeleton.Skeleton.Data;

            showEvents = EditorGUILayout.Foldout(showEvents, $"⚡ Events ({skeletonData.Events.Count})", true, EditorStyles.foldoutHeader);
            if (!showEvents) return;

            EditorGUI.indentLevel++;

            if (skeletonData.Events.Count == 0)
            {
                EditorGUILayout.HelpBox("No Events found.", MessageType.Info);
            }
            else
            {
                foreach (var eventData in skeletonData.Events)
                {
                    if (!MatchesFilter(eventData.Name)) continue;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(eventData.Name, EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField($"Int: {eventData.Int}");
                    EditorGUILayout.LabelField($"Float: {eventData.Float}");
                    EditorGUILayout.LabelField($"String: {eventData.String}");
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

        #region Bones

        private void DrawBonesSection()
        {
            var bones = selectedSkeleton.Skeleton.Bones;

            showBones = EditorGUILayout.Foldout(showBones, $"💀 Bones ({bones.Count})", true, EditorStyles.foldoutHeader);
            if (!showBones) return;

            EditorGUI.indentLevel++;

            foreach (var bone in bones)
            {
                if (!MatchesFilter(bone.Data.Name)) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(bone.Data.Name, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField($"Position: ({bone.X:F2}, {bone.Y:F2})");
                EditorGUILayout.LabelField($"Rotation: {bone.Rotation:F2}°");
                EditorGUILayout.LabelField($"Scale: ({bone.ScaleX:F2}, {bone.ScaleY:F2})");

                if (bone.Parent != null)
                {
                    EditorGUILayout.LabelField($"Parent: {bone.Parent.Data.Name}");
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

        #region Slots

        private void DrawSlotsSection()
        {
            var slots = selectedSkeleton.Skeleton.Slots;

            showSlots = EditorGUILayout.Foldout(showSlots, $"📌 Slots ({slots.Count})", true, EditorStyles.foldoutHeader);
            if (!showSlots) return;

            EditorGUI.indentLevel++;

            foreach (var slot in slots)
            {
                if (!MatchesFilter(slot.Data.Name)) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(slot.Data.Name, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField($"Bone: {slot.Bone.Data.Name}");
                EditorGUILayout.LabelField($"Attachment: {slot.Attachment?.Name ?? "None"}");
                EditorGUILayout.LabelField($"Color: ({slot.R:F2}, {slot.G:F2}, {slot.B:F2}, {slot.A:F2})");

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        #endregion

        #region Helpers

        private bool MatchesFilter(string name)
        {
            if (string.IsNullOrEmpty(searchFilter)) return true;
            return name.ToLower().Contains(searchFilter.ToLower());
        }

        #endregion

#else
        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Spine-Unity Runtime이 설치되지 않았거나 SPINE_UNITY 심볼이 정의되지 않았습니다.",
                MessageType.Warning
            );
        }
#endif
    }
}
#endif
