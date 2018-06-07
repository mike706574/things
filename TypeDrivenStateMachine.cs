using System;
using System.Collections.Generic;
using System.Linq;

namespace DomainLogic.Computation
{
    public class TypeDrivenStateMachine<TState, TContext>
    {
        private class Transition<TInput>
        {
            public TState State { get; set; }
            public Func<TContext, TInput, TContext> Apply { get; set; }
        }

        public class StateMachineBuilder
        {
            private readonly IDictionary<TState, IDictionary<Type, object>> _transitions =
                new Dictionary<TState, IDictionary<Type, object>>();

            public StateMachineBuilder Register<TInput>(TState inputState, TState outputState)
            {
                Register<TInput>(inputState, outputState, (ctx, input) => ctx);
                return this;
            }

            public StateMachineBuilder Register<TInput>(TState inputState, TState outputState,
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
                        State = outputState,
                        Apply = apply
                    };

                transitionsFromState[inputType] = transition;

                return this;
            }

            public TypeDrivenStateMachine<TState, TContext> Build(TState initialState, TContext initialContext)
            {
                return new TypeDrivenStateMachine<TState, TContext>(_transitions,
                    initialState,
                    initialContext);
            }
        }

        private readonly IDictionary<TState, IDictionary<Type, object>> _transitions;

        public TypeDrivenStateMachine(IDictionary<TState, IDictionary<Type, object>> transitions,
            TState state,
            TContext context)
        {
            _transitions = transitions;
            State = state;
            Context = context;
        }

        public static StateMachineBuilder Builder()
        {
            return new StateMachineBuilder();
        }

        public TState State { get; private set; }

       public TContext Context { get; private set; }

        public bool IsValid<TInput>(TInput input)
        {
            return TransitionFor(input) != null;
        }

        public TypeDrivenStateMachine<TState, TContext> Apply<TInput>(TInput input)
        {
            Console.WriteLine("Applying");

            Type type = input.GetType();

            Transition<object> transition = TransitionFor(input);

            Console.WriteLine("Got transaction");

            Console.WriteLine("Input: " + input.GetType());

            if (transition == null)
            {
                Console.WriteLine("Throwing");
                throw new InvalidInputException(input, State, _transitions[State].Keys.ToList());
            }

            Console.WriteLine("applying");
            State = transition.State;
            Context = transition.Apply(Context, input);

            Console.WriteLine("Returning");
            return this;
        }

        private Transition<object> TransitionFor(object input)
        {
            Type inputType = input.GetType();

            Console.WriteLine("Type: " + inputType);
            IDictionary<Type, object> transitionsFromState = _transitions[State];

            bool found = transitionsFromState.TryGetValue(inputType, out object transitionObject);

            return found ? (Transition<object>) transitionObject : null;
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
}