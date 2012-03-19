using System.Collections.Generic;
using System.Linq;

namespace YakShayQRS
{
    public class MessageStore
    {
        public List<Message> msgs = new List<Message>();

        public void Add(Message msg)
        {
            msgs.Add(msg);
        }

        public IEnumerable<Message> Filter(IEnumerable<KeyValuePair<string, object>> props)
        {
            return msgs.Where(x => props.All(y => x.Parameters.Any(z => z.Key == y.Key && z.Value == y.Value))).ToArray();
        }
    }
}