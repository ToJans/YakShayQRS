using System;
using System.Collections.Generic;
using System.Linq;

namespace MinimalisticCQRS.Infrastructure
{
    public class MessageResolverMapper
    {
        Func<string, Type, object> resolver;
        Message msg;

        public Dictionary<string, dynamic> ParametersResolvedFromMessage = new Dictionary<string, dynamic>();

        public MessageResolverMapper(Message msg, Func<string, Type, dynamic> resolver)
        {
            this.msg = msg;
            this.resolver = resolver;
        }

        public object Resolve(string name, Type t)
        {
            var parv = msg.Parameters.Where(x => x.Key == name);
            if (parv.Any())
            {
                ParametersResolvedFromMessage.Add(parv.First().Key, parv.First().Value);
                return parv.First().Value;
            }
            else
                return resolver(name, t);
        }
    }
}