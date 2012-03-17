using System;
using System.Collections.Generic;
using System.Dynamic;

namespace MinimalisticCQRS.Infrastructure
{
    public class MiniVan : DynamicObject
    {
        MiniVanRegistry Registry;

        public MiniVan(MiniVanRegistry Registry)
        {
            this.Registry = Registry;
        }

        public virtual IEnumerable<Message> Delegate(Message msg, Func<string, Type, dynamic> Resolve, IEnumerable<Message> History)
        {
            // invoke "Can" before invoking the message
            foreach (var t in Registry.ResolveInstancesForMessage(msg, Resolve))
            {
                foreach (var hm in History)
                    t.InvokeOnInstance(hm, Resolve, x => { });
                t.InvokeOnInstance(new Message("Can" + msg.MethodName, msg.Parameters), Resolve, x => { });
            }
            // invoke the message
            var localmessages = new List<Message>();
            foreach (var t in Registry.ResolveInstancesForMessage(msg, Resolve))
            {
                foreach (var hm in History)
                    t.InvokeOnInstance(hm, Resolve, x => { });
                t.InvokeOnInstance(msg, Resolve, x => localmessages.Add(x));
            }
            return localmessages;
        }
    }
}