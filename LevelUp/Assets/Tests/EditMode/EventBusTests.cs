using NUnit.Framework;
using LevelUp.Utils;

namespace LevelUp.Tests
{
    /// <summary>
    /// Vérifie l'isolation des handlers (un throw n'empêche pas les autres) et
    /// l'idempotence de Subscribe (double abonnement = une seule invocation).
    /// </summary>
    [TestFixture]
    public class EventBusTests
    {
        private struct PingEvent
        {
            public int Value;
        }

        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        [Test]
        public void Publish_noSubscribers_doesNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new PingEvent { Value = 1 }));
        }

        [Test]
        public void Subscribe_twiceSameHandler_invokedOnce()
        {
            int callCount = 0;
            void Handler(PingEvent e) => callCount++;

            EventBus.Subscribe<PingEvent>(Handler);
            EventBus.Subscribe<PingEvent>(Handler);
            EventBus.Publish(new PingEvent { Value = 42 });

            Assert.AreEqual(1, callCount);
            EventBus.Unsubscribe<PingEvent>(Handler);
        }

        [Test]
        public void Publish_multipleSubscribers_allInvoked()
        {
            int a = 0, b = 0;
            void HandlerA(PingEvent e) => a++;
            void HandlerB(PingEvent e) => b++;

            EventBus.Subscribe<PingEvent>(HandlerA);
            EventBus.Subscribe<PingEvent>(HandlerB);
            EventBus.Publish(new PingEvent());

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }

        [Test]
        public void Unsubscribe_stopsInvocation()
        {
            int count = 0;
            void Handler(PingEvent e) => count++;

            EventBus.Subscribe<PingEvent>(Handler);
            EventBus.Unsubscribe<PingEvent>(Handler);
            EventBus.Publish(new PingEvent());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Publish_throwingHandler_otherHandlersStillRun()
        {
            int aCount = 0, bCount = 0;
            void Throwing(PingEvent e) { throw new System.Exception("boom"); }
            void HandlerA(PingEvent e) => aCount++;
            void HandlerB(PingEvent e) => bCount++;

            EventBus.Subscribe<PingEvent>(HandlerA);
            EventBus.Subscribe<PingEvent>(Throwing);
            EventBus.Subscribe<PingEvent>(HandlerB);

            // L'exception est loggée mais ne propage pas, et HandlerB reste invoqué.
            UnityEngine.TestTools.LogAssert.Expect(
                UnityEngine.LogType.Error,
                new System.Text.RegularExpressions.Regex(".*PingEvent.*boom.*"));

            EventBus.Publish(new PingEvent());

            Assert.AreEqual(1, aCount);
            Assert.AreEqual(1, bCount);
        }

        [Test]
        public void Clear_removesAllSubscribers()
        {
            int count = 0;
            void Handler(PingEvent e) => count++;

            EventBus.Subscribe<PingEvent>(Handler);
            EventBus.Clear();
            EventBus.Publish(new PingEvent());

            Assert.AreEqual(0, count);
        }
    }
}
