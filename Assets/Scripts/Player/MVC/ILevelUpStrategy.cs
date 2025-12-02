/// <summary>레벨업 시 적용할 규칙을 정의합니다.</summary>
public interface ILevelUpStrategy
{
    /// <summary>전달된 플레이어 데이터에 레벨업 처리를 적용합니다.</summary>
    /// <param name="data">레벨업 수치가 반영될 플레이어 데이터입니다.</param>
    void ApplyLevelUp(PlayerData data);
}

public class DefaultLevelUpStrategy : ILevelUpStrategy
{
    /// <summary>기본 레벨업 규칙을 적용하여 스탯을 증가시킵니다.</summary>
    /// <param name="data">증가된 수치를 반영할 플레이어 데이터입니다.</param>
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