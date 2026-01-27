#if SPINE_UNITY
using UnityEngine;
using UnityEditor;
using Spine.Unity;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace SpineTool.Editor
{
    /// <summary>
    /// SkeletonAnimation에서 애니메이션 목록을 읽어서 Enum 코드를 자동 생성하는 에디터 윈도우
    /// 3가지 생성 모드 지원: Individual, Combined, Smart Combined
    /// </summary>
    public class SpineAnimationEnumGenerator : EditorWindow
    {
        public enum GenerationMode
        {
            Individual,      // 각각 별도 Enum 생성
            Combined,        // 하나의 Enum으로 통합 (Prefix)
            SmartCombined    // 공통/개별 자동 분리
        }

        // UI
        private Vector2 scrollPosition;
        private Vector2 skeletonListScroll;

        // 설정
        private List<SkeletonDataAsset> skeletonDataAssets = new List<SkeletonDataAsset>();
        private GenerationMode generationMode = GenerationMode.Individual;
        private string enumName = "CharacterAnimations";
        private string commonEnumName = "CommonAnimations";
        private string namespaceName = "Game.Animations";
        private string outputPath = "Assets/Scripts/Animations";
        private bool includeNamespace = true;
        private bool addPrefix = true;

        [MenuItem("Tools/SpineTool/Animation Enum Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpineAnimationEnumGenerator>("Anim Enum Generator");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // 현재 선택된 오브젝트에서 SkeletonDataAsset 찾기
            if (Selection.objects != null && Selection.objects.Length > 0)
            {
                foreach (var obj in Selection.objects)
                {
                    if (obj is SkeletonDataAsset skeletonData && !skeletonDataAssets.Contains(skeletonData))
                    {
                        skeletonDataAssets.Add(skeletonData);
                    }
                }
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Spine Animation Enum Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("SkeletonDataAsset에서 애니메이션 목록을 읽어서 Enum 코드를 자동 생성합니다.\n3가지 모드: Individual (각각), Combined (통합), Smart Combined (공통 자동 분리)", MessageType.Info);

            EditorGUILayout.Space(10);

            // ━━━━━ 1. Skeleton Data 선택 ━━━━━
            DrawSkeletonSelection();

            EditorGUILayout.Space(10);

            // ━━━━━ 2. Generation Mode ━━━━━
            DrawGenerationMode();

            EditorGUILayout.Space(10);

            // ━━━━━ 3. Enum 설정 ━━━━━
            DrawEnumSettings();

            EditorGUILayout.Space(10);

            // ━━━━━ 4. 출력 경로 ━━━━━
            DrawOutputPath();

            EditorGUILayout.Space(10);

            // ━━━━━ 5. 미리보기 ━━━━━
            DrawPreview();

            EditorGUILayout.Space(10);

            // ━━━━━ 6. 생성 버튼 ━━━━━
            DrawGenerateButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSkeletonSelection()
        {
            EditorGUILayout.LabelField("1. Skeleton Data 선택", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            // 리스트
            skeletonListScroll = EditorGUILayout.BeginScrollView(skeletonListScroll, GUILayout.MaxHeight(150));

            for (int i = 0; i < skeletonDataAssets.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                skeletonDataAssets[i] = (SkeletonDataAsset)EditorGUILayout.ObjectField(
                    $"#{i + 1}",
                    skeletonDataAssets[i],
                    typeof(SkeletonDataAsset),
                    false
                );

                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    skeletonDataAssets.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // 추가/제거 버튼
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("+ Add Skeleton Data"))
            {
                skeletonDataAssets.Add(null);
            }

            if (GUILayout.Button("Clear All") && skeletonDataAssets.Count > 0)
            {
                if (EditorUtility.DisplayDialog("확인", "모든 항목을 제거하시겠습니까?", "예", "아니오"))
                {
                    skeletonDataAssets.Clear();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // 통계
            int validCount = skeletonDataAssets.Count(s => s != null);
            if (validCount > 0)
            {
                EditorGUILayout.HelpBox($"✓ {validCount}개의 Skeleton Data 선택됨", MessageType.Info);
            }
        }

        private void DrawGenerationMode()
        {
            EditorGUILayout.LabelField("2. Generation Mode", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            generationMode = (GenerationMode)EditorGUILayout.EnumPopup("모드", generationMode);

            EditorGUILayout.Space(5);

            // 모드 설명
            switch (generationMode)
            {
                case GenerationMode.Individual:
                    EditorGUILayout.HelpBox(
                        "Individual: 각 Skeleton마다 별도 Enum 생성\n" +
                        "예: PlayerAnimations.cs, EnemyAnimations.cs\n" +
                        "장점: 명확한 분리, 타입 안전\n" +
                        "단점: 공통 애니메이션 중복",
                        MessageType.None
                    );
                    break;

                case GenerationMode.Combined:
                    EditorGUILayout.HelpBox(
                        "Combined: 하나의 Enum으로 통합 (Prefix 추가)\n" +
                        "예: AllAnimations.cs → Player_Idle, Enemy_Walk\n" +
                        "장점: 한 파일로 관리\n" +
                        "단점: Enum 값이 많아짐, 타입 안전성 낮음",
                        MessageType.None
                    );
                    addPrefix = EditorGUILayout.Toggle("Prefix 추가", addPrefix);
                    break;

                case GenerationMode.SmartCombined:
                    EditorGUILayout.HelpBox(
                        "Smart Combined: 공통 애니메이션 자동 감지 및 분리\n" +
                        "예: CommonAnimations.cs (Idle, Attack)\n" +
                        "    PlayerAnimations.cs (Shoot, Dash)\n" +
                        "장점: 중복 없음, 재사용성 높음\n" +
                        "단점: 파일이 여러 개 생성됨",
                        MessageType.None
                    );
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEnumSettings()
        {
            EditorGUILayout.LabelField("3. Enum 설정", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(GUI.skin.box);

            if (generationMode == GenerationMode.Individual)
            {
                EditorGUILayout.LabelField("각 Skeleton 이름에서 자동 생성됩니다.", EditorStyles.miniLabel);
            }
            else if (generationMode == GenerationMode.Combined)
            {
                enumName = EditorGUILayout.TextField("Enum 이름", enumName);
            }
            else // SmartCombined
            {
                commonEnumName = EditorGUILayout.TextField("공통 Enum 이름", commonEnumName);
                EditorGUILayout.LabelField("개별 Enum 이름은 Skeleton 이름에서 생성", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);

            includeNamespace = EditorGUILayout.Toggle("Namespace 포함", includeNamespace);

            if (includeNamespace)
            {
                namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOutputPath()
        {
            EditorGUILayout.LabelField("4. 출력 경로", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("저장 경로", outputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("저장 폴더 선택", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        outputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("5. 미리보기", EditorStyles.boldLabel);

            var validSkeletons = skeletonDataAssets.Where(s => s != null && s.GetSkeletonData(true) != null).ToList();

            if (validSkeletons.Count == 0)
            {
                EditorGUILayout.HelpBox("유효한 Skeleton Data가 없습니다.", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);

            int totalAnimations = validSkeletons.Sum(s => s.GetSkeletonData(true).Animations.Count);
            EditorGUILayout.LabelField($"총 {validSkeletons.Count}개 Skeleton, {totalAnimations}개 애니메이션", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            foreach (var skeleton in validSkeletons)
            {
                var skeletonData = skeleton.GetSkeletonData(true);
                var animations = skeletonData.Animations.ToArray();

                EditorGUILayout.LabelField($"▶ {skeleton.name} ({animations.Length}개)", EditorStyles.boldLabel);

                for (int i = 0; i < Mathf.Min(animations.Length, 5); i++)
                {
                    EditorGUILayout.LabelField($"  • {animations[i].Name}", EditorStyles.miniLabel);
                }

                if (animations.Length > 5)
                {
                    EditorGUILayout.LabelField($"  ... 외 {animations.Length - 5}개", EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGenerateButton()
        {
            EditorGUILayout.LabelField("6. 생성", EditorStyles.boldLabel);

            var validSkeletons = skeletonDataAssets.Where(s => s != null && s.GetSkeletonData(true) != null).ToList();

            GUI.enabled = validSkeletons.Count > 0;

            if (GUILayout.Button("Enum 코드 생성", GUILayout.Height(40)))
            {
                Generate();
            }

            GUI.enabled = true;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 생성 로직
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void Generate()
        {
            var validSkeletons = skeletonDataAssets.Where(s => s != null && s.GetSkeletonData(true) != null).ToList();

            if (validSkeletons.Count == 0)
            {
                EditorUtility.DisplayDialog("오류", "유효한 Skeleton Data가 없습니다.", "확인");
                return;
            }

            // 디렉토리 생성
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            switch (generationMode)
            {
                case GenerationMode.Individual:
                    GenerateIndividual(validSkeletons);
                    break;

                case GenerationMode.Combined:
                    GenerateCombined(validSkeletons);
                    break;

                case GenerationMode.SmartCombined:
                    GenerateSmartCombined(validSkeletons);
                    break;
            }

            AssetDatabase.Refresh();
        }

        // ━━━━━ Individual 모드 ━━━━━
        private void GenerateIndividual(List<SkeletonDataAsset> skeletons)
        {
            int fileCount = 0;

            foreach (var skeleton in skeletons)
            {
                string skeletonName = skeleton.name.Replace(" ", "");
                string fileName = $"{skeletonName}Animations";
                var skeletonData = skeleton.GetSkeletonData(true);
                var animations = skeletonData.Animations.ToArray();

                string code = GenerateEnumCode(
                    enumName: fileName,
                    animations: animations.Select(a => (a.Name, (string)null)).ToList(),
                    comment: $"{skeleton.name} 애니메이션 목록"
                );

                string fullPath = Path.Combine(outputPath, $"{fileName}.cs");
                File.WriteAllText(fullPath, code);
                fileCount++;

                Debug.Log($"[Individual] {fileName}.cs 생성 완료 ({animations.Length}개 애니메이션)");
            }

            EditorUtility.DisplayDialog(
                "생성 완료",
                $"Individual 모드로 {fileCount}개의 Enum 파일이 생성되었습니다!\n\n경로: {outputPath}",
                "확인"
            );
        }

        // ━━━━━ Combined 모드 ━━━━━
        private void GenerateCombined(List<SkeletonDataAsset> skeletons)
        {
            var allAnimations = new List<(string animName, string prefix)>();

            foreach (var skeleton in skeletons)
            {
                string prefix = addPrefix ? skeleton.name.Replace(" ", "") : null;
                var skeletonData = skeleton.GetSkeletonData(true);
                var animations = skeletonData.Animations.ToArray();

                foreach (var anim in animations)
                {
                    allAnimations.Add((anim.Name, prefix));
                }
            }

            string code = GenerateEnumCode(
                enumName: enumName,
                animations: allAnimations,
                comment: "모든 캐릭터 애니메이션 통합"
            );

            string fullPath = Path.Combine(outputPath, $"{enumName}.cs");
            File.WriteAllText(fullPath, code);

            EditorUtility.DisplayDialog(
                "생성 완료",
                $"Combined 모드로 1개의 통합 Enum이 생성되었습니다!\n\n경로: {fullPath}\n애니메이션 수: {allAnimations.Count}개",
                "확인"
            );

            Debug.Log($"[Combined] {enumName}.cs 생성 완료 ({allAnimations.Count}개 애니메이션)");
        }

        // ━━━━━ Smart Combined 모드 ━━━━━
        private void GenerateSmartCombined(List<SkeletonDataAsset> skeletons)
        {
            // 1. 모든 애니메이션 수집
            var allAnimsByCharacter = new Dictionary<string, List<string>>();

            foreach (var skeleton in skeletons)
            {
                string charName = skeleton.name.Replace(" ", "");
                var skeletonData = skeleton.GetSkeletonData(true);
                var animations = skeletonData.Animations.Select(a => a.Name).ToList();
                allAnimsByCharacter[charName] = animations;
            }

            // 2. 공통 애니메이션 찾기
            var commonAnimations = new HashSet<string>(allAnimsByCharacter.First().Value);
            foreach (var anims in allAnimsByCharacter.Values.Skip(1))
            {
                commonAnimations.IntersectWith(anims);
            }

            int fileCount = 0;

            // 3. 공통 Enum 생성
            if (commonAnimations.Count > 0)
            {
                var commonList = commonAnimations.Select(a => (a, (string)null)).ToList();
                string commonCode = GenerateEnumCode(
                    enumName: commonEnumName,
                    animations: commonList,
                    comment: "모든 캐릭터가 공통으로 가진 애니메이션"
                );

                string commonPath = Path.Combine(outputPath, $"{commonEnumName}.cs");
                File.WriteAllText(commonPath, commonCode);
                fileCount++;

                Debug.Log($"[SmartCombined] {commonEnumName}.cs 생성 ({commonAnimations.Count}개 공통 애니메이션)");
            }

            // 4. 개별 Enum 생성 (공통 제외)
            foreach (var kvp in allAnimsByCharacter)
            {
                string charName = kvp.Key;
                var uniqueAnimations = kvp.Value.Except(commonAnimations).ToList();

                if (uniqueAnimations.Count > 0)
                {
                    string fileName = $"{charName}Animations";
                    var uniqueList = uniqueAnimations.Select(a => (a, (string)null)).ToList();
                    string code = GenerateEnumCode(
                        enumName: fileName,
                        animations: uniqueList,
                        comment: $"{charName} 전용 애니메이션 (공통 제외)"
                    );

                    string fullPath = Path.Combine(outputPath, $"{fileName}.cs");
                    File.WriteAllText(fullPath, code);
                    fileCount++;

                    Debug.Log($"[SmartCombined] {fileName}.cs 생성 ({uniqueAnimations.Count}개 고유 애니메이션)");
                }
            }

            EditorUtility.DisplayDialog(
                "생성 완료",
                $"Smart Combined 모드로 {fileCount}개의 Enum 파일이 생성되었습니다!\n\n" +
                $"공통: {commonAnimations.Count}개\n" +
                $"경로: {outputPath}",
                "확인"
            );
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 코드 생성 유틸리티
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private string GenerateEnumCode(string enumName, List<(string animName, string prefix)> animations, string comment)
        {
            StringBuilder sb = new StringBuilder();

            // Namespace 시작
            if (includeNamespace && !string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine($"namespace {namespaceName}");
                sb.AppendLine("{");
            }

            // Enum 정의
            int indent = includeNamespace ? 1 : 0;
            string indentStr = new string(' ', indent * 4);

            sb.AppendLine($"{indentStr}/// <summary>");
            sb.AppendLine($"{indentStr}/// {comment}");
            sb.AppendLine($"{indentStr}/// 자동 생성됨 - SpineAnimationEnumGenerator ({generationMode} 모드)");
            sb.AppendLine($"{indentStr}/// </summary>");
            sb.AppendLine($"{indentStr}public enum {enumName}");
            sb.AppendLine($"{indentStr}{{");

            // 애니메이션 목록
            for (int i = 0; i < animations.Count; i++)
            {
                var (animName, prefix) = animations[i];
                string enumValue = string.IsNullOrEmpty(prefix)
                    ? SanitizeEnumName(animName)
                    : $"{SanitizeEnumName(prefix)}_{SanitizeEnumName(animName)}";

                sb.AppendLine($"{indentStr}    /// <summary>{animName}</summary>");
                sb.AppendLine($"{indentStr}    {enumValue}{(i < animations.Count - 1 ? "," : "")}");

                if (i < animations.Count - 1)
                    sb.AppendLine();
            }

            sb.AppendLine($"{indentStr}}}");

            // Namespace 종료
            if (includeNamespace && !string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 애니메이션 이름을 Enum 값으로 변환 (특수문자 제거, 첫글자 대문자)
        /// </summary>
        private string SanitizeEnumName(string animName)
        {
            if (string.IsNullOrEmpty(animName))
                return "Unknown";

            StringBuilder sb = new StringBuilder();
            bool capitalizeNext = true;

            foreach (char c in animName)
            {
                if (char.IsLetterOrDigit(c))
                {
                    if (capitalizeNext)
                    {
                        sb.Append(char.ToUpper(c));
                        capitalizeNext = false;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else if (c == '_' || c == '-' || c == ' ')
                {
                    sb.Append('_');
                    capitalizeNext = true;
                }
            }

            string result = sb.ToString();

            // 빈 결과 체크
            if (string.IsNullOrEmpty(result))
                return "Unknown";

            // 숫자로 시작하면 앞에 _ 추가
            if (char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            return result;
        }
    }
}
#endif
