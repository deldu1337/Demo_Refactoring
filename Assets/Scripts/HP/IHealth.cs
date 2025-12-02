/// <summary>
/// 체력 정보를 제공하는 객체들이 구현해야 하는 인터페이스입니다.
/// </summary>
public interface IHealth
{
    /// <summary>
    /// 현재 체력 값입니다.
    /// </summary>
    float CurrentHP { get; }

    /// <summary>
    /// 최대 체력 값입니다.
    /// </summary>
    float MaxHP { get; }
}
