namespace DomainLogic.System
{
    public delegate SystemState<TStatus, TContext> Transition<TStatus, TContext, TInput>(
        SystemState<TStatus, TContext> state,
        Schedule<TStatus, TContext, TInput> schedule,
        TInput input);
}