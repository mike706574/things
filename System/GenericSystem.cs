using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;

namespace DomainLogic.System
{
    public static class GenericSystem {}

    public class GenericSystem<TStatus, TContext, TInput> : IDisposable
    {
        // TODO: This seems like a hack.
        private static readonly ILog Log = LogManager.GetLogger(typeof(GenericSystem));

        // Fields
        private readonly Dictionary<TStatus, Transition<TStatus, TContext, TInput>> _transitions;
        private readonly IList<Task> _tasks;

        private CancelableTask _mainTask;
        private BlockingCollection<TInput> _inputQueue;
        private BlockingCollection<DispatchingFunction<TStatus, TContext, TInput>> _taskQueue;

        // Properties
        public TStatus Status { get; private set; }
        public TContext Context { get; private set; }

        // Constructors
        public GenericSystem()
        {
            _transitions = new Dictionary<TStatus, Transition<TStatus, TContext, TInput>>();
            _tasks = new List<Task>();
            _inputQueue = null;
            _taskQueue = new BlockingCollection<DispatchingFunction<TStatus, TContext, TInput>>();
            _mainTask = null;
        }

        public static GenericSystem<TStatus, TContext, TInput> Create()
        {
            return new GenericSystem<TStatus, TContext, TInput>();
        }

        // Methods
        public GenericSystem<TStatus, TContext, TInput> Register(TStatus status,
            Transition<TStatus, TContext, TInput> transition)
        {
            _transitions.Add(status, transition);
            return this;
        }

        public void Schedule(DispatchingFunction<TStatus, TContext, TInput> action)
        {
            AssertStarted("schedule tasks");
            _taskQueue.Add(action);
        }

        public void Dispatch(TInput input)
        {
            AssertStarted("dispatch input events");
            _inputQueue.Add(input);
        }

        public bool IsStarted()
        {
            return _inputQueue != null;
        }

        // Lifecycle
        public GenericSystem<TStatus, TContext, TInput> Start(BlockingCollection<TInput> queue, TStatus status,
            TContext context)
        {
            if (_inputQueue != null)
            {
                throw new InvalidOperationException($"System must be stopped to start.");
            }

            _inputQueue = queue;
            Status = status;
            Context = context;

            _mainTask = CancelableTask.Start(token =>
            {
                Log.Debug("Starting state machine loop.");

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        bool inputPresent = _inputQueue.TryTake(out TInput input, 10, token);

                        if (inputPresent)
                        {
                            Log.Debug($"Applying input: {input}");

                            TStatus originalStatus = Status;

                            Transition<TStatus, TContext, TInput> transition = _transitions[Status];

                            SystemState<TStatus, TContext> currentState = new SystemState<TStatus, TContext>(Status, Context);

                            SystemState<TStatus, TContext> nextState = transition.Invoke(currentState, Schedule, input);

                            Status = nextState.Status;
                            Context = nextState.Context;

                            Log.Debug($"Transition: {originalStatus} => {Status}");
                        }

                        while (true)
                        {
                            bool taskPresent = _taskQueue.TryTake(out DispatchingFunction<TStatus, TContext, TInput> action, 10, token);

                            if(taskPresent) {
                                Log.Debug("Running task: " + action);

                                Task task = Task.Run(() =>
                                {
                                    try
                                    {
                                        SystemState<TStatus, TContext> currentState = new SystemState<TStatus, TContext>(Status, Context);
                                        action.Invoke(currentState, Dispatch);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Exception: " + ex);
                                    }
                                }, token);
                                _tasks.Add(task);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("TODO: Unexpected exception during main loop.", ex);
                    }
                }

                Log.Debug("Finished state machine loop.");
            });

            return this;
        }

        public void Stop()
        {
            AssertStarted("stop it");

            Log.Debug("Waiting for main loop to finish up.");
            _mainTask.CancelAndWait();

            Log.Debug("Waiting for all tasks to finish up.");
            foreach (Task task in _tasks)
            {
                Log.Debug($"Waiting for a task.");
                task.Wait();
            }

            Log.Debug("Disposing task queue.");
            _taskQueue.Dispose();

            _mainTask = null;
            _inputQueue = null;
        }

        public void Dispose()
        {
            if (_inputQueue != null)
            {
                Stop();
            }
        }

        // Private methods
        private void AssertStarted(string action)
        {
            if (_inputQueue == null)
            {
                throw new InvalidOperationException($"System must be started to {action}.");
            }
        }
    }
}