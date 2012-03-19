using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace YakShayQRS
{
    public class Message : DynamicObject
    {
        public IEnumerable<KeyValuePair<string, object>> Parameters;
        public String MethodName;

        public Message() { }

        public Message(string MethodName, IEnumerable<KeyValuePair<string, object>> Parameters)
        {
            this.MethodName = MethodName;
            this.Parameters = Parameters;
        }

        public Message(InvokeMemberBinder binder, object[] args)
        {
            MethodName = binder.Name;
            var names = new List<string>();
            while (binder.CallInfo.ArgumentNames.Count + names.Count < binder.CallInfo.ArgumentCount)
                names.Add(names.Count.ToString());
            names.AddRange(binder.CallInfo.ArgumentNames);
            Parameters = names.Select((x, i) => new KeyValuePair<string, object>(x, args[i]));
        }

        public static Message FromObject(object msg)
        {
            var m = new Message();
            m.MethodName = msg.GetType().Name;
            m.Parameters = msg.GetType().GetProperties().Select(x => new KeyValuePair<string, object>(x.Name, x.GetValue(msg, null)))
                .Union(msg.GetType().GetFields().Select(x => new KeyValuePair<string, object>(x.Name, x.GetValue(msg))));
            return m;
        }

        public Message(string MethodName, object pars)
        {
            this.MethodName = MethodName;
            this.Parameters = pars.GetType().GetProperties().Select(x => new KeyValuePair<string, object>(x.Name, x.GetValue(pars, null)))
                .Union(pars.GetType().GetFields().Select(x => new KeyValuePair<string, object>(x.Name, x.GetValue(pars))));
        }

        public static Message FromAction(Action<dynamic> a)
        {
            var msg = new Message();
            a(msg);
            return msg;
        }

        public override int GetHashCode()
        {
            return MethodName.GetHashCode() ^ Parameters.Aggregate(0, (feed, x) => feed ^ x.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return (obj as Message) == this;
        }

        public static bool operator ==(Message m1, Message m2)
        {
            if (ReferenceEquals(m1, m2)) return true;
            if (ReferenceEquals(null, m1) || ReferenceEquals(null, m2)) return false;
            if (m1.GetHashCode() != m2.GetHashCode()) return false;
            if (m1.MethodName != m2.MethodName) return false;
            if (m1.Parameters.Count() != m2.Parameters.Count()) return false;
            if (m1.Parameters.All(x => m2.Parameters.Contains(x))) return true;
            return false;
        }

        public static bool operator !=(Message m1, Message m2)
        {
            return !(m1 == m2);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var msg = new Message(binder, args);
            this.MethodName = msg.MethodName;
            this.Parameters = msg.Parameters;
            result = msg;
            return true;
        }

        public string ToFriendlyString()
        {
            var sb = new StringBuilder();
            sb.Append(MethodName);
            sb.Append("(");
            var pars = Parameters.OrderBy(x => x.Key);
            foreach (var kv in pars)
            {
                sb.Append(kv.Key);
                sb.Append("=");
                sb.Append((kv.Value ?? "").ToString());
                if (kv.Key != pars.Last().Key)
                    sb.Append(",");
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}