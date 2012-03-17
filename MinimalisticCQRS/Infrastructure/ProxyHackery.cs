using System;
using System.Reflection;
using Castle.DynamicProxy;

namespace MinimalisticCQRS.Infrastructure
{
    public class ProxyHackery
    {
        static ProxyGenerator _generator = new ProxyGenerator();

        private static T GetProxyClass<T>(T target, Action<Message> EmitEvent) where T : class
        {
            var proxy = _generator.CreateClassProxyWithTarget<T>(target, new VirtualMethodCallLogger(EmitEvent));
            return proxy;
        }

        public static object GetProxy(object instance, Action<Message> EmitEvent)
        {
            var mi = typeof(ProxyHackery).GetMethod("GetProxyClass", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic);
            var gmi = mi.MakeGenericMethod(instance.GetType());

            var proxy = gmi.Invoke(null, new object[] { instance, EmitEvent });
            return proxy;
        }
    }
}