using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YakShayQRS
{
    public class YakShayBus
    {
        Dictionary<Type, RegisteredType> RegisteredTypes = new Dictionary<Type, RegisteredType>();

        public void RegisterType<T>()
        {
            if (!RegisteredTypes.ContainsKey(typeof(T)))
                RegisteredTypes.Add(typeof(T), new RegisteredType(typeof(T)));
        }

        public void ApplyHistory(object instance, Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<Message>> GetHistory, Func<string, Type, object> Resolver = null)
        {
            if (!RegisteredTypes.ContainsKey(instance.GetType()))
                return;
            var t = RegisteredTypes[instance.GetType()];
            t.ApplyHistory(instance, GetHistory, Resolver);
            return;
        }

        public void HandleUntilAllConsumed(Message msg, Action<Message> EmitMessage, Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<Message>> GetHistory = null, Func<string, Type, object> Resolver = null)
        {
            var queue = new List<Message>();
            queue.Add(msg);
            var toEmit = new List<Message>();
            while (queue.Any())
            {
                var newmsgs = new List<Message>();
                Handle(queue.First(), x => newmsgs.Add(x), GetHistory, Resolver);
                foreach (var m in newmsgs)
                {
                    toEmit.Add(m);
                    queue.Add(m);
                }
                queue.RemoveAt(0);
            }
            foreach (var em in toEmit)
                EmitMessage(em);
        }

        public void Handle(Message msg, Action<Message> EmitMessage, Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<Message>> GetHistory = null, Func<string, Type, object> Resolver = null)
        {
            if (Resolver == null)
            {
                Resolver = (name, t) => Activator.CreateInstance(t);
            }
            foreach (var t in RegisteredTypes.Values)
            {
                var logger = t.GetInstanceIfMessageAppliesToType(msg, true);
                if (logger == null) continue;
                var pars = msg.Parameters.Where(x => t.UniqueIdPropertySetters.ContainsKey(x.Key));
                if (GetHistory != null)
                {
                    foreach (var hm in GetHistory(pars))
                    {
                        t.InvokeOnInstance(logger.Instance, hm, Resolver, false, true);
                    }
                }
                logger.EmitMessage = x =>
                {
                    x.Parameters = x.Parameters.Union(pars).ToArray();
                    EmitMessage(x);
                };
                t.InvokeOnInstance(logger.Instance, msg, Resolver, false);
            }
        }

        internal class RegisteredType
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            public Dictionary<string, Action<object, object>> UniqueIdPropertySetters { get; private set; }

            Dictionary<string, MethodInfo> MethodInfos;

            private Type T;

            public RegisteredType(Type T)
            {
                this.T = T;
                this.MethodInfos = T.GetMethods(bf).ToDictionary(x => x.Name);
                UniqueIdPropertySetters = new Dictionary<string, Action<object, object>>();
                foreach (var pi in T.GetProperties(bf).Where(x => x.GetAccessors(true)[0].IsVirtual))
                {
                    var sm = pi.GetSetMethod(true);
                    UniqueIdPropertySetters.Add(pi.Name, (i, p) => sm.Invoke(i, new object[] { p }));
                }
            }

            public VirtualMethodCallLogger GetInstanceIfMessageAppliesToType(Message msg, bool NonVirtualMethodsOnly = false)
            {
                if (!MethodInfos.ContainsKey(msg.MethodName))
                    return null;
                if (NonVirtualMethodsOnly && MethodInfos[msg.MethodName].IsVirtual)
                    return null;
                var requiredFields = UniqueIdPropertySetters.Keys;
                if (!requiredFields.All(x => msg.Parameters.Any(y => y.Key == x)))
                    return null;

                var res = new VirtualMethodCallLogger() { InvokingMessage = msg };
                res.Instance = pg.CreateClassProxy(T, res);
                foreach (var fn in UniqueIdPropertySetters)
                {
                    fn.Value.Invoke(res.Instance, msg.Parameters.Where(x => x.Key == fn.Key).First().Value);
                }
                return res;
            }

            public void InvokeOnInstance(object instance, Message m, Func<string, Type, object> Resolver, bool ThrowWhenNotFound = false, bool virtualonly = false)
            {
                Func<string, Type, object> wrappedresolver = (name, t) =>
                {
                    var k = m.Parameters.Where(x => x.Key == name);
                    if (k.Any())
                        return k.First().Value;
                    else
                        return Resolver(name, t);
                };
                var mi = MethodInfos[m.MethodName];
                if (virtualonly && !mi.IsVirtual)
                    return;
                var pars = mi.GetParameters().Select(x => wrappedresolver(x.Name, x.ParameterType)).ToArray();
                mi.Invoke(instance, pars);
            }

            static Castle.DynamicProxy.ProxyGenerator pg = new Castle.DynamicProxy.ProxyGenerator();

            internal class VirtualMethodCallLogger : Castle.DynamicProxy.IInterceptor
            {
                public Action<Message> EmitMessage = x => { };
                public object Instance;
                public Message InvokingMessage;

                public VirtualMethodCallLogger()
                {
                }

                public void Intercept(Castle.DynamicProxy.IInvocation invocation)
                {
                    var msg = new Message();
                    var mi = invocation.GetConcreteMethodInvocationTarget();
                    if (!mi.Name.StartsWith("set_") && !mi.Name.StartsWith("get_"))
                    {
                        msg.MethodName = mi.Name;
                        msg.Parameters = mi.GetParameters().Select(x => x.Name).Zip(invocation.Arguments, (n, v) => new KeyValuePair<string, object>(n, v));
                        if (InvokingMessage.MethodName != mi.Name)
                            EmitMessage(msg);
                    }
                    invocation.Proceed();
                }
            }

            internal void ApplyHistory(object instance, Func<IEnumerable<KeyValuePair<string, object>>, IEnumerable<Message>> GetHistory, Func<string, Type, object> Resolver)
            {
                const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                var uniquepars = UniqueIdPropertySetters.Keys
                    .Select(x => new KeyValuePair<string, object>(x, instance.GetType().GetProperty(x, bf).GetValue(instance, new object[] { })));

                foreach (var hm in GetHistory(uniquepars))
                {
                    Func<string, Type, object> wrappedresolver = (name, t) =>
                    {
                        var k = hm.Parameters.Where(x => x.Key == name);
                        if (k.Any())
                            return k.First().Value;
                        else
                            return Resolver(name, t);
                    };
                    var mi = MethodInfos[hm.MethodName];
                    if (!mi.IsVirtual) continue;
                    var pars = mi.GetParameters().Select(x => wrappedresolver(x.Name, x.ParameterType)).ToArray();
                    mi.Invoke(instance, pars);
                }
            }
        }
    }
}