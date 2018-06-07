namespace DomainLogic.System
{
    public static class SystemState
    {
        public static SystemState<TStatus, TContext> Create<TStatus, TContext>(TStatus status, TContext context)
        {
            return new SystemState<TStatus, TContext>(status, context);
        }
    }

    public class SystemState<TStatus, TContext>
    {
        public TStatus Status { get; private set; }
        public TContext Context { get; private set; }


        public SystemState(TStatus status, TContext context)
        {
            Status = status;
            Context = context;
        }

        public SystemState<TStatus, TContext> To(TStatus status, TContext context)
        {
            return new SystemState<TStatus, TContext>(status, context);
        }

        public SystemState<TStatus, TContext> To(TStatus status)
        {
            return new SystemState<TStatus, TContext>(status, Context);
        }

        public SystemState<TStatus, TContext> To(TContext context)
        {
            return new SystemState<TStatus, TContext>(Status, context);
        }
    }
}