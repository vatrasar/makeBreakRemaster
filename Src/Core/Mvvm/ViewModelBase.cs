using ReactiveUI;

namespace makeBreak.Src.Core.Mvvm;

public abstract class ViewModelBase : ReactiveObject
{
}

public abstract class ViewModelBase<TState> : ViewModelBase where TState : class
{
    protected ViewModelBase(TState initialState) => State = initialState;

    public TState State { get; private set; }

    protected void UpdateState(Func<TState, TState> update)
    {
        State = update(State);
        this.RaisePropertyChanged(nameof(State));
    }
}
