using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MinimalisticCQRS.Infrastructure
{
    public class MiniVanRegistry
    {
        public class ResolvedInstance
        {
            public object instance;
            public Dictionary<string, object> CtorPars;
            public Predicate<Message> CanHandle;

            public void InvokeOnInstance(Message m, Func<string, Type, dynamic> Resolver, Action<Message> LogMessage, bool ThrowWhenNotFound = false)
            {
                if (!CanHandle(m))
                {
                    if (ThrowWhenNotFound)
                        throw new InvalidOperationException("Unable to exec message");
                    else
                        return;
                }
                var wrappedresolver = new MessageResolverMapper(m, Resolver);
                var mi = methodInfos[m.MethodName];
                var pars = mi.GetParameters().Select(x => wrappedresolver.Resolve(x.Name, x.ParameterType)).ToArray();
                var proxy = ProxyHackery.GetProxy(instance, x =>
                {
                    x.Parameters = x.Parameters.Union(CtorPars.Select(y => new KeyValuePair<string, object>(y.Key, y.Value)));
                    if (!mi.IsVirtual)
                        LogMessage(x);
                });
                mi.Invoke(proxy, pars);
            }

            public Dictionary<string, MethodInfo> methodInfos { get; set; }
        }

        protected class RegisteredType
        {
            Type T;
            ConstructorInfo longestCtor;
            Dictionary<string, MethodInfo> methodinfos = new Dictionary<string, MethodInfo>();

            public ResolvedInstance CreateInstance(Message msg, Func<string, Type, dynamic> Resolver)
            {
                var wrappedresolver = new MessageResolverMapper(msg, Resolver);
                var pars = longestCtor.GetParameters().Select(x => wrappedresolver.Resolve(x.Name, x.ParameterType)).ToArray();
                var i = longestCtor.Invoke(pars);
                return new ResolvedInstance
                {
                    instance = i,
                    methodInfos = methodinfos,
                    CanHandle = m =>
                    {
                        return
                            i.GetType() == T &&
                            methodinfos.ContainsKey(m.MethodName) &&
                            wrappedresolver.ParametersResolvedFromMessage.All(x => m.Parameters.Any(y => y.Key == x.Key && y.Value == x.Value));
                    },
                    CtorPars = wrappedresolver.ParametersResolvedFromMessage
                };
            }

            public RegisteredType(Type t)
            {
                this.T = t;
                longestCtor = t.GetConstructors().OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();
                methodinfos = t.GetMethods().Where(x => x.ReturnType == typeof(void)).ToDictionary(x => x.Name);
            }
        }

        Dictionary<Type, RegisteredType> RegisteredTypes = new Dictionary<Type, RegisteredType>();

        public void Register<T>()
        {
            Register(typeof(T));
        }

        public void Register(Type T)
        {
            if (RegisteredTypes.ContainsKey(T))
                return;
            RegisteredTypes.Add(T, new RegisteredType(T));
        }

        public IEnumerable<ResolvedInstance> ResolveInstancesForMessage(Message msg, Func<string, Type, dynamic> Resolve)
        {
            foreach (var t in RegisteredTypes)
            {
                var instr = t.Value.CreateInstance(msg, Resolve);
                if (instr == null)
                    continue;
                yield return instr;
            }
        }
    }
}