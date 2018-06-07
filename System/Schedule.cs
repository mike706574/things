namespace DomainLogic.System
{
    public delegate void Schedule<TStatus, TContext, in TInput>(DispatchingFunction<TStatus, TContext, TInput> action);
}