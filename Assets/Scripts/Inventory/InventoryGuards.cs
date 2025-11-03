public static class InventoryGuards
{
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