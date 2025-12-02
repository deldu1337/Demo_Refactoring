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

    /// <summary>
    /// 인스턴스를 보존하고 중복 객체를 제거합니다.
    /// </summary>
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

    /// <summary>
    /// 전역 인스턴스를 반환하거나 없으면 새로 만듭니다.
    /// </summary>
    public static UIEscapeStack GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("UIEscapeStack");
        return go.AddComponent<UIEscapeStack>();
    }

    /// <summary>
    /// ESC로 닫을 수 있는 UI를 스택에 등록합니다.
    /// </summary>
    public void Push(string key, Action close, Func<bool> isOpen = null)
    {
        if (string.IsNullOrEmpty(key) || close == null) return;
        Remove(key);
        _stack.Add(new Entry { key = key, close = close, isOpen = isOpen });
    }

    /// <summary>
    /// 등록된 UI를 스택에서 제거합니다.
    /// </summary>
    public void Remove(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        _stack.RemoveAll(e => e.key == key);
    }

    /// <summary>
    /// 스택의 상단 UI를 닫고 처리 여부를 반환합니다.
    /// </summary>
    public bool PopTop()
    {
        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            var e = _stack[i];
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

    /// <summary>
    /// 스택이 비어 있는지 여부입니다.
    /// </summary>
    public bool IsEmpty => _stack.Count == 0;
}
