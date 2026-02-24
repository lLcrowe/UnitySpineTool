#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SpineVAT.Editor
{
    /// <summary>
    /// VAT 애니메이션을 에디터 모드에서 미리보기하는 윈도우.
    /// SpineAnimationPreviewWindow와 동일한 사용감.
    ///
    /// - SpineVatAnimModule이 있는 GameObject 여러 개 선택 가능
    /// - 클립별 재생/정지/일시정지
    /// - 에디터 모드에서 실시간 DrawMeshInstanced 렌더링
    /// - 이벤트 타이밍 표시
    /// </summary>
    public class SpineVatPreviewWindow : EditorWindow
    {
        [MenuItem("Tools/lLcrowe/SpineTool/SpineVAT/VAT Animation Preview")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpineVatPreviewWindow>("VAT Animation Preview");
            window.minSize = new Vector2(350, 250);
            window.Show();
        }

        // 선택된 모듈 목록
        private List<SpineVatAnimModule> selectedModules = new List<SpineVatAnimModule>();

        // 에디터 프리뷰 상태 (모듈 인덱스 → 프리뷰 상태)
        private Dictionary<int, PreviewState> previewStates = new Dictionary<int, PreviewState>();

        // 에디터 deltaTime
        private double lastTimeSinceStartup;
        private float editorDeltaTime;

        // UI
        private Vector2 scrollPosition;
        private GUIStyle activeButtonStyle;
        private GUIStyle idleButtonStyle;
        private GUIStyle headerStyle;
        private GUIStyle eventLabelStyle;

        // 씬 렌더링용
        private MaterialPropertyBlock propertyBlock;
        private Matrix4x4[] singleMatrix = new Matrix4x4[1];
        private float[] singleAnimTime = new float[1];
        private float[] singleFrameOffset = new float[1];
        private float[] singleFrameCount = new float[1];

        private static readonly int PropAnimTime = Shader.PropertyToID("_AnimTime");
        private static readonly int PropFrameOffset = Shader.PropertyToID("_FrameOffset");
        private static readonly int PropFrameCount = Shader.PropertyToID("_FrameCount");
        private static readonly int PropTotalFrames = Shader.PropertyToID("_TotalFrames");
        private static readonly int PropVertexCount = Shader.PropertyToID("_VertexCount");
        private static readonly int PropVatPositionTex = Shader.PropertyToID("_VatPositionTex");

        private struct PreviewState
        {
            public int clipIndex;     // -1 = 정지
            public float normTime;    // 0 ~ 1
            public bool playing;
            public float speed;
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            lastTimeSinceStartup = EditorApplication.timeSinceStartup;
            propertyBlock = new MaterialPropertyBlock();

            OnSelectionChanged();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Selection.selectionChanged -= OnSelectionChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 선택 변경
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void OnSelectionChanged()
        {
            selectedModules.Clear();

            foreach (var go in Selection.gameObjects)
            {
                if (go == null) continue;
                var module = go.GetComponent<SpineVatAnimModule>();
                if (module == null) continue;
                selectedModules.Add(module);
            }

            Repaint();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 에디터 업데이트 (애니메이션 시간 갱신)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void OnEditorUpdate()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            editorDeltaTime = (float)(currentTime - lastTimeSinceStartup);
            lastTimeSinceStartup = currentTime;

            if (selectedModules.Count == 0) return;

            bool needsRepaint = false;

            foreach (var kvp in new Dictionary<int, PreviewState>(previewStates))
            {
                var state = kvp.Value;
                if (!state.playing) continue;
                if (state.clipIndex < 0) continue;

                state.normTime += editorDeltaTime * state.speed / GetClipDuration(kvp.Key, state.clipIndex);
                if (state.normTime >= 1f) state.normTime -= 1f;

                previewStates[kvp.Key] = state;
                needsRepaint = true;
            }

            if (needsRepaint)
            {
                SceneView.RepaintAll();
                Repaint();
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 씬 뷰 렌더링
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void OnSceneGUI(SceneView sceneView)
        {
            for (int i = 0; i < selectedModules.Count; i++)
            {
                var module = selectedModules[i];
                if (module == null) continue;

                int id = module.GetInstanceID();
                if (!previewStates.TryGetValue(id, out var state)) continue;
                if (state.clipIndex < 0) continue;

                DrawPreviewMesh(module, state);
            }
        }

        private void DrawPreviewMesh(SpineVatAnimModule module, PreviewState state)
        {
            // vatData 리플렉션으로 접근 (private SerializeField)
            var vatDataField = typeof(SpineVatAnimModule).GetField("vatData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (vatDataField == null) return;

            var vatData = vatDataField.GetValue(module) as SpineVatData;
            if (vatData == null) return;
            if (vatData.positionTexture == null) return;
            if (vatData.sharedMesh == null) return;
            if (state.clipIndex < 0 || state.clipIndex >= vatData.clips.Count) return;

            // 머티리얼 찾기: 씬의 SpineVatRenderer에서 가져오기
            var renderer = FindObjectOfType<SpineVatRenderer>();
            if (renderer == null) return;

            var matField = typeof(SpineVatRenderer).GetField("vatMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (matField == null) return;

            var material = matField.GetValue(renderer) as Material;
            if (material == null) return;

            var clip = vatData.clips[state.clipIndex];
            var transform = module.transform;

            singleMatrix[0] = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            singleAnimTime[0] = Mathf.Clamp01(state.normTime);
            singleFrameOffset[0] = clip.frameOffset;
            singleFrameCount[0] = clip.frameCount;

            propertyBlock.Clear();
            propertyBlock.SetFloatArray(PropAnimTime, singleAnimTime);
            propertyBlock.SetFloatArray(PropFrameOffset, singleFrameOffset);
            propertyBlock.SetFloatArray(PropFrameCount, singleFrameCount);

            material.SetFloat(PropTotalFrames, vatData.totalFrames);
            material.SetFloat(PropVertexCount, vatData.vertexCount);
            material.SetTexture(PropVatPositionTex, vatData.positionTexture);

            Graphics.DrawMeshInstanced(
                vatData.sharedMesh,
                0,
                material,
                singleMatrix,
                1,
                propertyBlock
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // GUI
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.LabelField("VAT Animation Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (selectedModules.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "SpineVatAnimModule 컴포넌트가 있는 GameObject를 선택하세요.\n" +
                    "여러 개를 동시에 선택하면 함께 제어할 수 있습니다.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"선택된 오브젝트: {selectedModules.Count}개", EditorStyles.helpBox);
            EditorGUILayout.Space(4);

            // 전체 제어 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All Stop", GUILayout.Height(24)))
            {
                StopAll();
            }
            if (GUILayout.Button("All Pause", GUILayout.Height(24)))
            {
                PauseAll();
            }
            if (GUILayout.Button("All Resume", GUILayout.Height(24)))
            {
                ResumeAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < selectedModules.Count; i++)
            {
                var module = selectedModules[i];
                if (module == null) continue;

                DrawModuleUI(module, i);

                if (i < selectedModules.Count - 1)
                {
                    EditorGUILayout.Space(6);
                    DrawSeparator();
                    EditorGUILayout.Space(6);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawModuleUI(SpineVatAnimModule module, int index)
        {
            int id = module.GetInstanceID();

            // vatData 접근
            var vatDataField = typeof(SpineVatAnimModule).GetField("vatData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (vatDataField == null) return;

            var vatData = vatDataField.GetValue(module) as SpineVatData;
            if (vatData == null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"[{index + 1}] {module.name}", headerStyle);
                EditorGUILayout.HelpBox("SpineVatData가 할당되지 않았습니다.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // 프리뷰 상태 초기화
            if (!previewStates.ContainsKey(id))
            {
                previewStates[id] = new PreviewState
                {
                    clipIndex = -1,
                    normTime = 0f,
                    playing = false,
                    speed = 1f
                };
            }

            var state = previewStates[id];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 헤더
            EditorGUILayout.LabelField($"[{index + 1}] {module.name}", headerStyle);

            // 현재 상태
            if (state.clipIndex >= 0 && state.clipIndex < vatData.clips.Count)
            {
                string clipName = vatData.clips[state.clipIndex].clipName;
                string statusText = state.playing ? "Playing" : "Paused";
                EditorGUILayout.LabelField(
                    $"  {statusText}: {clipName}  ({state.normTime:P0})",
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);

            // 클립 목록
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(35);
            EditorGUILayout.LabelField("Name", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField("Duration", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Events", EditorStyles.miniLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Frames", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            for (int c = 0; c < vatData.clips.Count; c++)
            {
                DrawClipRow(id, vatData, c, state);
            }

            // 이벤트 표시
            if (state.clipIndex >= 0 && state.clipIndex < vatData.clips.Count)
            {
                var clip = vatData.clips[state.clipIndex];
                if (clip.events != null && clip.events.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Events:", EditorStyles.miniLabel);
                    for (int e = 0; e < clip.events.Count; e++)
                    {
                        var evt = clip.events[e];
                        EditorGUILayout.LabelField(
                            $"  @{evt.normalizedTime:P0}  {evt.eventName}",
                            eventLabelStyle);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawClipRow(int moduleId, SpineVatData vatData, int clipIndex, PreviewState state)
        {
            var clip = vatData.clips[clipIndex];
            bool isActive = (state.clipIndex == clipIndex);
            bool isPlaying = isActive && state.playing;

            EditorGUILayout.BeginHorizontal();

            // 재생/정지 버튼
            Color originalBg = GUI.backgroundColor;
            if (isPlaying)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            }

            GUIStyle btnStyle = isPlaying ? activeButtonStyle : idleButtonStyle;
            string btnLabel = isPlaying ? "■" : "▶";

            if (GUILayout.Button(btnLabel, btnStyle, GUILayout.Width(30), GUILayout.Height(20)))
            {
                ToggleClip(moduleId, clipIndex, vatData);
            }

            GUI.backgroundColor = originalBg;

            // 클립 이름
            GUIStyle labelStyle = isActive ? EditorStyles.boldLabel : EditorStyles.label;
            EditorGUILayout.LabelField(clip.clipName, labelStyle, GUILayout.Width(120));

            // Duration
            EditorGUILayout.LabelField($"{clip.duration:F2}s", GUILayout.Width(60));

            // 이벤트 수
            int eventCount = (clip.events != null) ? clip.events.Count : 0;
            EditorGUILayout.LabelField($"{eventCount}", GUILayout.Width(50));

            // 프레임 수
            EditorGUILayout.LabelField($"{clip.frameCount}f", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 조작
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void ToggleClip(int moduleId, int clipIndex, SpineVatData vatData)
        {
            var state = previewStates[moduleId];

            if (state.clipIndex == clipIndex && state.playing)
            {
                // 같은 클립 → 일시정지
                state.playing = false;
            }
            else if (state.clipIndex == clipIndex && !state.playing)
            {
                // 같은 클립 일시정지 상태 → 재개
                state.playing = true;
            }
            else
            {
                // 다른 클립 → 새로 재생
                state.clipIndex = clipIndex;
                state.normTime = 0f;
                state.playing = true;
                state.speed = 1f;
            }

            previewStates[moduleId] = state;
        }

        private void StopAll()
        {
            var keys = new List<int>(previewStates.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var state = previewStates[keys[i]];
                state.clipIndex = -1;
                state.normTime = 0f;
                state.playing = false;
                previewStates[keys[i]] = state;
            }
        }

        private void PauseAll()
        {
            var keys = new List<int>(previewStates.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var state = previewStates[keys[i]];
                state.playing = false;
                previewStates[keys[i]] = state;
            }
        }

        private void ResumeAll()
        {
            var keys = new List<int>(previewStates.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var state = previewStates[keys[i]];
                if (state.clipIndex >= 0) state.playing = true;
                previewStates[keys[i]] = state;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 유틸
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private float GetClipDuration(int moduleId, int clipIndex)
        {
            for (int i = 0; i < selectedModules.Count; i++)
            {
                if (selectedModules[i] == null) continue;
                if (selectedModules[i].GetInstanceID() != moduleId) continue;

                var vatDataField = typeof(SpineVatAnimModule).GetField("vatData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (vatDataField == null) return 1f;

                var vatData = vatDataField.GetValue(selectedModules[i]) as SpineVatData;
                if (vatData == null) return 1f;
                if (clipIndex < 0 || clipIndex >= vatData.clips.Count) return 1f;

                float dur = vatData.clips[clipIndex].duration;
                return dur > 0f ? dur : 1f;
            }
            return 1f;
        }

        private void FindAnyObjectByType<T>() where T : Object { }

        private void InitializeStyles()
        {
            if (idleButtonStyle != null) return;

            idleButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 12
            };

            activeButtonStyle = new GUIStyle(idleButtonStyle)
            {
                fontStyle = FontStyle.Bold
            };
            activeButtonStyle.normal.textColor = Color.white;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            eventLabelStyle = new GUIStyle(EditorStyles.miniLabel);
            eventLabelStyle.normal.textColor = new Color(0.3f, 0.7f, 1f);
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
    }
}
#endif
