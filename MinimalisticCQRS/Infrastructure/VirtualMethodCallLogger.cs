using System;
using System.Collections.Generic;
using System.Linq;

namespace MinimalisticCQRS.Infrastructure
{
    public class VirtualMethodCallLogger : Castle.DynamicProxy.IInterceptor
    {
        Action<Message> EmitMessage;

        public VirtualMethodCallLogger(Action<Message> EmitMessage)
        {
            this.EmitMessage = EmitMessage;
        }

        public void Intercept(Castle.DynamicProxy.IInvocation invocation)
        {
            var msg = new Message();
            var mi = invocation.GetConcreteMethodInvocationTarget();
            msg.MethodName = mi.Name;
            msg.Parameters = mi.GetParameters().Select(x => x.Name).Zip(invocation.Arguments, (n, v) => new KeyValuePair<string, object>(n, v));
            EmitMessage(msg);
            invocation.Proceed();
        }
    }
}