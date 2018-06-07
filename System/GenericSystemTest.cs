using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using DomainLogic.System;
using log4net;
using log4net.Config;
using log4net.Core;
using log4net.Repository;
using Xunit;
using Xunit.Abstractions;

namespace DomainLogic.Tests
{
    public class GenericSystemTest
    {
        public GenericSystemTest()
        {
            ILoggerRepository logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository);
        }

        private enum Status
        {
            On,
            Off
        }

        private enum Input
        {
            Toggle
        }

        [Fact]
        public void Works()
        {
            BlockingCollection<Input> inputQueue = new BlockingCollection<Input>(new ConcurrentQueue<Input>());

            GenericSystem<Status, int, Input> system = GenericSystem<Status, int, Input>.Create()
                .Register(Status.On, (state, schedule, input) =>
                {
                    switch (input)
                    {
                        case Input.Toggle:
                            return state.To(Status.Off, state.Context + 1);
                        default:
                            return state;
                    }
                })
                .Register(Status.Off, (state, schedule, input) =>
                {
                    switch (input)
                    {
                        case Input.Toggle:
                            return state.To(Status.On, state.Context + 1);
                        default:
                            return state;
                    }
                })
                .Start(inputQueue, Status.Off, 0);

            Assert.Equal(Status.Off, system.Status);
            Assert.Equal(0, system.Context);

            system.Dispatch(Input.Toggle);

            Thread.Sleep(100);

            Assert.Equal(Status.On, system.Status);
            Assert.Equal(1, system.Context);

            bool changed = false;

            system.Schedule((state, dispatch) =>
            {
                Thread.Sleep(200);
                changed = true;
                dispatch(Input.Toggle);
            });

            Thread.Sleep(100);

            Assert.Equal(Status.On, system.Status);
            Assert.Equal(1, system.Context);
            Assert.False(changed);

            Thread.Sleep(150);

            Assert.Equal(Status.Off, system.Status);
            Assert.Equal(2, system.Context);
            Assert.True(changed);
        }
    }
}