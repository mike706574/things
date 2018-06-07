namespace DomainLogic.System
{
    public delegate void DispatchingFunction<TStatus, TContext, out TInput>(SystemState<TStatus, TContext> state,
        Dispatch<TInput> dispatch);
}