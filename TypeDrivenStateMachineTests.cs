using System;
using System.Collections.Generic;
using DomainLogic.Computation;
using Xunit;

namespace DomainLogic.Tests.Computation
{
    public class SwitchTest
    {
        private enum State
        {
            On,
            Off
        }

        private class Toggle
        {
        }

        [Fact]
        public void Works()
        {
            int Inc(int count, Toggle toggle) => count + 1;

            TypeDrivenStateMachine<State, int> machine =
                TypeDrivenStateMachine<State, int>.Builder()
                    .Register<Toggle>(State.On, State.Off, Inc)
                    .Register<Toggle>(State.Off, State.On, Inc)
                    .Build(State.Off, 0);

            Assert.Equal(State.Off, machine.State);
            Assert.Equal(0, machine.Context);

            machine.Apply(new Toggle());

            Assert.Equal(State.On, machine.State);
            Assert.Equal(1, machine.Context);

            machine.Apply(new Toggle());

            Assert.Equal(State.Off, machine.State);
            Assert.Equal(2, machine.Context);
        }

        [Fact]
        public void InvalidInput()
        {
            int Inc(int count, Toggle toggle) => count + 1;

            TypeDrivenStateMachine<State, int> machine =
                TypeDrivenStateMachine<State, int>.Builder()
                    .Register<Toggle>(State.On, State.Off, Inc)
                    .Register<Toggle>(State.Off, State.On, Inc)
                    .Build(State.Off, 0);

            Assert.False(machine.IsValid("foo"));

            InvalidInputException exception = Assert.Throws<InvalidInputException>(() => machine.Apply("foo"));

            Assert.Equal("foo", exception.Input);
            Assert.Equal(State.Off, exception.State);
            Assert.Equal(new List<Type> { typeof(Toggle) }, exception.ValidInputTypes);
            Assert.Equal("Value \"foo\" of type \"System.String\" is not a valid input from state \"Off\". Valid input types: Toggle", exception.Message);
        }


        [Fact]
        public void Dumb()
        {
            int Inc(int count, Toggle toggle) => count + 1;

            TypeDrivenStateMachine<State, int> machine =
                TypeDrivenStateMachine<State, int>.Builder()
                    .Register<Toggle>(State.On, State.Off, Inc)
                    .Register<Toggle>(State.Off, State.On, Inc)
                    .Build(State.Off, 0);

            object aToggle = new Toggle();

            Console.WriteLine("here");
            machine.Apply(aToggle);

            Console.WriteLine("State: " + machine.State);
        }
    }
}