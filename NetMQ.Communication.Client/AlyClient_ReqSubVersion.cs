using NetMQ.Extension;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/**
 * 服务端和客户端无论谁先启动，效果是相同的，这点不同于Socket。
在服务端收到信息以前，程序是阻塞的，会一直等待客户端连接上来。
服务端收到信息以后，会send一个“World”给客户端。值得注意的是一定是client连接上来以后，send消息给Server，然后Server再rev然后响应client，这种一问一答式的。
如果Server先send，client先rev是会报错的。
ZMQ通信通信单元是消息，他除了知道Bytes的大小，他并不关心的消息格式。因此，你可以使用任何你觉得好用的数据格式。Xml、Protocol Buffers、Thrift、json等等。
虽然可以使用ZMQ实现HTTP协议，但是，这绝不是他所擅长的。
 * 
 * 连接建立 数据流：client:request ->server：response [(sub) ip port] 这两个ip port相同  bind [(pub)ip port] ->client:sub [ip port]
 * 之后就可以client发送str server pub回client啦
 * 【我试过直接用server 的response 发送消息至 request 直接报错，正好符合上面所说的问答流程错误】
 **/

namespace NetMQ.Communication.Client
{
  public   class AlyClient_ReqSubVersion
    {
        private RequestSocket _querySocket;
        private NetMQPoller _queryPoller;
        private Queue<NetMQMessage> _queryMsgList = new Queue<NetMQMessage>();
        private const string QUERY_CMD_PCLIENT = "AlyClient";
        private log4net.ILog logger = log4net.LogManager.GetLogger(typeof(AlyClient_ReqSubVersion));

        private SubscriberSocket _subscriber;
        private RequestSocket _requester;
        private NetMQPoller _poller;

        public Options Options { get; set; }
        public bool IsConnecting
        {
            get { return this._poller != null && _poller.IsRunning; }
        }

        private bool IsLocalClient { get; set; }

        public void start()
        {
            StartQueryMQ();
            QueryOptionFromServer();
        }
        public void stop()
        {
            StopQueryMQ();
        }

        private void StartQueryMQ()
        {
            this.Options = Options.LoadFromAppSettings();
            string serverAddress = System.Configuration.ConfigurationManager.AppSettings["AlyServerAddress"];
            string queryPort = System.Configuration.ConfigurationManager.AppSettings["AlyServerQueryPort"];
            if (!string.IsNullOrEmpty(serverAddress) &&
                !string.IsNullOrEmpty(queryPort)
                )
            {
                string addressUrl = string.Format("tcp://{0}:{1}", serverAddress, queryPort);
                try
                {
                    if (null == _querySocket)
                    {
                        _querySocket = new RequestSocket();
                        _querySocket.ReceiveReady += _querySocket_ReceiveReady;
                        _querySocket.SendReady += _querySocket_SendReady;
                    }

                    if (null == _queryPoller)
                    {
                        _queryPoller = new NetMQPoller();
                        _queryPoller.Add(_querySocket);
                        _querySocket.Connect(addressUrl);
                        _queryMsgList.Clear();
                        _queryPoller.RunAsync();
                        Console.WriteLine("[Progress]:[1] Request Listening");
                    }
                }
                catch (Exception ex)
                {

                }

            }
        }
        public void QueryOptionFromServer()
        {
            try
            {
                if (null != _querySocket)
                {
                    NetMQMessage msg = new NetMQMessage();
                    msg.Append("AlyClient");
                    msg.Append(QUERY_CMD_PCLIENT);
                    msg.Append("ID60311");
                    _queryMsgList.Enqueue(msg);
                    Console.WriteLine("[Progress]:[2] Request Send");
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void StopQueryMQ()
        {

            try
            {
                if (null != _queryPoller)
                {
                    if (null != _querySocket)
                    {
                        _queryPoller.Remove(_querySocket);
                    }
                    if (_queryPoller.IsRunning)
                    {
                        _queryPoller.Stop();
                    }
                    _queryPoller.Dispose();
                    _queryPoller = null;
                }
                if (null != _querySocket)
                {
                    _querySocket.ReceiveReady -= _querySocket_ReceiveReady;
                    _querySocket.SendReady -= _querySocket_SendReady;
                    //_querySocket.Disconnect(_querySocket.Options.LastEndpoint);
                    _querySocket.Dispose();
                    _querySocket = null;
                }
                _queryMsgList.Clear();
            }
            catch (Exception ex)
            {

            }
        }


        public void sendMSGtoServer(String str)
        {
            NetMQMessage msg = new NetMQMessage();
            msg.Append(CreateFrame((byte)EnumMessageFlag.Str));
            msg.Append(str);
            _queryMsgList.Enqueue(msg); 
        }
        private NetMQFrame CreateFrame(long value)
        {
            return new NetMQFrame(BitConverter.GetBytes(value));
        }

        private void _querySocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                if (e.IsReadyToSend && _queryMsgList.Count > 0)
                {
                    var msg = _queryMsgList.Dequeue();
                    e.Socket.SendMultipartMessage(msg);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void _querySocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                if (e.IsReadyToReceive)
                {
                    NetMQMessage msg = e.Socket.ReceiveMultipartMessage();
                    if (null != msg && msg.FrameCount >= 3)
                    {
                        string queryType = msg[1].ReadString();
                        string stationId = msg[2].ReadString();
                        switch (queryType)
                        {
                            case QUERY_CMD_PCLIENT:
                                string param = msg[3].ReadString();
                                Console.WriteLine("[Progress]:[3] Request Received");
                                //do something
                                ReceiveOptionFromServer(stationId, param);
                                break;
                            default:
                                break;
                        }
                    }
                    else if (null != msg && msg.FrameCount == 2)
                    {
                        string str = msg[1].ReadString();
                        Console.WriteLine("req received:"+ str);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

         private void ReceiveOptionFromServer(string stationId, string param)
        {
            if (true ==  !string.IsNullOrEmpty(param))
            {
                string[] items = param.Split(' ');
                if (items.Length == 8)
                {
                    if (IsConnecting)
                    {
                        StopStationNetMQ();
                    }
                    this.Options.MsgPubPort = items[1];
                    this.Options.MsgResPort = items[3];
                    this.Options.ServerAddress = items[5];
                    this.Options.RootPath = items[7];
                    StartStationNetMQ();
                  
                }
                else
                {
                    logger.InfoFormat("{0} in pserver.config error", this.Options.StationID);
                }
            }
        }

        private void StartStationNetMQ()
        {
            try
            {
                _subscriber = new SubscriberSocket();
                _subscriber.ReceiveReady += _subscriber_ReceiveReady;
                //_fileSubscriber = new SubscriberSocket();
                _requester = new RequestSocket();
                _poller = new NetMQPoller();

                IsLocalClient = CheckIsLocalIP(this.Options.ServerAddress);

                _subscriber.Connect(string.Format("tcp://{0}:{1}", this.Options.ServerAddress, this.Options.MsgPubPort));
                _subscriber.SubscribeToAnyTopic();
                _requester.Connect(string.Format("tcp://{0}:{1}", this.Options.ServerAddress, this.Options.MsgResPort));
                //_fileSubscriber.Connect(string.Format("tcp://{0}:{1}", this.Options.ServerAddress, this.Options.FilePubPort));
                //_fileSubscriber.SubscribeToAnyTopic();

                logger.InfoFormat("peripheral client netmq info: IP-{0},msg pub port:{1},msg res port:{2}", this.Options.ServerAddress, this.Options.MsgPubPort, this.Options.MsgResPort);

               // HockSocketEvents(true);

                //_poller.Add(_fileSubscriber);
                _poller.Add(_subscriber);
                _poller.Add(_requester);

                _poller.RunAsync();
                Console.WriteLine("[Progress]:[4] Sub Listening");

                // this.OnConnected();
            }
            catch (Exception ex)
            {

            }
        }

        private void _subscriber_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            if (null != msg && msg.FrameCount == 2)
            {
                Console.WriteLine("sub received:"+msg[1].ReadString());
            }
        }

        private void StopStationNetMQ()
        {
           // HockSocketEvents(false);
            try
            {
                if (null != _poller)
                {
                    if (null != _subscriber)
                    {
                        _poller.Remove(_subscriber);
                    }

                    if (null != _requester)
                    {
                        _poller.Remove(_requester);
                    }

                    //_poller.Remove(_fileSubscriber);

                    if (_poller.IsRunning)
                        _poller.Stop();

                    _poller.Dispose();
                    _poller = null;
                }

                if (null != _subscriber)
                {
                    _subscriber.Dispose();
                    _subscriber = null;
                }

                if (null != _requester)
                {
                    _requester.Dispose();
                    _requester = null;
                }

                //_fileSubscriber.Dispose();
                //_fileSubscriber = null;
            }
            catch (Exception err)
            {
                logger.ErrorFormat("Disconnect pub-sub error:{0}", err);
            }

           // this.OnDisconnected();
        }

        private bool CheckIsLocalIP(string zmqEndPort)
        {
            string zmqIP = GetIP(zmqEndPort);
            return string.Equals("127.0.0.1", zmqIP, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(zmqIP, GetLocalIPAddress(), StringComparison.OrdinalIgnoreCase);
        }
        private string GetIP(string zmqEndPort)
        {
            Regex regex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            var match = regex.Match(zmqEndPort);
            if (match.Success)
            {
                return match.Groups[0].Value;
            }
            return string.Empty;
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
    }
}
