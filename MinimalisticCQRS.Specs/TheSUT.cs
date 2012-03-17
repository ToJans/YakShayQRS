using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MinimalisticCQRS.Infrastructure;

namespace MinimalisticCQRS.Specs
{
    public class TheSUT
    {
        public readonly MiniVanRegistry Registry = new MiniVanRegistry();
        MiniVan mv;
        Exception Except = null;

        List<Message> GivenEvents = new List<Message>();
        List<Message> ResultingEvents = new List<Message>();
        public Func<string, Type, dynamic> Resolver = (n, t) => Activator.CreateInstance(t);

        public TheSUT()
        {
            mv = new MiniVan(Registry);
        }

        public void GivenEvent(Action<dynamic> @event)
        {
            GivenEvent(new Message(@event));
        }

        public void GivenEvent(object @event)
        {
            GivenEvents.Add((@event as Message) ?? new Message(@event));
        }

        public void WhenCommand(Action<dynamic> command)
        {
            WhenCommand(new Message(command));
        }

        public void WhenCommand(object command)
        {
            var msg = (command as Message) ?? new Message(command);
            try
            {
                var msgsToProcess = new List<Message>();
                msgsToProcess.Add(msg);
                while (msgsToProcess.Any())
                {
                    var evts = mv.Delegate(msg, Resolver, GivenEvents);

                    ResultingEvents.AddRange(evts);
                    msgsToProcess.AddRange(evts);
                    msgsToProcess.RemoveAt(0);
                    msg = msgsToProcess.FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = (e as TargetInvocationException).InnerException;
                Except = e;
            }
        }

        public void ThenExpectEvent(Action<dynamic> @event)
        {
            ThenExpectEvent(new Message(@event));
        }

        public void ThenExpectEvent(object @event)
        {
            var msg = (@event as Message) ?? new Message(@event);
            if (ResultingEvents.All(x => x != msg))
            {
                var txt = MessageToText(msg);
                var amsg = "Expected \n * " + txt + "\n but could not find it.";
                var matches = ResultingEvents.Select(x => MessageToText(x))
                    .Select(x => new { distance = Levenshtein.Distance(txt, x), Event = x })
                    .OrderBy(x => x.distance)
                    .Take(5).Select(x => x.Event).ToArray();
                amsg += "\nTop 5 of best possible matching events are:\n" + string.Join("\n * ", matches);
                AssertFail(amsg);
            }
        }

        public void ThenDoNotExpectEvent(Action<dynamic> @event)
        {
            ThenDoNotExpectEvent(new Message(@event));
        }

        public void ThenDoNotExpectEvent(object @event)
        {
            var msg = (@event as Message) ?? new Message(@event);
            if (ResultingEvents.Any(x => x == msg))
            {
                AssertFail("Expected " + MessageToText(msg) + " but could not find it");
            }
        }

        public void ThenExpectException<T>(Predicate<T> assertion = null) where T : Exception
        {
            if ((this.Except as T) == null)
                AssertFail("Expected an exception of type " + typeof(T) + " but found none");
            if (assertion != null && !assertion(Except as T))
            {
                AssertFail("Received an exception of type " + typeof(T) + " but the assertion did not match");
            }
        }

        private void AssertFail(string message)
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.Assert.Fail(message);
        }

        public static string MessageToText(Message msg)
        {
            var sb = new StringBuilder();
            sb.Append(msg.MethodName);
            sb.Append("(");
            foreach (var kv in msg.Parameters)
            {
                sb.Append(kv.Key);
                sb.Append("=");
                sb.Append((kv.Value ?? "").ToString());
                if (kv.Key != msg.Parameters.Last().Key)
                    sb.Append(",");
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}