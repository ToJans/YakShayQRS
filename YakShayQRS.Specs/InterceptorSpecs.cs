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
            public bool HasMethodToInterceptBeenCalled = false;

            public virtual string SomePartOfTheUID { get; set; }

            public virtual string AnotherPartOfTheUID { get; set; }

            public void MethodToInvokeUsingAMessage(bool SomeFlag, string SomeString)
            {
                MethodWasInvoked(SomeString, 123);
            }

            public virtual void MethodWasInvoked(string SomeString, int AnotherInt)
            {
                HasMethodToInterceptBeenCalled = true;
            }
        }

        public class MethodToInvokeUsingAMessage
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
            var resultingMessages = new List<Message>();
            var msg = new Message("MethodToInvokeUsingAMessage", new
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeFlag = true,
                SomeString = "ABC"
            });

            SUT.Handle(msg, x => resultingMessages.Add(x));

            resultingMessages.Count.ShouldBe(1);
            resultingMessages.First().ToFriendlyString().ShouldBe(new Message("MethodWasInvoked", new
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeString = "ABC",
                AnotherInt = 123
            }).ToFriendlyString());
        }

        [TestMethod]
        public void Behaviour_should_also_work_with_strongly_typed_message_classes()
        {
            var SUT = new YakShayBus();
            SUT.RegisterType<SomeTestClass>();
            var resultingMessages = new List<Message>();
            var msg = new MethodToInvokeUsingAMessage
            {
                SomePartOfTheUID = "A",
                AnotherPartOfTheUID = "B",
                SomeFlag = true,
                SomeString = "ABC"
            };

            SUT.Handle(Message.FromObject(msg), x => resultingMessages.Add(x));

            resultingMessages.Count.ShouldBe(1);
            resultingMessages.First().ToFriendlyString().ShouldBe(new Message("MethodWasInvoked", new
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
                Message.FromAction(x => x.MethodToInvokeUsingAMessage(SomePartOfTheUID: "A", AnotherPartOfTheUID: "B", SomeFlag: true, SomeString: "ABC"))
                , x => resultingMessages.Add(x));

            resultingMessages.Count.ShouldBe(1);
            resultingMessages.First().ToFriendlyString().ShouldBe(new Message("MethodWasInvoked", new
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
            var msg = new Message("MethodToInvokeUsingAMessage", new
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