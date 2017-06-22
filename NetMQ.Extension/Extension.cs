using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace NetMQ.Extension
{
    public static class Extension
    {
        private const int MinMsgFrameCount = 4;

        public static NetMQFrame ToFrame_Ex(this string value)
        {
            return new NetMQFrame(ASCIIEncoding.UTF8.GetBytes(value));
        }

        public static NetMQFrame ToFrame_Ex(this uint value)
        {
            return new NetMQFrame(BitConverter.GetBytes(value));
        }

        public static NetMQFrame ToFrame_Ex(this ushort value)
        {
            return new NetMQFrame(BitConverter.GetBytes(value));
        }

        public static NetMQFrame ToFrame_Ex(this byte value)
        {
            return new NetMQFrame(BitConverter.GetBytes(value));
        }

        public static NetMQFrame ToFrame_Ex(this EnumReplyFlag value)
        {
            return new NetMQFrame(BitConverter.GetBytes((byte)value));
        }

        public static NetMQMessage Duplicate_Ex(this NetMQMessage sourceMsg, EnumReplyFlag reply, params NetMQFrame[] contents)
        {
            Debug.Assert(sourceMsg.FrameCount >= MinMsgFrameCount, string.Format("Frame count must large than {0}.", MinMsgFrameCount));

            NetMQMessage msg = new NetMQMessage();
            msg.Append(sourceMsg[0].Duplicate());
            msg.Append(sourceMsg[1].Duplicate());
            msg.Append(sourceMsg[2].Duplicate());
            msg.Append(reply.ToFrame_Ex());
            if (contents != null)
            {
                foreach (var each in contents)
                    msg.Append(each);
            }
            return msg;
        }

        public static NetMQFrame GetContentFrame_Ex(this NetMQMessage msg, int contentIndex)
        {
            if (msg == null || contentIndex + MinMsgFrameCount >= msg.FrameCount)
            {
                return null;
            }

            return msg[MinMsgFrameCount + contentIndex];
        }

        public static ushort ReadUInt16(this NetMQFrame frame)
        {
            return BitConverter.ToUInt16(frame.Buffer, 0);
        }

        public static long ReadInt64(this NetMQFrame frame)
        {
            return BitConverter.ToInt64(frame.Buffer, 0);
        }

        public static int ReadInt32(this NetMQFrame frame)
        {
            return BitConverter.ToInt32(frame.Buffer, 0);
        }

        public static long ReadAsByte(this NetMQFrame frame)
        {
            return frame.Buffer[0];
        }

        public static string ReadString(this NetMQFrame frame)
        {
            return ASCIIEncoding.UTF8.GetString(frame.Buffer, 0, frame.BufferSize);
        }

        public static byte[] Read(this NetMQFrame frame)
        {
            return frame.Buffer;
        }
    }
}