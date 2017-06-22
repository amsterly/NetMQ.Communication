using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetMQ.Extension
{
    public enum EnumReplyFlag : byte
    {
        Fail = 0x00,
        Success = 0x01,
        Refuse = 0x02,
        Unhandled = 0x03,
    }
}