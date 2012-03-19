using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace YakShayQRS.Specs
{
    [TestClass]
    public class InterceptorSpecs
    {
        public class SomeTestClass
        {
            public bool HasMethodBeenCalled = false;

            public virtual string SomePartOfTheUID { get; set; }

            public virtual string AnotherPartOfTheUID { get; set; }

            public void MethodToInvoke(bool SomeFlag, string SomeString)
            {
                OnMethodWasInvoked(SomeString, 123);
            }

            public virtual void OnMethodWasInvoked(string SomeString, int AnotherInt)
            {
                HasMethodBeenCalled = true;
            }
        }

        public class MethodToInvoke
        {
            public string SomePartOfTheUID;
            public string AnotherPartOfTheUID;
            public bool SomeFlag = false;
            public string SomeString = null;
        }

        [TestMethod]
        public void Message_should_get_processed_and_generate_a_single_InterceptThis_message_with_the_correct_parameters()
        {
            var SUT = new YakShayBus();
            SUT.RegisterType<SomeTestClass>();
            var es = new EventQueue();
            var msg = new Message("MethodToInvoke", new
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeFlag = true,
                SomeString = "ABC"
            });

            SUT.Handle(msg, es.Add);

            es.msgs.Count.ShouldBe(1);
            es.msgs.First().ToFriendlyString().ShouldBe(new Message("OnMethodWasInvoked", new
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeString = "ABC",
                AnotherInt = 123
            }).ToFriendlyString());

            var ArInstance = new SomeTestClass { SomePartOfTheUID = "A", AnotherPartOfTheUID = "B" };
            var AnotherArInstance = new SomeTestClass { SomePartOfTheUID = "X", AnotherPartOfTheUID = "Y" };
            SUT.ApplyHistory(ArInstance, es.Filter);
            SUT.ApplyHistory(AnotherArInstance, es.Filter);

            ArInstance.HasMethodBeenCalled.ShouldBe(true);
            AnotherArInstance.HasMethodBeenCalled.ShouldBe(false);
        }

        [TestMethod]
        public void Behaviour_should_also_work_with_strongly_typed_message_classes()
        {
            var SUT = new YakShayBus();
            SUT.RegisterType<SomeTestClass>();
            var resultingMessages = new List<Message>();
            var msg = new MethodToInvoke
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeFlag = true,
                SomeString = "ABC"
            };

            SUT.Handle(Message.FromObject(msg), x => resultingMessages.Add(x));

            resultingMessages.Count.ShouldBe(1);
            resultingMessages.First().ToFriendlyString().ShouldBe(new Message("OnMethodWasInvoked", new
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeString = "ABC",
                AnotherInt = 123
            }).ToFriendlyString());
        }

        [TestMethod]
        public void Behaviour_should_also_work_with_method_calls()
        {
            var SUT = new YakShayBus();
            SUT.RegisterType<SomeTestClass>();
            var resultingMessages = new List<Message>();
            var msg = new
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeFlag = true,
                SomeString = "ABC"
            };

            SUT.Handle(
                Message.FromAction(x => x.MethodToInvoke(SomePartOfTheUID: "A", AnotherPartOfTheUID: "B", SomeFlag: true, SomeString: "ABC"))
                , x => resultingMessages.Add(x));

            resultingMessages.Count.ShouldBe(1);
            resultingMessages.First().ToFriendlyString().ShouldBe(new Message("OnMethodWasInvoked", new
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeString = "ABC",
                AnotherInt = 123
            }).ToFriendlyString());
        }

        [TestMethod]
        public void Invoking_a_message_that_does_not_have_the_unique_ids_should_not_generate_events()
        {
            var SUT = new YakShayBus();
            SUT.RegisterType<SomeTestClass>();
            var resultingMessages = new List<Message>();
            var msg = new Message("MethodToInvoke", new
            {
                NotPartOfTheKey = "A",
                AnotherPartOfTheUID = "B",
                SomeFlag = true,
                SomeString = "ABC"
            });

            SUT.Handle(msg, x => resultingMessages.Add(x));

            resultingMessages.ShouldBeEmpty();
        }
    }
}