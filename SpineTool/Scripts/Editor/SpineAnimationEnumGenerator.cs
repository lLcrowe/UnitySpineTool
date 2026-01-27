#if SPINE_UNITY
using UnityEngine;
using UnityEditor;
using Spine.Unity;
using System.IO;
using System.Text;
using System.Linq;

namespace SpineTool.Editor
{
    /// <summary>
    /// SkeletonAnimation에서 애니메이션 목록을 읽어서 Enum 코드를 자동 생성하는 에디터 윈도우
    /// </summary>
    public class SpineAnimationEnumGenerator : EditorWindow
    {
        private SkeletonAnimation targetSkeleton;
        private string enumName = "PlayerAnimations";
        private string namespaceName = "Game.Animations";
        private string outputPath = "Assets/Scripts/Animations";
        private bool includeNamespace = true;
        private Vector2 scrollPosition;

        [MenuItem("Tools/SpineTool/Animation Enum Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpineAnimationEnumGenerator>("Anim Enum Generator");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // 현재 선택된 오브젝트 확인
            if (Selection.activeGameObject != null)
            {
                targetSkeleton = Selection.activeGameObject.GetComponent<SkeletonAnimation>();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Spine Animation Enum Generator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("SkeletonAnimation에서 애니메이션 목록을 읽어서 Enum 코드를 자동 생성합니다.", MessageType.Info);

            EditorGUILayout.Space(10);

            // ━━━━━ 1. Skeleton 선택 ━━━━━
            EditorGUILayout.LabelField("1. Skeleton 선택", EditorStyles.boldLabel);

            var newSkeleton = (SkeletonAnimation)EditorGUILayout.ObjectField(
                "Skeleton Animation",
                targetSkeleton,
                typeof(SkeletonAnimation),
                true
            );

            if (newSkeleton != targetSkeleton)
            {
                targetSkeleton = newSkeleton;

                // Skeleton 이름으로 Enum 이름 자동 설정
                if (targetSkeleton != null && targetSkeleton.skeletonDataAsset != null)
                {
                    string skeletonName = targetSkeleton.skeletonDataAsset.name;
                    enumName = skeletonName.Replace(" ", "") + "Animations";
                }
            }

            EditorGUILayout.Space(10);

            // ━━━━━ 2. Enum 설정 ━━━━━
            EditorGUILayout.LabelField("2. Enum 설정", EditorStyles.boldLabel);

            enumName = EditorGUILayout.TextField("Enum 이름", enumName);

            includeNamespace = EditorGUILayout.Toggle("Namespace 포함", includeNamespace);

            if (includeNamespace)
            {
                namespaceName = EditorGUILayout.TextField("Namespace", namespaceName);
            }

            EditorGUILayout.Space(10);

            // ━━━━━ 3. 출력 경로 ━━━━━
            EditorGUILayout.LabelField("3. 출력 경로", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            outputPath = EditorGUILayout.TextField("저장 경로", outputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("저장 폴더 선택", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 절대 경로를 상대 경로로 변환
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        outputPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // ━━━━━ 4. 미리보기 ━━━━━
            if (targetSkeleton != null && targetSkeleton.Skeleton != null)
            {
                EditorGUILayout.LabelField("4. 미리보기", EditorStyles.boldLabel);

                var animations = targetSkeleton.Skeleton.Data.Animations.ToArray();

                EditorGUILayout.HelpBox($"총 {animations.Length}개의 애니메이션이 발견되었습니다.", MessageType.Info);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("애니메이션 목록:", EditorStyles.boldLabel);

                for (int i = 0; i < Mathf.Min(animations.Length, 10); i++)
                {
                    EditorGUILayout.LabelField($"  • {animations[i].Name}");
                }

                if (animations.Length > 10)
                {
                    EditorGUILayout.LabelField($"  ... 외 {animations.Length - 10}개");
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);

                // ━━━━━ 5. 생성 ━━━━━
                EditorGUILayout.LabelField("5. 생성", EditorStyles.boldLabel);

                if (GUILayout.Button("Enum 코드 생성", GUILayout.Height(40)))
                {
                    GenerateEnumCode();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("SkeletonAnimation을 선택해주세요.", MessageType.Warning);
            }

            EditorGUILayout.EndScrollView();
        }

        private void GenerateEnumCode()
        {
            if (targetSkeleton == null || targetSkeleton.Skeleton == null)
            {
                EditorUtility.DisplayDialog("오류", "SkeletonAnimation이 유효하지 않습니다.", "확인");
                return;
            }

            if (string.IsNullOrEmpty(enumName))
            {
                EditorUtility.DisplayDialog("오류", "Enum 이름을 입력해주세요.", "확인");
                return;
            }

            var animations = targetSkeleton.Skeleton.Data.Animations.ToArray();

            if (animations.Length == 0)
            {
                EditorUtility.DisplayDialog("오류", "애니메이션이 없습니다.", "확인");
                return;
            }

            // 코드 생성
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
            sb.AppendLine($"{indentStr}/// {targetSkeleton.skeletonDataAsset.name} 애니메이션 목록");
            sb.AppendLine($"{indentStr}/// 자동 생성됨 - SpineAnimationEnumGenerator");
            sb.AppendLine($"{indentStr}/// </summary>");
            sb.AppendLine($"{indentStr}public enum {enumName}");
            sb.AppendLine($"{indentStr}{{");

            // 애니메이션 목록
            for (int i = 0; i < animations.Length; i++)
            {
                string animName = animations[i].Name;
                string enumValue = SanitizeEnumName(animName);

                sb.AppendLine($"{indentStr}    /// <summary>{animName}</summary>");
                sb.AppendLine($"{indentStr}    {enumValue}{(i < animations.Length - 1 ? "," : "")}");

                if (i < animations.Length - 1)
                    sb.AppendLine();
            }

            sb.AppendLine($"{indentStr}}}");

            // Namespace 종료
            if (includeNamespace && !string.IsNullOrEmpty(namespaceName))
            {
                sb.AppendLine("}");
            }

            // 파일 저장
            string fullPath = Path.Combine(outputPath, $"{enumName}.cs");

            // 디렉토리 생성
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            File.WriteAllText(fullPath, sb.ToString());

            // Asset 새로고침
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "생성 완료",
                $"Enum 코드가 생성되었습니다!\n\n경로: {fullPath}\n애니메이션 수: {animations.Length}개",
                "확인"
            );

            // 생성된 파일 선택
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(fullPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        /// <summary>
        /// 애니메이션 이름을 Enum 값으로 변환 (특수문자 제거, 첫글자 대문자)
        /// </summary>
        private string SanitizeEnumName(string animName)
        {
            // 빈 문자열 체크
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
