
using NetMQ.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetMQ_Communication.Server
{
    internal enum EnumMessageFlag : ushort
    {
        Hello = MessageFlagConst.Hello,

        Get_CCRSummary = 0x0010,
        Get_CCRFile = 0x0011,
    }
}