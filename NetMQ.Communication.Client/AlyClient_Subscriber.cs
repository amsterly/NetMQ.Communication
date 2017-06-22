using Microsoft.SqlServer.Server;
using NetMQ.Extension;
using NetMQ.Sockets;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NetMQ.Communication.Client
{
    internal class AlyClient_Subscriber : IDisposable
    {
        private Beacon _beacon;

        public string Name { get; private set; }

        public bool IsRunning
        {
            get { return _beacon != null && _beacon.IsRunning; }
        }

        public bool IsConnected
        {
            get { return _poller != null && _poller.IsRunning; }
        }

        public AlyClient_Subscriber(string name)
        {
            this.Name = name;
            int random = new Random().Next(100);
            this.Name += random;
            _beacon = new Beacon("AlyClient", this.Name);
            Console.WriteLine("Init AlyClient " + this.Name);
        }

        public void Start()
        {
            HockBeaconEvents(true);
            
            _beacon.Start();
        }

        public void Stop()
        {
            HockBeaconEvents(false);

            if (_beacon != null)
            {
                _beacon.Stop();
            }
        }

        public event Action<object, string> CCRReady;

        private void OnCCRReady(string ccr)
        {
            if (this.CCRReady != null) this.CCRReady(this, ccr);
        }

        private void HockBeaconEvents(bool flag)
        {
            if (_beacon == null) return;

            if (flag)
            {
                _beacon.NodeConnected += beacon_NodeConnected;
                _beacon.NodeDisConnected += _beacon_NodeDisConnected;
            }
            else
            {
                _beacon.NodeConnected -= beacon_NodeConnected;
                _beacon.NodeDisConnected -= _beacon_NodeDisConnected;
            }
        }

        private void beacon_NodeConnected(Beacon arg1, BeaconNode arg2)
        {
            Console.WriteLine("Client:beacon_NodeConnected");
            if (arg2.Name == "AlyServer"&&!this.IsConnected)
            {
                string[] args = arg2.Arguments.Split(' ');
                ConnectServer(string.Format("tcp://{0}:{1}", arg2.Address, args[1]));
                Console.WriteLine("Client"+this.Name+": ConnectServer");
            }
          
        }

        private void _beacon_NodeDisConnected(Beacon arg1, BeaconNode arg2)
        {
            DisconnectServer();
        }

        #region NetMQ

        private NetMQPoller _poller;
        private SubscriberSocket _subscriber;

        private void ConnectServer(string subAddress)
        {
            _subscriber = new SubscriberSocket();
            _subscriber.SubscribeToAnyTopic();
            _subscriber.Connect(subAddress);
            _subscriber.ReceiveReady += _subscriber_ReceiveReady;

            _poller = new NetMQPoller { _subscriber };

            _poller.RunAsync();
            Thread.Sleep(1000);
            //如果改变参数 2台client 会收不到信息
            //this._beacon.SelfNode.Arguments = string.Format("-c {0}", this.IsConnected);
        }

        private void DisconnectServer()
        {
            if (this.IsConnected)
            {
                _poller.Stop();
            }

            if (_subscriber != null) _subscriber.ReceiveReady -= _subscriber_ReceiveReady;

            this._beacon.SelfNode.Arguments = string.Format("-c {0}", this.IsConnected);
        }

        private void _subscriber_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            string crc = msg.GetContentFrame_Ex(0).ReadString();
            Console.WriteLine("Client:Str="+crc);
            this.OnCCRReady(crc);
        }

        #endregion NetMQ

        public void Dispose()
        {
            if (_beacon != null)
            {
                _beacon.Dispose();
                _beacon = null;
            }

            if (_poller != null)
            {
                _poller.Dispose();
                _poller = null;
            }
            if (_subscriber != null)
            {
                _subscriber.Dispose();
                _subscriber = null;
            }
        }
    }
}