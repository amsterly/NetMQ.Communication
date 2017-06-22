using NetMQ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetMQ.Extension
{
    public class MessageFactory
    {
        private readonly Dictionary<string, uint> _idDictionary = new Dictionary<string, uint>();
        private readonly static MessageFactory _instance = new MessageFactory();

        private MessageFactory()
        {
        }

        public static MessageFactory Instance
        {
            get { return _instance; }
        }

        public NetMQMessage CreateMessage(string name, ushort flag, params NetMQFrame[] contentFrames)
        {
            NetMQMessage msg = new NetMQMessage();
            msg.Append(name);
            msg.Append(PopID(name).ToFrame_Ex());
            msg.Append(flag.ToFrame_Ex());
            msg.Append(EnumReplyFlag.Success.ToFrame_Ex());

            if (contentFrames != null)
            {
                foreach (var each in contentFrames)
                    msg.Append(each);
            }

            return msg;
        }

        public NetMQMessage CopyFrom(NetMQMessage requestMsg, EnumReplyFlag reply, params NetMQFrame[] contents)
        {
            return requestMsg.Duplicate_Ex(reply, contents);
        }

        private uint PopID(string name)
        {
            if (!_idDictionary.ContainsKey(name))
            {
                _idDictionary.Add(name, 0);
            }

            _idDictionary[name] += 1;
            return _idDictionary[name];
        }
    }
}