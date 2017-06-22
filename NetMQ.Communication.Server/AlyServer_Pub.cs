using NetMQ;
using NetMQ.Extension;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ_Communication.Server
{
    class AlyServer_Pub
    {
        private readonly Beacon _beacon;
        public string Name { get; private set; }
        #region NetMQ

        private PublisherSocket _publisher;
        private NetMQPoller _poller;
        private readonly Queue<NetMQMessage> _pubMsgQueue = new Queue<NetMQMessage>();

        #endregion NetMQ
        public AlyServer_Pub(String name)
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
