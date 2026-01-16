#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

#if SPINE_UNITY
using Spine.Unity;
using Spine;
using Spine.Unity.Editor;
#endif

namespace InteractAnimation.Editor
{
    /// <summary>
    /// Spine JSON 파일에 이벤트를 직접 편집하는 GUI 에디터
    /// GUI로 직접 추가/편집/삭제, 애니메이션 미리보기 및 재생 컨트롤
    /// </summary>
    public class SpineEventInjectorEditor : EditorWindow
    {
        private TextAsset spineJsonAsset;
        private Vector2 scrollPosition;
        private Vector2 animListScrollPosition;
        private List<string> animationNames = new List<string>();
        private string selectedAnimation = "";
        private List<SpineEventItem> eventItems = new List<SpineEventItem>();
        private bool isDirty = false;

#if SPINE_UNITY
        // Animation Preview
        private SkeletonDataAsset skeletonDataAsset;
        private UnityEditor.Editor skeletonDataEditor;
        private PreviewRenderUtility previewRenderUtility;
        private GameObject previewInstance;
        private SkeletonAnimation previewSkeletonAnimation;

        // Playback Control
        private enum PlaybackState { Stopped, Playing, Paused }
        private PlaybackState playbackState = PlaybackState.Stopped;
        private float currentTime = 0f;
        private float animationDuration = 0f;
        private bool isLooping = false;
        private float playbackSpeed = 1f;
        private double lastUpdateTime = 0;

        // Preview Settings
        private Vector2 previewScroll;
        private float previewZoom = 0.5f; // Start zoomed out to see full character
        private Vector2 previewPan = Vector2.zero;
        private Color previewBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Mouse interaction
        private bool isDraggingPreview = false;
        private Vector2 lastMousePosition;
#endif

        private class SpineEventItem
        {
            public string eventName = "NewEvent";
            public float time = 0f;
            public string stringParameter = "";
            public int intParameter = 0;
            public float floatParameter = 0f;
            public bool isExpanded = true;
        }

        [MenuItem("Tools/InteractAnimation/Spine Event Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpineEventInjectorEditor>("Spine Event Editor");
            window.minSize = new Vector2(1000, 700); // Wider for side-by-side layout
            window.Show();
        }

#if SPINE_UNITY
        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            InitializePreview();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            CleanupPreview();
        }

        private void OnDestroy()
        {
            CleanupPreview();
        }

        private void OnEditorUpdate()
        {
            if (playbackState == PlaybackState.Playing && animationDuration > 0)
            {
                // Calculate delta time using EditorApplication.timeSinceStartup
                double currentEditorTime = EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentEditorTime - lastUpdateTime);
                lastUpdateTime = currentEditorTime;

                // Cap delta time to prevent huge jumps
                deltaTime = Mathf.Min(deltaTime, 0.1f);

                currentTime += deltaTime * playbackSpeed;

                if (currentTime >= animationDuration)
                {
                    if (isLooping)
                    {
                        currentTime = currentTime % animationDuration;
                    }
                    else
                    {
                        currentTime = 0f; // Reset to beginning
                        playbackState = PlaybackState.Stopped;
                    }
                }

                // Update animation with real deltaTime
                if (previewSkeletonAnimation != null)
                {
                    // Update skeleton animation with real deltaTime
                    previewSkeletonAnimation.Update(deltaTime * playbackSpeed);
                    previewSkeletonAnimation.LateUpdate();

                    // Also update TrackTime for scrubbing
                    if (previewSkeletonAnimation.AnimationState != null)
                    {
                        var trackEntry = previewSkeletonAnimation.AnimationState.GetCurrent(0);
                        if (trackEntry != null)
                        {
                            trackEntry.TrackTime = currentTime;
                        }
                    }
                }

                Repaint();
            }
        }
#endif

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spine Event Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "SkeletonDataAsset을 선택하면 자동으로 JSON을 로드하여 이벤트를 편집합니다.",
                MessageType.Info
            );

            EditorGUILayout.Space();

#if SPINE_UNITY
            // SkeletonDataAsset 선택
            EditorGUI.BeginChangeCheck();
            skeletonDataAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField(
                "Skeleton Data Asset",
                skeletonDataAsset,
                typeof(SkeletonDataAsset),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                // SkeletonDataAsset 변경 시 기존 프리뷰 인스턴스 정리
                if (previewInstance != null)
                {
                    DestroyImmediate(previewInstance);
                    previewInstance = null;
                    previewSkeletonAnimation = null;
                }

                // 이전 애니메이션 상태 초기화
                selectedAnimation = "";
                eventItems.Clear();
                isDirty = false;

                // SkeletonDataAsset에서 JSON 파일 자동 추출
                if (skeletonDataAsset != null)
                {
                    var jsonField = skeletonDataAsset.GetType().GetField("skeletonJSON",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (jsonField != null)
                    {
                        var jsonAsset = jsonField.GetValue(skeletonDataAsset) as TextAsset;
                        if (jsonAsset != null)
                        {
                            spineJsonAsset = jsonAsset;
                            LoadAnimationsFromJson();
                            Debug.Log($"[SpineEventEditor] Auto-loaded JSON: {jsonAsset.name}");
                        }
                    }
                }
                else
                {
                    spineJsonAsset = null;
                    animationNames.Clear();
                    selectedAnimation = "";
                    eventItems.Clear();
                }
            }

            if (skeletonDataAsset == null)
            {
                EditorGUILayout.HelpBox("SkeletonDataAsset을 선택하세요.", MessageType.Warning);
                return;
            }

            if (spineJsonAsset != null)
            {
                EditorGUILayout.LabelField("JSON Path", AssetDatabase.GetAssetPath(spineJsonAsset));
            }
#else
            // Spine Runtime이 없을 경우
            EditorGUILayout.HelpBox(
                "Spine Runtime이 설치되지 않았거나 SPINE_UNITY 심볼이 정의되지 않았습니다.",
                MessageType.Warning
            );

            if (GUILayout.Button("Enable Spine Runtime (Add SPINE_UNITY Symbol)", GUILayout.Height(35)))
            {
                EnableSpineRuntime();
            }
            return;
#endif

            EditorGUILayout.Space();

            // 애니메이션 목록
            if (animationNames.Count == 0)
            {
                EditorGUILayout.HelpBox("JSON에 애니메이션이 없습니다.", MessageType.Warning);
                return;
            }

            // Main Layout: Left (Animation List + Events) | Right (Preview)
            EditorGUILayout.BeginHorizontal();

            // ===== LEFT PANEL: Animation List + Events =====
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.5f));

            // 애니메이션 목록
            EditorGUILayout.LabelField("Animations:", EditorStyles.boldLabel);
            animListScrollPosition = EditorGUILayout.BeginScrollView(animListScrollPosition, GUILayout.Height(250));

            foreach (var animName in animationNames)
            {
                if (GUILayout.Toggle(selectedAnimation == animName, animName, "Button"))
                {
                    if (selectedAnimation != animName)
                    {
                        // Check for unsaved changes
                        if (isDirty)
                        {
                            int choice = EditorUtility.DisplayDialogComplex(
                                "Unsaved Changes",
                                $"'{selectedAnimation}'에 저장되지 않은 변경사항이 있습니다.\n어떻게 하시겠습니까?",
                                "Save and Continue",  // 0
                                "Cancel",             // 1
                                "Discard Changes"     // 2
                            );

                            if (choice == 0) // Save and Continue
                            {
                                SaveEventsToJson();
                                selectedAnimation = animName;
                                LoadEventsForAnimation(animName);
                            }
                            else if (choice == 2) // Discard Changes
                            {
                                selectedAnimation = animName;
                                LoadEventsForAnimation(animName);
                            }
                            // choice == 1 (Cancel): Do nothing, keep current animation
                        }
                        else
                        {
                            selectedAnimation = animName;
                            LoadEventsForAnimation(animName);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            if (string.IsNullOrEmpty(selectedAnimation))
            {
                EditorGUILayout.HelpBox("애니메이션을 선택하세요.", MessageType.Info);
                EditorGUILayout.EndVertical(); // End left panel
                EditorGUILayout.EndHorizontal(); // End main layout
                return;
            }

            EditorGUILayout.Space();

            // 선택된 애니메이션 정보
            EditorGUILayout.LabelField($"Selected: {selectedAnimation}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Events: {eventItems.Count}");

            EditorGUILayout.Space();

            // 버튼들
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add New Event", GUILayout.Height(30)))
            {
                AddNewEvent();
            }

            if (GUILayout.Button("Save to JSON", GUILayout.Height(30)))
            {
                SaveEventsToJson();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Clear All Events", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog(
                    "Clear All Events",
                    $"'{selectedAnimation}'의 모든 이벤트를 삭제하시겠습니까?",
                    "Yes", "No"))
                {
                    ClearAllEvents();
                }
            }

            EditorGUILayout.Space();

            // 경고 메시지를 이벤트 목록 위로 이동
            if (isDirty)
            {
                EditorGUILayout.HelpBox("변경사항이 있습니다. 'Save to JSON'을 클릭하세요.", MessageType.Warning);
            }

            // 이벤트 목록
            if (eventItems.Count == 0)
            {
                EditorGUILayout.HelpBox("이벤트가 없습니다. 'Add New Event'를 클릭하세요.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Event List ({eventItems.Count}):", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

                for (int i = 0; i < eventItems.Count; i++)
                {
                    DrawEventItem(i);
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical(); // End left panel

            // ===== RIGHT PANEL: Preview =====
            EditorGUILayout.BeginVertical();

#if SPINE_UNITY
            // Animation Preview Section
            DrawPreviewSection();
#else
            // Spine Runtime이 없을 경우 경고 메시지
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Spine Runtime이 설치되지 않았거나 SPINE_UNITY 심볼이 정의되지 않았습니다.\n" +
                "애니메이션 미리보기를 사용하려면:\n\n" +
                "1. Spine Runtime이 이미 설치되어 있다면 아래 버튼을 클릭\n" +
                "2. 설치되지 않았다면 Spine-Unity Runtime을 먼저 임포트하세요",
                MessageType.Warning
            );

            EditorGUILayout.Space();

            // Enable SPINE_UNITY button
            if (GUILayout.Button("Enable Spine Runtime (Add SPINE_UNITY Symbol)", GUILayout.Height(35)))
            {
                EnableSpineRuntime();
            }

            EditorGUILayout.Space();

            // Preview placeholder
            Rect placeholderRect = GUILayoutUtility.GetRect(400, 300);
            EditorGUI.DrawRect(placeholderRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            GUIStyle centeredStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            centeredStyle.fontSize = 14;
            centeredStyle.normal.textColor = Color.gray;

            GUI.Label(placeholderRect, "Preview requires Spine Runtime", centeredStyle);

            EditorGUILayout.EndVertical();
#endif

            EditorGUILayout.EndVertical(); // End right panel

            EditorGUILayout.EndHorizontal(); // End main layout
        }

        private void DrawEventItem(int index)
        {
            var item = eventItems[index];

            EditorGUILayout.BeginVertical("box");

            // 헤더
            EditorGUILayout.BeginHorizontal();
            item.isExpanded = EditorGUILayout.Foldout(item.isExpanded, $"Event {index + 1}: {item.eventName} @ {item.time:F2}s", true);

            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                RemoveEvent(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (item.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();

                item.eventName = EditorGUILayout.TextField("Event Name", item.eventName);
                item.time = EditorGUILayout.FloatField("Time (seconds)", item.time);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Parameters:", EditorStyles.boldLabel);
                item.stringParameter = EditorGUILayout.TextField("String", item.stringParameter);
                item.intParameter = EditorGUILayout.IntField("Int", item.intParameter);
                item.floatParameter = EditorGUILayout.FloatField("Float", item.floatParameter);

                if (EditorGUI.EndChangeCheck())
                {
                    isDirty = true;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void LoadAnimationsFromJson()
        {
            animationNames.Clear();
            selectedAnimation = "";
            eventItems.Clear();
            isDirty = false;

            if (spineJsonAsset == null) return;

            try
            {
                JObject json = JObject.Parse(spineJsonAsset.text);
                var animations = json["animations"] as JObject;

                if (animations != null)
                {
                    foreach (var prop in animations.Properties())
                    {
                        animationNames.Add(prop.Name);
                    }
                }

                Debug.Log($"[SpineEventEditor] Loaded {animationNames.Count} animations from JSON");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"JSON 파싱 실패:\n{ex.Message}", "OK");
                Debug.LogError($"[SpineEventEditor] JSON parse error: {ex}");
            }
        }

        private void LoadEventsForAnimation(string animName)
        {
            eventItems.Clear();
            isDirty = false;

#if SPINE_UNITY
            // Stop preview when changing animation
            StopPreview();
#endif

            if (spineJsonAsset == null || string.IsNullOrEmpty(animName)) return;

            try
            {
                JObject json = JObject.Parse(spineJsonAsset.text);
                var animations = json["animations"] as JObject;
                var animation = animations?[animName] as JObject;
                var events = animation?["events"] as JArray;

                if (events != null)
                {
                    foreach (var evt in events)
                    {
                        eventItems.Add(new SpineEventItem
                        {
                            eventName = evt["name"]?.ToString() ?? "Unnamed",
                            time = evt["time"]?.ToObject<float>() ?? 0f,
                            stringParameter = evt["string"]?.ToString() ?? "",
                            intParameter = evt["int"]?.ToObject<int>() ?? 0,
                            floatParameter = evt["float"]?.ToObject<float>() ?? 0f,
                            isExpanded = false
                        });
                    }
                }

                // 시간순 정렬
                eventItems = eventItems.OrderBy(e => e.time).ToList();

                Debug.Log($"[SpineEventEditor] Loaded {eventItems.Count} events for '{animName}'");

#if SPINE_UNITY
                // Set up animation in preview
                if (previewSkeletonAnimation != null && previewSkeletonAnimation.AnimationState != null)
                {
                    try
                    {
                        var trackEntry = previewSkeletonAnimation.AnimationState.SetAnimation(0, animName, false);
                        if (trackEntry != null)
                        {
                            trackEntry.TrackTime = 0f;
                            trackEntry.TimeScale = 0f;
                        }
                        previewSkeletonAnimation.Update(0);
                        previewSkeletonAnimation.LateUpdate();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[SpineEventEditor] Failed to set animation in preview: {ex.Message}");
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"이벤트 로드 실패:\n{ex.Message}", "OK");
                Debug.LogError($"[SpineEventEditor] Load events error: {ex}");
            }
        }

        private void AddNewEvent()
        {
            // Use current timeline time, clamped to animation duration
            float newEventTime = currentTime;
            if (animationDuration > 0)
            {
                newEventTime = Mathf.Clamp(currentTime, 0f, animationDuration);
            }

            eventItems.Add(new SpineEventItem
            {
                eventName = "NewEvent",
                time = newEventTime,
                isExpanded = true
            });

            isDirty = true;
        }

        private void RemoveEvent(int index)
        {
            if (index >= 0 && index < eventItems.Count)
            {
                eventItems.RemoveAt(index);
                isDirty = true;
            }
        }

        private void SaveEventsToJson()
        {
            if (spineJsonAsset == null || string.IsNullOrEmpty(selectedAnimation))
            {
                EditorUtility.DisplayDialog("Error", "JSON 파일 또는 애니메이션이 선택되지 않았습니다.", "OK");
                return;
            }

            try
            {
                string jsonPath = AssetDatabase.GetAssetPath(spineJsonAsset);
                string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), jsonPath);

                JObject json = JObject.Parse(File.ReadAllText(fullPath));

                // events 섹션 업데이트
                JObject eventsSection = json["events"] as JObject ?? new JObject();
                json["events"] = eventsSection;

                // 각 이벤트 정의 추가/업데이트 (덮어쓰기)
                foreach (var item in eventItems)
                {
                    // 항상 최신 파라미터로 업데이트
                    JObject eventDef = new JObject
                    {
                        ["int"] = item.intParameter,
                        ["float"] = item.floatParameter,
                        ["string"] = item.stringParameter ?? ""
                    };
                    eventsSection[item.eventName] = eventDef; // 덮어쓰기
                }

                // 애니메이션 이벤트 배열 업데이트
                var animations = json["animations"] as JObject;
                var animation = animations[selectedAnimation] as JObject;

                JArray animEvents = new JArray();
                foreach (var item in eventItems.OrderBy(e => e.time))
                {
                    JObject evt = new JObject
                    {
                        ["time"] = item.time,
                        ["name"] = item.eventName
                    };

                    if (item.intParameter != 0)
                        evt["int"] = item.intParameter;
                    if (item.floatParameter != 0)
                        evt["float"] = item.floatParameter;
                    if (!string.IsNullOrEmpty(item.stringParameter))
                        evt["string"] = item.stringParameter;

                    animEvents.Add(evt);
                }

                animation["events"] = animEvents;

                // 저장
                File.WriteAllText(fullPath, json.ToString(Newtonsoft.Json.Formatting.Indented));

                // Refresh only the JSON asset instead of all assets
                AssetDatabase.ImportAsset(jsonPath, ImportAssetOptions.ForceUpdate);

#if SPINE_UNITY
                // Recreate preview instance after JSON update
                if (previewInstance != null)
                {
                    DestroyImmediate(previewInstance);
                    previewInstance = null;
                    previewSkeletonAnimation = null;
                }

                // Recreate preview with updated data
                if (skeletonDataAsset != null)
                {
                    CreatePreviewSkeletonInstance();

                    // Restore animation state
                    if (previewSkeletonAnimation != null && !string.IsNullOrEmpty(selectedAnimation))
                    {
                        try
                        {
                            var trackEntry = previewSkeletonAnimation.AnimationState.SetAnimation(0, selectedAnimation, false);
                            if (trackEntry != null)
                            {
                                trackEntry.TrackTime = currentTime;
                                trackEntry.TimeScale = 0f;
                            }
                            previewSkeletonAnimation.Update(0);
                            previewSkeletonAnimation.LateUpdate();
                        }
                        catch (Exception previewEx)
                        {
                            Debug.LogWarning($"[SpineEventEditor] Failed to restore preview: {previewEx.Message}");
                        }
                    }
                }
#endif

                isDirty = false;

                EditorUtility.DisplayDialog(
                    "Success",
                    $"'{selectedAnimation}'에 {eventItems.Count}개의 이벤트가 저장되었습니다.",
                    "OK"
                );

                Debug.Log($"[SpineEventEditor] Saved {eventItems.Count} events to '{selectedAnimation}'");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"저장 실패:\n{ex.Message}", "OK");
                Debug.LogError($"[SpineEventEditor] Save error: {ex}");
            }
        }

        private void ClearAllEvents()
        {
            eventItems.Clear();
            isDirty = true;
        }

#if !SPINE_UNITY
        /// <summary>
        /// SPINE_UNITY 스크립팅 심볼을 PlayerSettings에 추가
        /// </summary>
        private void EnableSpineRuntime()
        {
            // Get current build target group
            BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            // Get current scripting define symbols
            string currentDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

            // Check if SPINE_UNITY is already defined
            if (currentDefines.Contains("SPINE_UNITY"))
            {
                EditorUtility.DisplayDialog(
                    "Already Enabled",
                    "SPINE_UNITY 심볼이 이미 정의되어 있습니다.",
                    "OK"
                );
                return;
            }

            // Add SPINE_UNITY to defines
            string newDefines = string.IsNullOrEmpty(currentDefines)
                ? "SPINE_UNITY"
                : currentDefines + ";SPINE_UNITY";

            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newDefines);

            EditorUtility.DisplayDialog(
                "Success",
                "SPINE_UNITY 심볼이 추가되었습니다.\n\n" +
                "Unity가 스크립트를 재컴파일합니다.\n" +
                "재컴파일 후 에디터를 다시 열어주세요.",
                "OK"
            );

            Debug.Log($"[SpineEventEditor] Added SPINE_UNITY to scripting defines: {newDefines}");
        }
#endif

#if SPINE_UNITY
        #region Animation Preview

        private void DrawPreviewSection()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Animation Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Animation Duration - JSON 파일에서 직접 읽기
            if (!string.IsNullOrEmpty(selectedAnimation) && spineJsonAsset != null)
            {
                try
                {
                    JObject json = JObject.Parse(spineJsonAsset.text);
                    var animations = json["animations"] as JObject;
                    var animation = animations?[selectedAnimation] as JObject;

                    if (animation != null)
                    {
                        // Spine JSON에서 duration을 직접 계산 (모든 타임라인의 최대 시간)
                        float maxTime = 0f;

                        // bones, slots, deform, drawOrder, events 등 모든 타임라인 확인
                        foreach (var section in animation.Properties())
                        {
                            if (section.Value is JObject sectionObj)
                            {
                                // bones/slots 등의 섹션 안에 있는 각 항목
                                foreach (var item in sectionObj.Properties())
                                {
                                    if (item.Value is JObject itemObj)
                                    {
                                        // 각 항목 안의 타임라인 (translate, rotate, scale 등)
                                        foreach (var timeline in itemObj.Properties())
                                        {
                                            if (timeline.Value is JArray frames)
                                            {
                                                foreach (var frame in frames)
                                                {
                                                    var time = frame["time"]?.ToObject<float>() ?? 0f;
                                                    if (time > maxTime) maxTime = time;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (section.Value is JArray sectionArray)
                            {
                                // events, drawOrder 등의 배열
                                foreach (var item in sectionArray)
                                {
                                    var time = item["time"]?.ToObject<float>() ?? 0f;
                                    if (time > maxTime) maxTime = time;
                                }
                            }
                        }

                        // 최소값 설정 (이벤트만 있을 경우를 대비)
                        if (maxTime < 0.1f && eventItems.Count > 0)
                        {
                            maxTime = eventItems.Max(e => e.time) + 0.5f;
                        }

                        // 여전히 0이면 기본값 설정
                        if (maxTime <= 0f)
                        {
                            maxTime = 1.0f; // 기본 1초
                        }

                        animationDuration = maxTime;
                        EditorGUILayout.LabelField($"Duration: {animationDuration:F2}s");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[SpineEventEditor] Failed to parse animation duration: {ex.Message}");
                    animationDuration = 1.0f; // 에러 시 기본값
                }
            }

            // Playback Controls
            EditorGUILayout.BeginHorizontal();

            // Loop toggle button
            Color loopColor = isLooping ? new Color(0.5f, 1f, 0.5f) : new Color(0.3f, 0.3f, 0.3f);
            GUI.backgroundColor = loopColor;
            if (GUILayout.Button(isLooping ? "Loop: ON" : "Loop: OFF", GUILayout.Height(30), GUILayout.Width(90)))
            {
                isLooping = !isLooping;
            }

            GUI.backgroundColor = Color.white;

            // Toggle Play/Pause button
            if (playbackState == PlaybackState.Playing)
            {
                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("❚❚ Pause", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    PausePreview();
                }
            }
            else
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Play", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    PlayPreview();
                }
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("■ Stop", GUILayout.Height(30), GUILayout.Width(80)))
            {
                StopPreview();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Speed control
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Speed:", GUILayout.Width(50));
            playbackSpeed = EditorGUILayout.Slider(playbackSpeed, 0.1f, 3f);
            EditorGUILayout.EndHorizontal();

            // Timeline scrubber
            EditorGUILayout.LabelField($"Time: {currentTime:F2}s / {animationDuration:F2}s");
            EditorGUI.BeginChangeCheck();
            float newTime = EditorGUILayout.Slider(currentTime, 0f, animationDuration);
            if (EditorGUI.EndChangeCheck())
            {
                currentTime = newTime;
                UpdatePreviewAnimation();
            }

            EditorGUILayout.Space();

            // Spine Preview using Spine-Unity's preview system
            DrawSpinePreview();

            EditorGUILayout.Space();

            // Visual Timeline (이벤트 위치 표시)
            DrawEventTimeline();

            EditorGUILayout.Space();

            // Preview controls
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Zoom:", GUILayout.Width(50));
            previewZoom = EditorGUILayout.Slider(previewZoom, 0.1f, 5f);
            if (GUILayout.Button("Reset View", GUILayout.Width(100)))
            {
                previewZoom = 0.5f;
                previewPan = Vector2.zero;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawSpinePreview()
        {
            if (skeletonDataAsset == null) return;

            EditorGUILayout.LabelField("Spine Preview", EditorStyles.boldLabel);

            // Preview 영역 - 동적 크기 (우측 패널의 남은 공간 사용)
            float previewWidth = position.width * 0.48f; // Right panel width
            float previewHeight = Mathf.Max(400, position.height - 400); // Flexible height
            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);

            // Create preview instance if needed
            if (previewSkeletonAnimation == null && skeletonDataAsset != null)
            {
                CreatePreviewSkeletonInstance();
            }

            if (previewRenderUtility != null && previewSkeletonAnimation != null)
            {
                try
                {
                    // Render preview (animation is updated in OnEditorUpdate)
                    previewRenderUtility.BeginPreview(previewRect, GUIStyle.none);

                    previewRenderUtility.camera.orthographicSize = 2f / previewZoom;
                    previewRenderUtility.camera.transform.position = new Vector3(previewPan.x, previewPan.y, -10);

                    // Draw mesh
                    var meshRenderer = previewInstance.GetComponent<MeshRenderer>();
                    var meshFilter = previewInstance.GetComponent<MeshFilter>();

                    if (meshRenderer != null && meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
                        {
                            if (i < meshRenderer.sharedMaterials.Length && meshRenderer.sharedMaterials[i] != null)
                            {
                                previewRenderUtility.DrawMesh(
                                    meshFilter.sharedMesh,
                                    previewInstance.transform.localToWorldMatrix,
                                    meshRenderer.sharedMaterials[i],
                                    i
                                );
                            }
                        }
                    }

                    previewRenderUtility.camera.Render();
                    Texture resultTexture = previewRenderUtility.EndPreview();

                    GUI.DrawTexture(previewRect, resultTexture, ScaleMode.StretchToFill, false);
                }
                catch (System.Exception ex)
                {
                    EditorGUI.DrawRect(previewRect, previewBackgroundColor);
                    EditorGUI.LabelField(previewRect, $"Preview Error: {ex.Message}", EditorStyles.centeredGreyMiniLabel);
                }
            }
            else
            {
                EditorGUI.DrawRect(previewRect, previewBackgroundColor);
                GUI.Label(previewRect, "Spine Preview", EditorStyles.centeredGreyMiniLabel);
            }

            // Handle mouse input for zoom and pan
            HandlePreviewMouseInput(previewRect);
        }

        private void HandlePreviewMouseInput(Rect previewRect)
        {
            UnityEngine.Event e = UnityEngine.Event.current;

            if (!previewRect.Contains(e.mousePosition))
            {
                isDraggingPreview = false;
                return;
            }

            // Mouse wheel zoom
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -e.delta.y * 0.05f;
                previewZoom = Mathf.Clamp(previewZoom + zoomDelta, 0.1f, 5f);
                e.Use();
                Repaint();
            }

            // Mouse drag pan - simpler version with right click
            if (e.type == EventType.MouseDown && e.button == 1) // Right click
            {
                isDraggingPreview = true;
                lastMousePosition = e.mousePosition;
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 2) // Middle click
            {
                isDraggingPreview = true;
                lastMousePosition = e.mousePosition;
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && e.alt) // Alt + left
            {
                isDraggingPreview = true;
                lastMousePosition = e.mousePosition;
                e.Use();
            }

            if (e.type == EventType.MouseDrag && isDraggingPreview)
            {
                Vector2 delta = e.mousePosition - lastMousePosition;
                // Scale delta by zoom level (inverted because higher zoom = more zoomed in = slower pan)
                previewPan += delta * 0.01f / previewZoom;
                lastMousePosition = e.mousePosition;
                e.Use();
                Repaint();
            }

            if (e.type == EventType.MouseUp)
            {
                isDraggingPreview = false;
            }
        }

        private void DrawEventTimeline()
        {
            if (animationDuration <= 0) return;

            EditorGUILayout.LabelField("Event Timeline", EditorStyles.boldLabel);

            float timelineWidth = position.width * 0.48f; // Match preview width
            Rect timelineRect = GUILayoutUtility.GetRect(timelineWidth, 60);
            EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f, 1f));

            // Draw timeline background
            Rect timelineBg = new Rect(timelineRect.x + 10, timelineRect.y + 25, timelineRect.width - 20, 10);
            EditorGUI.DrawRect(timelineBg, new Color(0.3f, 0.3f, 0.3f, 1f));

            // Draw current time marker
            if (animationDuration > 0)
            {
                float normalizedTime = currentTime / animationDuration;
                float markerX = timelineBg.x + timelineBg.width * normalizedTime;
                Rect markerRect = new Rect(markerX - 1, timelineBg.y - 5, 2, 20);
                EditorGUI.DrawRect(markerRect, Color.green);
            }

            // Draw events on timeline
            foreach (var evt in eventItems)
            {
                if (evt.time <= animationDuration)
                {
                    float normalizedTime = evt.time / animationDuration;
                    float eventX = timelineBg.x + timelineBg.width * normalizedTime;
                    Rect eventRect = new Rect(eventX - 3, timelineBg.y - 3, 6, 16);
                    EditorGUI.DrawRect(eventRect, Color.yellow);
                }
            }

            // Draw time labels
            GUI.Label(new Rect(timelineBg.x, timelineBg.y + 12, 50, 20), "0s", EditorStyles.miniLabel);
            GUI.Label(new Rect(timelineBg.xMax - 50, timelineBg.y + 12, 50, 20), $"{animationDuration:F1}s", EditorStyles.miniLabel);
        }

        private void InitializePreview()
        {
            if (previewRenderUtility == null)
            {
                previewRenderUtility = new PreviewRenderUtility();
                previewRenderUtility.camera.transform.position = new Vector3(0, 0, -10);
                previewRenderUtility.camera.transform.rotation = Quaternion.identity;
                previewRenderUtility.camera.clearFlags = CameraClearFlags.SolidColor;
                previewRenderUtility.camera.backgroundColor = previewBackgroundColor;
                previewRenderUtility.camera.orthographic = true;
                previewRenderUtility.camera.orthographicSize = 2f;
                previewRenderUtility.camera.nearClipPlane = 0.01f;
                previewRenderUtility.camera.farClipPlane = 100f;
            }
        }

        private void CleanupPreview()
        {
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
                previewInstance = null;
                previewSkeletonAnimation = null;
            }

            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
                previewRenderUtility = null;
            }

            if (skeletonDataEditor != null)
            {
                DestroyImmediate(skeletonDataEditor);
                skeletonDataEditor = null;
            }
        }

        private void CreatePreviewSkeletonInstance()
        {
            // Cleanup old instance
            if (previewInstance != null)
            {
                DestroyImmediate(previewInstance);
                previewInstance = null;
                previewSkeletonAnimation = null;
            }

            if (skeletonDataAsset == null || previewRenderUtility == null) return;

            try
            {
                // Create temporary preview GameObject in the preview scene
                previewInstance = EditorUtility.CreateGameObjectWithHideFlags(
                    "SpinePreview",
                    HideFlags.HideAndDontSave
                );

                // Add to preview render utility's scene
                previewRenderUtility.AddSingleGO(previewInstance);

                // Add SkeletonAnimation component
                previewSkeletonAnimation = previewInstance.AddComponent<SkeletonAnimation>();
                previewSkeletonAnimation.skeletonDataAsset = skeletonDataAsset;

                // Try to initialize - suppress Unity console errors
                bool initSuccess = false;
                try
                {
                    // Temporarily suppress error logs to prevent spam
                    var logType = Application.GetStackTraceLogType(LogType.Error);
                    Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

                    previewSkeletonAnimation.Initialize(false);
                    initSuccess = previewSkeletonAnimation.SkeletonDataAsset != null;

                    Application.SetStackTraceLogType(LogType.Error, logType);
                }
                catch (System.Exception initEx)
                {
                    Debug.LogWarning($"[SpineEventEditor] Skeleton initialization failed: {initEx.Message}\n" +
                        "프리뷰를 표시할 수 없지만 이벤트 편집은 가능합니다.\n" +
                        "JSON 파일에 애니메이션 데이터 오류가 있을 수 있습니다. Spine 에디터에서 다시 익스포트해보세요.");
                }

                if (!initSuccess)
                {
                    Debug.LogWarning("[SpineEventEditor] Preview disabled due to SkeletonData errors. Event editing will still work.");
                    DestroyImmediate(previewInstance);
                    previewInstance = null;
                    previewSkeletonAnimation = null;
                }
                else
                {
                    Debug.Log("[SpineEventEditor] Preview skeleton instance created successfully");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SpineEventEditor] Failed to create preview instance: {ex.Message}");

                if (previewInstance != null)
                {
                    DestroyImmediate(previewInstance);
                    previewInstance = null;
                    previewSkeletonAnimation = null;
                }
            }
        }

        private void CreatePreviewInstance()
        {
            // Create skeleton preview
            CreatePreviewSkeletonInstance();
        }


        private void PlayPreview()
        {
            if (string.IsNullOrEmpty(selectedAnimation))
            {
                EditorUtility.DisplayDialog("Error", "애니메이션을 먼저 선택하세요.", "OK");
                return;
            }

            if (animationDuration <= 0)
            {
                EditorUtility.DisplayDialog("Error", "애니메이션 duration이 유효하지 않습니다.", "OK");
                return;
            }

            // Create preview if needed
            if (previewSkeletonAnimation == null)
            {
                CreatePreviewSkeletonInstance();
            }

            // Set animation
            if (previewSkeletonAnimation != null && previewSkeletonAnimation.AnimationState != null)
            {
                try
                {
                    var trackEntry = previewSkeletonAnimation.AnimationState.SetAnimation(0, selectedAnimation, isLooping);
                    if (trackEntry != null)
                    {
                        trackEntry.TrackTime = currentTime;
                        trackEntry.TimeScale = 1f; // Always use 1f, speed is handled in Update
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[SpineEventEditor] Failed to set animation: {ex.Message}");
                }
            }

            // Initialize lastUpdateTime to prevent huge first deltaTime
            lastUpdateTime = EditorApplication.timeSinceStartup;
            playbackState = PlaybackState.Playing;
        }

        private void PausePreview()
        {
            playbackState = PlaybackState.Paused;
        }

        private void StopPreview()
        {
            currentTime = 0f;
            playbackState = PlaybackState.Stopped;
        }

        private void UpdatePreviewAnimation()
        {
            if (previewSkeletonAnimation == null || string.IsNullOrEmpty(selectedAnimation))
                return;

            if (previewSkeletonAnimation.AnimationState == null)
                return;

            try
            {
                var trackEntry = previewSkeletonAnimation.AnimationState.GetCurrent(0);

                // Always set animation if track is null or different animation
                if (trackEntry == null || trackEntry.Animation.Name != selectedAnimation)
                {
                    trackEntry = previewSkeletonAnimation.AnimationState.SetAnimation(0, selectedAnimation, false);
                    if (trackEntry != null)
                    {
                        trackEntry.TimeScale = 0f; // Freeze for manual scrubbing
                    }
                }

                // Update track time
                if (trackEntry != null)
                {
                    trackEntry.TrackTime = currentTime;
                }

                // Always force update to apply changes
                previewSkeletonAnimation.Update(0);
                previewSkeletonAnimation.LateUpdate();

                // Force repaint to show the changes
                Repaint();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SpineEventEditor] Failed to update preview: {ex.Message}");
            }
        }

        #endregion
#endif
    }
}
#endif
