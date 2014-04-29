﻿using System;
using System.Collections.Generic;
using System.Linq;
using EventStore.Common.Options;
using EventStore.Core.Bus;
using EventStore.Core.Messaging;
using EventStore.Core.Services.TimerService;
using EventStore.Core.Tests.Fakes;
using EventStore.Core.Tests.Services.TimeService;
using EventStore.Projections.Core.Services.Management;
using NUnit.Framework;

namespace EventStore.Projections.Core.Tests.Services.projections_manager
{
    [TestFixture]
    public class when_creating_projection_manager
    {
        private ITimeProvider _timeProvider;
        private Dictionary<Guid, IPublisher> _queues;
        private TimeoutScheduler[] _timeoutSchedulers;

        [SetUp]
        public void setup()
        {
            _timeProvider = new FakeTimeProvider();
            _queues = new Dictionary<Guid, IPublisher> {{Guid.NewGuid(), new FakePublisher()}};
            _timeoutSchedulers = ProjectionCoreWorkersNode.CreateTimeoutSchedulers(_queues.Count);
            new ProjectionCoreCoordinator(
                RunProjections.All, 
                _timeoutSchedulers,
                _queues.Values.ToArray(),
                new FakePublisher(),
                new NoopEnvelope());
        }

        [Test]
        public void it_can_be_created()
        {
            using (
                new ProjectionManager(
                    new FakePublisher(),
                    new FakePublisher(),
                    _queues,
                    _timeProvider,
                    RunProjections.All))
            {
            }

        }

        [Test, ExpectedException(typeof (ArgumentNullException))]
        public void null_main_queue_throws_argument_null_exception()
        {
            using (
                new ProjectionManager(
                    null,
                    new FakePublisher(),
                    _queues,
                    _timeProvider,
                    RunProjections.All))
            {
            }
        }

        [Test, ExpectedException(typeof (ArgumentNullException))]
        public void null_publisher_throws_argument_null_exception()
        {
            using (
                new ProjectionManager(
                    new FakePublisher(),
                    null,
                    _queues,
                    _timeProvider,
                    RunProjections.All))
            {
            }
        }

        [Test, ExpectedException(typeof (ArgumentNullException))]
        public void null_queues_throws_argument_null_exception()
        {
            using (
                new ProjectionManager(
                    new FakePublisher(),
                    new FakePublisher(),
                    null,
                    _timeProvider,
                    RunProjections.All))
            {
            }
        }

        [Test, ExpectedException(typeof (ArgumentException))]
        public void empty_queues_throws_argument_exception()
        {
            using (
                new ProjectionManager(
                    new FakePublisher(),
                    new FakePublisher(),
                    new Dictionary<Guid, IPublisher>(),
                    _timeProvider,
                    RunProjections.All))
            {
            }
        }

    }
}
