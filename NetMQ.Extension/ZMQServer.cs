using NetMQ;
using NetMQ.Sockets;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;

namespace NetMQ.Extension
{
    public class ZMQServer
    {
        public ZMQServer(string group, string name)
        {
            _beacon = new Beacon(group, name);
        }

        private const int SocketIDLETime = 5;
        private readonly Beacon _beacon;
        private PublisherSocket _publisher;
        private ResponseSocket _responserSocket;
        private NetMQPoller _poller;
        private readonly Queue<NetMQMessage> _pubMsgQueue = new Queue<NetMQMessage>();

        public bool IsRunning
        {
            get { return _beacon != null && _beacon.IsRunning && _poller != null && _poller.IsRunning; }
        }

        public void Start()
        {
            if (IsRunning) return;

            _publisher = new PublisherSocket();
            int pPort = _publisher.BindRandomPort("tcp://*");
            _responserSocket = new ResponseSocket();
            int rPort = _responserSocket.BindRandomPort("tcp://*");

            _beacon.SelfNode.Arguments = string.Format("-p {0} -r {1}", pPort, rPort);

            HockSocketEvents(true);

            _poller = new NetMQPoller { _publisher, _responserSocket };

            _poller.RunAsync();

            _beacon.Start();
        }

        public void Stop()
        {
            if (!IsRunning) return;

            HockSocketEvents(false);

            _beacon.Stop();
        }

        public void PubMessage(ushort msgFlag, params NetMQFrame[] contentFrames)
        {
            _pubMsgQueue.Enqueue(MessageFactory.Instance.CreateMessage(this._beacon.Name, msgFlag, contentFrames));
        }

        public Func<NetMQMessage, NetMQMessage> MesageReceivedCallback;

        private void HockSocketEvents(bool flag)
        {
            if (flag)
            {
                this._publisher.SendReady += _publisher_SendReady;
                this._responserSocket.ReceiveReady += _responserSocket_ReceiveReady;
            }
        }

        private void _responserSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            if (!e.IsReadyToReceive)
            {
                Thread.Sleep(SocketIDLETime);
                return;
            }

            var msg = e.Socket.ReceiveMultipartMessage();

            if (MesageReceivedCallback != null)
            {
                var reply = MesageReceivedCallback(msg);
                if (reply != null)
                {
                    e.Socket.SendMultipartMessage(reply);
                }
                else
                {
                    e.Socket.SendMultipartMessage(MessageFactory.Instance.CopyFrom(msg, EnumReplyFlag.Unhandled));
                }
            }
            else
            {
                e.Socket.SendMultipartMessage(MessageFactory.Instance.CopyFrom(msg, EnumReplyFlag.Unhandled));
            }
        }

        private void _publisher_SendReady(object sender, NetMQSocketEventArgs e)
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
                Thread.Sleep(SocketIDLETime);
            }
        }
    }
}