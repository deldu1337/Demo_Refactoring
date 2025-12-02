/// <summary>
/// 인벤토리 아이템의 유효성을 검사하는 도우미 클래스입니다.
/// </summary>
public static class InventoryGuards
{
    /// <summary>
    /// 아이템이 null이거나 필수 속성이 비어 있으면 true를 반환합니다.
    /// </summary>
    public static bool IsInvalid(InventoryItem it)
    {
        if (it == null) return true;
        if (string.IsNullOrWhiteSpace(it.uniqueId)) return true;
        if (it.data == null) return true;
        if (it.data.id <= 0) return true;
        if (string.IsNullOrWhiteSpace(it.data.type)) return true;
        return false;
    }
}
