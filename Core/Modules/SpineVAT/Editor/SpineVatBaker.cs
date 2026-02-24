#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

#if SPINE_UNITY
using Spine;
using Spine.Unity;
#endif

namespace SpineVAT.Editor
{
    /// <summary>
    /// Spine SkeletonAnimation을 분석하여 VAT 텍스처를 굽고,
    /// EventTimeline에서 이벤트를 추출하여 SpineVatData 에셋으로 저장하는 에디터 도구.
    /// </summary>
    public class SpineVatBaker : EditorWindow
    {
#if SPINE_UNITY
        [Header("Source")]
        private SkeletonDataAsset skeletonDataAsset;
        private List<string> selectedAnimations = new List<string>();
        private bool[] animationToggles;
        private string[] animationNames;

        [Header("Settings")]
        private int sampleRate = 30;
        private string outputFolder = "Assets/SpineVATData";
        private string assetName = "NewVatData";

        private Vector2 scrollPos;

        [MenuItem("Tools/SpineVAT/VAT Baker")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpineVatBaker>("Spine VAT Baker");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Spine VAT Baker", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            // Source
            EditorGUI.BeginChangeCheck();
            skeletonDataAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField(
                "Skeleton Data Asset", skeletonDataAsset, typeof(SkeletonDataAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                RefreshAnimationList();
            }

            if (skeletonDataAsset == null)
            {
                EditorGUILayout.HelpBox("SkeletonDataAsset를 할당해주세요.", MessageType.Info);
                return;
            }

            // Settings
            EditorGUILayout.Space(4);
            sampleRate = EditorGUILayout.IntSlider("Sample Rate (FPS)", sampleRate, 10, 60);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            assetName = EditorGUILayout.TextField("Asset Name", assetName);

            // Animation selection
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);

            if (animationNames == null || animationNames.Length == 0)
            {
                RefreshAnimationList();
            }

            if (animationNames == null || animationNames.Length == 0)
            {
                EditorGUILayout.HelpBox("애니메이션이 없습니다.", MessageType.Warning);
                return;
            }

            // Select all / none
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(100)))
            {
                for (int i = 0; i < animationToggles.Length; i++) animationToggles[i] = true;
            }
            if (GUILayout.Button("Select None", GUILayout.Width(100)))
            {
                for (int i = 0; i < animationToggles.Length; i++) animationToggles[i] = false;
            }
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            for (int i = 0; i < animationNames.Length; i++)
            {
                animationToggles[i] = EditorGUILayout.ToggleLeft(animationNames[i], animationToggles[i]);
            }
            EditorGUILayout.EndScrollView();

            // Bake button
            EditorGUILayout.Space(12);
            GUI.enabled = HasSelectedAnimations();
            if (GUILayout.Button("Bake VAT", GUILayout.Height(36)))
            {
                BakeVAT();
            }
            GUI.enabled = true;
        }

        private bool HasSelectedAnimations()
        {
            if (animationToggles == null) return false;
            for (int i = 0; i < animationToggles.Length; i++)
            {
                if (animationToggles[i]) return true;
            }
            return false;
        }

        private void RefreshAnimationList()
        {
            if (skeletonDataAsset == null)
            {
                animationNames = null;
                animationToggles = null;
                return;
            }

            var skeletonData = skeletonDataAsset.GetSkeletonData(false);
            if (skeletonData == null)
            {
                animationNames = null;
                animationToggles = null;
                return;
            }

            var animations = skeletonData.Animations;
            animationNames = new string[animations.Count];
            animationToggles = new bool[animations.Count];
            for (int i = 0; i < animations.Count; i++)
            {
                animationNames[i] = animations.Items[i].Name;
                animationToggles[i] = true;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 베이크 핵심 로직
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void BakeVAT()
        {
            // 선택된 애니메이션 수집
            selectedAnimations.Clear();
            for (int i = 0; i < animationToggles.Length; i++)
            {
                if (!animationToggles[i]) continue;
                selectedAnimations.Add(animationNames[i]);
            }

            if (selectedAnimations.Count == 0) return;

            // 임시 GameObject 생성
            var go = new GameObject("_VATBaker_Temp");
            var skeletonAnim = go.AddComponent<SkeletonAnimation>();
            skeletonAnim.skeletonDataAsset = skeletonDataAsset;
            skeletonAnim.Initialize(false);

            if (skeletonAnim.Skeleton == null)
            {
                Debug.LogError("[SpineVatBaker] Skeleton 초기화 실패!");
                DestroyImmediate(go);
                return;
            }

            try
            {
                DoBake(skeletonAnim);
            }
            finally
            {
                DestroyImmediate(go);
            }
        }

        private void DoBake(SkeletonAnimation skeletonAnim)
        {
            var skeleton = skeletonAnim.Skeleton;
            var skeletonData = skeleton.Data;
            var animState = skeletonAnim.AnimationState;

            float sampleInterval = 1f / sampleRate;

            // 1단계: 모든 클립의 프레임 수를 먼저 계산
            var clipInfos = new List<ClipBakeInfo>();
            int totalFrames = 0;

            for (int c = 0; c < selectedAnimations.Count; c++)
            {
                string animName = selectedAnimations[c];
                var animation = skeletonData.FindAnimation(animName);
                if (animation == null)
                {
                    Debug.LogWarning($"[SpineVatBaker] Animation not found: {animName}");
                    continue;
                }

                float duration = animation.Duration;
                // 최소 2프레임 (시작 + 끝)
                int frameCount = Mathf.Max(2, Mathf.CeilToInt(duration * sampleRate) + 1);

                clipInfos.Add(new ClipBakeInfo
                {
                    animName = animName,
                    animation = animation,
                    duration = duration,
                    frameCount = frameCount,
                    frameOffset = totalFrames
                });

                totalFrames += frameCount;
            }

            if (clipInfos.Count == 0)
            {
                Debug.LogError("[SpineVatBaker] 유효한 애니메이션이 없습니다.");
                return;
            }

            // 2단계: 첫 프레임 샘플링으로 버텍스 수 결정 + 공유 메쉬 생성
            skeleton.SetToSetupPose();
            var firstAnim = clipInfos[0].animation;
            firstAnim.Apply(skeleton, 0, 0, false, null, 1f, MixBlend.Setup, MixDirection.In);
            skeleton.UpdateWorldTransform(Skeleton.Physics.Update);

            // MeshGenerator로 메쉬 추출
            var meshGenerator = new MeshGenerator();
            meshGenerator.settings = new MeshGenerator.Settings
            {
                useClipping = false,
                zSpacing = 0f,
                pmaVertexColors = true,
                tintBlack = false
            };

            var instructionBuilder = new SkeletonRendererInstruction();
            instructionBuilder.Clear();

            // Skeleton의 DrawOrder로 instruction 생성
            var drawOrder = skeleton.DrawOrder;
            var submeshInstruction = new SubmeshInstruction();
            submeshInstruction.skeleton = skeleton;
            submeshInstruction.startSlot = 0;
            submeshInstruction.endSlot = drawOrder.Count;
            submeshInstruction.rawTriangleCount = 0;
            submeshInstruction.rawVertexCount = 0;
            submeshInstruction.rawFirstVertexIndex = 0;
            submeshInstruction.hasClipping = false;

            // 슬롯별 삼각형/버텍스 수 계산
            for (int s = 0; s < drawOrder.Count; s++)
            {
                var slot = drawOrder.Items[s];
                var attachment = slot.Attachment;
                if (attachment is RegionAttachment regionAtt)
                {
                    submeshInstruction.rawTriangleCount += 6;
                    submeshInstruction.rawVertexCount += 4;
                }
                else if (attachment is MeshAttachment meshAtt)
                {
                    submeshInstruction.rawTriangleCount += meshAtt.Triangles.Length;
                    submeshInstruction.rawVertexCount += meshAtt.WorldVerticesLength / 2;
                }
            }

            instructionBuilder.submeshInstructions.Add(submeshInstruction);
            instructionBuilder.hasActiveClipping = false;
            instructionBuilder.rawVertexCount = submeshInstruction.rawVertexCount;

            meshGenerator.Begin();
            meshGenerator.BuildMesh(instructionBuilder, false);

            int vertexCount = meshGenerator.VertexCount;
            if (vertexCount == 0)
            {
                Debug.LogError("[SpineVatBaker] 버텍스가 없습니다. 메쉬를 확인해주세요.");
                return;
            }

            // 공유 메쉬 생성
            var sharedMesh = new Mesh();
            sharedMesh.name = $"{assetName}_Mesh";
            meshGenerator.FillVertexData(sharedMesh);
            meshGenerator.FillTriangles(sharedMesh);

            // UV2에 버텍스 인덱스 기록 (셰이더에서 텍스처 Fetch용)
            var uv2 = new Vector2[vertexCount];
            for (int v = 0; v < vertexCount; v++)
            {
                uv2[v] = new Vector2((v + 0.5f) / vertexCount, 0f);
            }
            sharedMesh.uv2 = uv2;
            sharedMesh.UploadMeshData(false);

            // 3단계: 모든 프레임 샘플링 → 텍스처 생성
            // 텍스처 크기: Width=vertexCount, Height=totalFrames
            var positionTex = new Texture2D(vertexCount, totalFrames, TextureFormat.RGBAHalf, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = $"{assetName}_PositionTex"
            };

            var vatData = ScriptableObject.CreateInstance<SpineVatData>();
            vatData.totalFrames = totalFrames;
            vatData.vertexCount = vertexCount;

            var reusableVertices = new Vector3[vertexCount];

            EditorUtility.DisplayProgressBar("Spine VAT Baker", "Baking...", 0f);

            for (int c = 0; c < clipInfos.Count; c++)
            {
                var info = clipInfos[c];
                float progress = (float)c / clipInfos.Count;
                EditorUtility.DisplayProgressBar("Spine VAT Baker",
                    $"Baking: {info.animName} ({c + 1}/{clipInfos.Count})", progress);

                var clip = new VatClipData
                {
                    clipName = info.animName,
                    frameOffset = info.frameOffset,
                    frameCount = info.frameCount,
                    duration = info.duration,
                    events = new List<VatEventData>()
                };

                // 프레임별 버텍스 위치 샘플링
                for (int f = 0; f < info.frameCount; f++)
                {
                    float time = (info.duration > 0f)
                        ? Mathf.Min((float)f / (info.frameCount - 1) * info.duration, info.duration)
                        : 0f;

                    skeleton.SetToSetupPose();
                    info.animation.Apply(skeleton, 0, time, false, null, 1f, MixBlend.Setup, MixDirection.In);
                    skeleton.UpdateWorldTransform(Skeleton.Physics.Update);

                    // 메쉬 재생성
                    meshGenerator.Begin();
                    meshGenerator.BuildMesh(instructionBuilder, false);

                    var bufferPositions = meshGenerator.Buffers.vertexBuffer;
                    int count = Mathf.Min(bufferPositions.Length, vertexCount);

                    for (int v = 0; v < count; v++)
                    {
                        reusableVertices[v] = bufferPositions[v];
                    }

                    int row = info.frameOffset + f;
                    for (int v = 0; v < vertexCount; v++)
                    {
                        Vector3 pos = (v < count) ? reusableVertices[v] : Vector3.zero;
                        positionTex.SetPixel(v, row, new Color(pos.x, pos.y, pos.z, 1f));
                    }
                }

                // 이벤트 추출: EventTimeline 분석
                ExtractEvents(info.animation, info.duration, clip.events);

                vatData.clips.Add(clip);
            }

            EditorUtility.ClearProgressBar();

            positionTex.Apply(false, false);

            // 4단계: 에셋 저장
            SaveAssets(vatData, positionTex, sharedMesh);
        }

        /// <summary>
        /// Spine Animation의 Timeline들을 순회하여 EventTimeline에서 이벤트를 추출한다.
        /// </summary>
        private void ExtractEvents(Spine.Animation animation, float duration, List<VatEventData> outEvents)
        {
            if (duration <= 0f) return;

            var timelines = animation.Timelines;
            for (int t = 0, tCount = timelines.Count; t < tCount; t++)
            {
                var timeline = timelines.Items[t];
                if (!(timeline is EventTimeline eventTimeline)) continue;

                var events = eventTimeline.Events;
                for (int e = 0, eCount = events.Length; e < eCount; e++)
                {
                    var evt = events[e];
                    float normalizedTime = Mathf.Clamp01(evt.Time / duration);

                    outEvents.Add(new VatEventData
                    {
                        eventName = evt.Data.Name,
                        normalizedTime = normalizedTime
                    });
                }
            }
        }

        private void SaveAssets(SpineVatData vatData, Texture2D positionTex, Mesh sharedMesh)
        {
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            string texPath = $"{outputFolder}/{assetName}_PositionTex.asset";
            string meshPath = $"{outputFolder}/{assetName}_Mesh.asset";
            string dataPath = $"{outputFolder}/{assetName}.asset";

            AssetDatabase.CreateAsset(positionTex, texPath);
            AssetDatabase.CreateAsset(sharedMesh, meshPath);

            vatData.positionTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            vatData.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

            AssetDatabase.CreateAsset(vatData, dataPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = vatData;

            Debug.Log($"[SpineVatBaker] Bake 완료! → {dataPath}\n" +
                      $"  Clips: {vatData.clips.Count}, Frames: {vatData.totalFrames}, Vertices: {vatData.vertexCount}");
        }

        private struct ClipBakeInfo
        {
            public string animName;
            public Spine.Animation animation;
            public float duration;
            public int frameCount;
            public int frameOffset;
        }

#else
        [MenuItem("Tools/SpineVAT/VAT Baker")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpineVatBaker>("Spine VAT Baker");
            window.ShowNotification(new GUIContent("Spine-Unity가 설치되어 있지 않습니다."));
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Spine-Unity 패키지가 설치되어 있지 않습니다.\n" +
                "SPINE_UNITY 심볼을 추가하고 Spine-Unity를 설치해주세요.",
                MessageType.Error);
        }
#endif
    }
}
#endif
