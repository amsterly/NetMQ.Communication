using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Communication.Client
{
    public enum EnumMessageFlag : byte
    {
        Hello = 0x01,
        Str = 0x02,
        CommunicationConstuction = 0x03
    }
}
