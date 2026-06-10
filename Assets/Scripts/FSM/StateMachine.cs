public interface IState<in TOwner>
{
    void Enter(TOwner owner);
    void Tick(TOwner owner);
    void FixedTick(TOwner owner);
    void Exit(TOwner owner);
}

public sealed class StateMachine<TOwner>
{
    private readonly TOwner owner;

    public IState<TOwner> CurrentState { get; private set; }

    public StateMachine(TOwner owner)
    {
        this.owner = owner;
    }

    public void ChangeState(IState<TOwner> nextState)
    {
        if (nextState == null || ReferenceEquals(CurrentState, nextState))
            return;

        CurrentState?.Exit(owner);
        CurrentState = nextState;
        CurrentState.Enter(owner);
    }

    public void Tick()
    {
        CurrentState?.Tick(owner);
    }

    public void FixedTick()
    {
        CurrentState?.FixedTick(owner);
    }
}
