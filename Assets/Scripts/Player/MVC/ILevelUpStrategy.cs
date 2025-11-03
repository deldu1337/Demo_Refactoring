public interface ILevelUpStrategy
{
    void ApplyLevelUp(PlayerData data);
}

public class DefaultLevelUpStrategy : ILevelUpStrategy
{
    public void ApplyLevelUp(PlayerData data)
    {
        data.Level++;
        data.ExpToNextLevel = 50f * data.Level;
        data.MaxHP += 10f;
        data.MaxMP += 5f;
        data.Atk += 2f;
        data.Def += 1f;
        data.CurrentHP = data.MaxHP;
        data.CurrentMP = data.MaxMP;
    }
}