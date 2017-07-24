using NetMQ;
using NetMQ.Extension;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


/**
 * 服务端和客户端无论谁先启动，效果是相同的，这点不同于Socket。
在服务端收到信息以前，程序是阻塞的，会一直等待客户端连接上来。
服务端收到信息以后，会send一个“World”给客户端。值得注意的是一定是client连接上来以后，send消息给Server，然后Server再rev然后响应client，这种一问一答式的。
如果Server先send，client先rev是会报错的。
ZMQ通信通信单元是消息，他除了知道Bytes的大小，他并不关心的消息格式。因此，你可以使用任何你觉得好用的数据格式。Xml、Protocol Buffers、Thrift、json等等。
虽然可以使用ZMQ实现HTTP协议，但是，这绝不是他所擅长的。
 * 
 * 数据流：client:request ->server：response [(sub) ip port] 这两个ip port相同  bind [(pub)ip port] ->client:sub [ip port]
 * 之后就可以client发送str server pub回client啦
 * 【我试过直接用server 的response 发送消息至 request 直接报错，正好符合上面所说的问答流程错误】
 **/
namespace NetMQ_Communication.Server
{
    class AlyServer_Pub_BeaconVersion
    {
        private readonly Beacon _beacon;
        public string Name { get; private set; }
        #region NetMQ

        private PublisherSocket _publisher;
        private NetMQPoller _poller;
        private readonly Queue<NetMQMessage> _pubMsgQueue = new Queue<NetMQMessage>();

        #endregion NetMQ
        public AlyServer_Pub_BeaconVersion(String name)
        {
            this.Name = name;
            this._beacon = new Beacon("AlyServer", this.Name);
        }
        public bool IsRunning
        {
            get
            {
                return this._beacon != null && this._poller != null && this._beacon.IsRunning && this._poller.IsRunning;
            }
        }
        public void Start()
        {
            _publisher = new PublisherSocket();
            int pPort = _publisher.BindRandomPort("tcp://*");
            _publisher.SendReady += OnPubReady;

            int rPort = 0;
            _poller = new NetMQPoller { _publisher };
            _poller.RunAsync();

            this._beacon.SelfNode.Arguments = string.Format("-p {0} -r {1}", pPort, rPort);
            this._beacon.Start();
        }
        public void Stop()
        {
            if (this._beacon != null)
            {
                this._beacon.Stop();
            }

            if (_poller != null)
            {
                _poller.Stop();
            }
        }
        public void PubCCR(string ccr)
        {
            if (string.IsNullOrEmpty(ccr)) return;

            _pubMsgQueue.Enqueue(CreateMessage(EnumMessageFlag.Get_CCRSummary, ccr.ToFrame_Ex()));
        }

        private void OnPubReady(object sender, NetMQSocketEventArgs e)
        {
            if (e.IsReadyToSend && _pubMsgQueue.Count > 0)
            {
                var msg = _pubMsgQueue.Dequeue();
                if (msg != null)
                {
                    e.Socket.SendMultipartMessage(msg);
                }
            }
            else
            {
                Thread.Sleep(5);
            }
        }

        private NetMQMessage CreateMessage(EnumMessageFlag flag, params NetMQFrame[] contentFrames)
        {
            return MessageFactory.Instance.CreateMessage("", (ushort)flag, contentFrames);
        }
    }
}
