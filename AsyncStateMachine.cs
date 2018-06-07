using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DomainLogic.Computation.System
{
    public class AsyncStateMachine<TState, TContext>
    {
        private class Transition<TInput>
        {
            public string Id { get; set; }
            public TState State { get; set; }
            public Func<TContext, TInput, TContext> Apply { get; set; }
        }

        private readonly IDictionary<TState, IDictionary<Type, object>> _transitions;

        private readonly IDictionary<string, Task> _tasks;

        public TState State { get; private set; }

        public TContext Context { get; private set; }

        public AsyncStateMachine(IDictionary<TState, IDictionary<Type, object>> transitions,
            TState state,
            TContext context)
        {
            _transitions = new Dictionary<TState, IDictionary<Type, object>>();
            State = state;
            Context = context;
        }

        public AsyncStateMachine<TState, TContext> Register<TInput>(string id, TState inputState, TState outputState)
        {
            return Register<TInput>(id, inputState, outputState, (ctx, input) => ctx);
        }

        public AsyncStateMachine<TState, TContext> Register<TInput>(string id, TState inputState, TState outputState,
            Func<TContext, TInput, TContext> apply)
        {
            Type inputType = typeof(TInput);

            if (!_transitions.ContainsKey(inputState))
            {
                _transitions[inputState] = new Dictionary<Type, object>();
            }

            IDictionary<Type, object> transitionsFromState = _transitions[inputState];

            Transition<TInput> transition =
                new Transition<TInput>
                {
                    Id = id,
                    State = outputState,
                    Apply = apply
                };

            transitionsFromState[inputType] = transition;

            return this;
        }

        public bool IsValid<TInput>(TInput input)
        {
            return TransitionFor<TInput>() != null;
        }

        public AsyncStateMachine<TState, TContext> Apply<TInput>(TInput input)
        {
            Transition<TInput> transition = TransitionFor<TInput>();

            if (transition == null)
            {
                throw new InvalidInputException(input, State, _transitions[State].Keys.ToList());
            }

            State = transition.State;
            Context = transition.Apply(Context, input);

            return this;
        }

        public AsyncStateMachine<TState, TContext> Schedule<TInput>(string id, Action action)
        {
            if (_tasks.ContainsKey(id))
            {
                throw new TaskAlreadyRunningException(id);
            }

            _tasks.Add(id, Task.Run(action));
            return this;
        }

        private Transition<TInput> TransitionFor<TInput>()
        {
            Type inputType = typeof(TInput);

            IDictionary<Type, object> transitionsFromState = _transitions[State];

            bool found = transitionsFromState.TryGetValue(inputType, out object transitionObject);

            return found ? (Transition<TInput>) transitionObject : null;
        }
    }

    public class InvalidInputException : Exception
    {
        public object Input { get; }
        public object State { get; }
        public IList<Type> ValidInputTypes { get; }

        public InvalidInputException(object input, object state, IList<Type> validInputTypes) : base(
            BuildMessage(input, state, validInputTypes))
        {
            Input = input;
            State = state;
            ValidInputTypes = validInputTypes;
        }

        private static string BuildMessage(object input, object state, IEnumerable<Type> validInputTypes)
        {
            Type inputType = input.GetType();
            string validTypeListing =
                string.Join(", ", validInputTypes.Select(validInputType => validInputType.Name));
            return
                $"Value \"{input}\" of type \"{inputType}\" is not a valid input from state \"{state}\". Valid input types: {validTypeListing}";
        }
    }

    public class TaskAlreadyRunningException : Exception
    {
        public string Id { get; }

        public TaskAlreadyRunningException(string id) : base(BuildMessage(id))
        {
            Id = id;
        }

        private static string BuildMessage(string id)
        {
            return $"Task \"{id}\" is already running.";
        }
    }
}