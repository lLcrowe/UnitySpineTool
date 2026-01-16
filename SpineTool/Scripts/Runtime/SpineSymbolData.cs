using System;
using System.Collections.Generic;
using UnityEngine;

namespace InteractAnimation.AnimationSystems.Spine
{
    /// <summary>
    /// Spine 애니메이션의 심볼 데이터를 관리하는 클래스
    /// 각 애니메이션에 대한 메타데이터와 심볼 정보를 저장
    /// </summary>
    [Serializable]
    public class SpineSymbolData
    {
        [Header("Symbol Information")]
        [Tooltip("심볼의 고유 ID")]
        public string symbolId;

        [Tooltip("심볼 이름")]
        public string symbolName;

        [Tooltip("심볼 설명")]
        [TextArea(3, 5)]
        public string description;

        [Header("Animation Mapping")]
        [Tooltip("이 심볼과 연결된 애니메이션 이름")]
        public string animationName;

        [Tooltip("애니메이션 재생 시간 (초)")]
        public float duration;

        [Tooltip("애니메이션 반복 여부")]
        public bool isLooping;

        [Header("Symbol Properties")]
        [Tooltip("심볼 우선순위 (높을수록 우선)")]
        public int priority;

        [Tooltip("심볼 태그들")]
        public List<string> tags = new List<string>();

        [Header("Trigger Settings")]
        [Tooltip("특정 이벤트에서 트리거될 수 있는지 여부")]
        public bool canBeTriggered = true;

        [Tooltip("트리거 쿨다운 시간")]
        public float triggerCooldown = 0f;

        [Header("Advanced Settings")]
        [Tooltip("커스텀 애니메이션 속도")]
        public float customSpeed = 1f;

        [Tooltip("Spine 스킨 이름 (선택사항)")]
        public string skinName = "";

        [Tooltip("애니메이션 블렌딩 시간")]
        public float blendDuration = 0.2f;

        /// <summary>
        /// 심볼 데이터의 유효성 검증
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(symbolId) &&
                   !string.IsNullOrEmpty(symbolName) &&
                   !string.IsNullOrEmpty(animationName);
        }

        /// <summary>
        /// 특정 태그를 포함하는지 확인
        /// </summary>
        public bool HasTag(string tag)
        {
            return tags.Contains(tag);
        }

        /// <summary>
        /// 태그 추가
        /// </summary>
        public void AddTag(string tag)
        {
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        /// <summary>
        /// 태그 제거
        /// </summary>
        public void RemoveTag(string tag)
        {
            tags.Remove(tag);
        }
    }

    /// <summary>
    /// Spine 심볼 데이터 컬렉션을 관리하는 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "SpineSymbolCollection", menuName = "InteractAnimation/Spine Symbol Collection")]
    public class SpineSymbolCollection : ScriptableObject
    {
        [Header("Symbol Collection")]
        [Tooltip("모든 심볼 데이터 목록")]
        public List<SpineSymbolData> symbols = new List<SpineSymbolData>();

        private Dictionary<string, SpineSymbolData> symbolDictionary;

        /// <summary>
        /// 초기화 (딕셔너리 생성)
        /// </summary>
        public void Initialize()
        {
            symbolDictionary = new Dictionary<string, SpineSymbolData>();
            foreach (var symbol in symbols)
            {
                if (symbol.IsValid() && !symbolDictionary.ContainsKey(symbol.symbolId))
                {
                    symbolDictionary.Add(symbol.symbolId, symbol);
                }
            }
        }

        /// <summary>
        /// ID로 심볼 데이터 가져오기
        /// </summary>
        public SpineSymbolData GetSymbolById(string symbolId)
        {
            if (symbolDictionary == null)
            {
                Initialize();
            }

            symbolDictionary.TryGetValue(symbolId, out SpineSymbolData symbol);
            return symbol;
        }

        /// <summary>
        /// 애니메이션 이름으로 심볼 데이터 가져오기
        /// </summary>
        public SpineSymbolData GetSymbolByAnimationName(string animationName)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.animationName == animationName)
                {
                    return symbol;
                }
            }
            return null;
        }

        /// <summary>
        /// 특정 태그를 가진 모든 심볼 가져오기
        /// </summary>
        public List<SpineSymbolData> GetSymbolsByTag(string tag)
        {
            List<SpineSymbolData> result = new List<SpineSymbolData>();
            foreach (var symbol in symbols)
            {
                if (symbol.HasTag(tag))
                {
                    result.Add(symbol);
                }
            }
            return result;
        }

        /// <summary>
        /// 우선순위에 따라 정렬된 심볼 목록 가져오기
        /// </summary>
        public List<SpineSymbolData> GetSymbolsSortedByPriority()
        {
            List<SpineSymbolData> sortedSymbols = new List<SpineSymbolData>(symbols);
            sortedSymbols.Sort((a, b) => b.priority.CompareTo(a.priority));
            return sortedSymbols;
        }
    }
}
