using System;
using System.Collections.Generic;
using UnityEngine;

public class UIEscapeStack : MonoBehaviour
{
    public static UIEscapeStack Instance { get; private set; }

    private class Entry
    {
        public string key;
        public Action close;
        public Func<bool> isOpen;
    }

    private readonly List<Entry> _stack = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>없으면 만든다. 어디서든 Instance 보장용.</summary>
    public static UIEscapeStack GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("UIEscapeStack");
        return go.AddComponent<UIEscapeStack>();
    }

    /// <summary>UI를 열 때 호출: ESC로 닫을 액션을 스택에 등록</summary>
    public void Push(string key, Action close, Func<bool> isOpen = null)
    {
        if (string.IsNullOrEmpty(key) || close == null) return;
        Remove(key); // 중복 방지
        _stack.Add(new Entry { key = key, close = close, isOpen = isOpen });
    }

    /// <summary>UI를 닫을 때 호출: 스택에서 제거</summary>
    public void Remove(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        _stack.RemoveAll(e => e.key == key);
    }

    /// <summary>가장 최근 항목을 닫는다. 닫을 것이 없으면 false.</summary>
    public bool PopTop()
    {
        // 뒤에서부터 스캔 (가장 최근)
        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            var e = _stack[i];
            // 이미 닫혀 있다면 버리고 다음
            if (e.isOpen != null && !e.isOpen())
            {
                _stack.RemoveAt(i);
                continue;
            }
            _stack.RemoveAt(i);
            try { e.close?.Invoke(); } catch (Exception ex) { Debug.LogException(ex); }
            return true;
        }
        return false;
    }

    public bool IsEmpty => _stack.Count == 0;
}
