using NetMQ.Extension;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ.Communication.Server
{
    public class AlyServer_RepPubVersion
    {

        private readonly log4net.ILog logger = log4net.LogManager.GetLogger(typeof(AlyServer_RepPubVersion));
        private ResponseSocket _resSocket;
        private NetMQPoller _resPoller;
        string selfPubZmqParams = System.Configuration.ConfigurationManager.AppSettings["SelfPubZmqParams"];
        private PublisherSocket _publisher;
        private readonly Queue<NetMQMessage> _pubMsgQueue = new Queue<NetMQMessage>();
        private readonly Queue<NetMQMessage> _resMsgList = new Queue<NetMQMessage>(); 

        public void start()
        {
            StartQueryMQ();
        }
        public void stop()
        {
            StopQueryMQ();        }

        private void StartQueryMQ()
        {
            //request
            string ipAddress = System.Configuration.ConfigurationManager.AppSettings["AlyServerAddress"];
            string queryPort = System.Configuration.ConfigurationManager.AppSettings["AlyServerQueryPort"];
            //pub
            string[] selfParams = selfPubZmqParams.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string pubAddress = string.Format("tcp://{0}:{1}", selfParams[5], selfParams[1]);
            string resAddress = string.Format("tcp://{0}:{1}", selfParams[5], selfParams[3]);


            if (!string.IsNullOrEmpty(ipAddress) &&
               !string.IsNullOrEmpty(queryPort)
               )
            {
                string address = string.Format("tcp://{0}:{1}", ipAddress, queryPort);
                try
                {
                    if (_resSocket == null)
                    {
                        _resSocket = new ResponseSocket();

                        _resSocket.ReceiveReady += _resSocket_ReceiveReady;
                        _resSocket.SendReady += _resSocket_SendReady;
                        _resSocket.Bind(address);
                    }

                   
                    if (_publisher==null)
                    {
                        _publisher = new PublisherSocket();
                        _publisher.Bind(pubAddress);
                        _publisher.SendReady += _publisher_SendReady;
                    }

                    if (_resPoller == null)
                    {
                        _resPoller = new NetMQPoller();
                        _resPoller.Add(_resSocket);
                        _resPoller.Add(_publisher);
                        _resPoller.RunAsync();
                        Console.WriteLine("[Progress]:[1]Pub Response Listening");
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }

            }
        }

        private void _resSocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                if (e.IsReadyToSend && _resMsgList.Count > 0)
                {
                    var msg = _resMsgList.Dequeue();
                    e.Socket.SendMultipartMessage(msg);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void _publisher_SendReady(object sender, NetMQSocketEventArgs e)
        {
            try
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
            catch (Exception err)
            {
                logger.ErrorFormat("Pub message error:{0}", err);
            }
        }
    



        private void _resSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            try
            {
                if (e.IsReadyToReceive)
                {
                    NetMQMessage msg = e.Socket.ReceiveMultipartMessage();
                    if (null != msg && msg.FrameCount >= 3)
                    {
                        NetMQMessage resMsg = OnQueryMessage(msg);
                        if (null != resMsg)
                        {
                            e.Socket.SendMultipartMessage(resMsg);
                            Console.WriteLine("[Progress]:[2] Response Send Para");
                        }
                    }
                    else if (null != msg && msg.FrameCount == 2)//字符串 显示并pub
                    {
                        if ((EnumMessageFlag)msg[0].ReadAsByte() == EnumMessageFlag.Str)
                        {
                            string str = msg[1].ReadString();
                            Console.WriteLine("AlyServer:" + str);
                          
                            sendMsg(str);
                        }

                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public void sendMsg(string nmsg)
        {
            NetMQMessage m = new NetMQMessage();
            m.Append(CreateFrame((byte)EnumMessageFlag.Str));
            m.Append("$$AlyServer$$:" + nmsg);
          //  _resSocket.SendFrame("aaaaa");
          //  _resMsgList.Enqueue(m);
            _pubMsgQueue.Enqueue(m);
            ////rep 返回
            //_resSocket.SendMultipartMessage(m);
            ////pub 返回
            //_publisher.SendMultipartMessage(m);
        }

        private NetMQFrame CreateFrame(long value)
        {
            return new NetMQFrame(BitConverter.GetBytes(value));
        }

        private NetMQMessage OnQueryMessage(NetMQMessage msg)
        {
           
            NetMQMessage resMsg = null;
            try
            {
                string from = msg[0].ReadString();
                string queryType = msg[1].ReadString();
                string stationId = msg[2].ReadString();
                switch (queryType)
                {
                    case "AlyClient":

                        resMsg = CreateQueryMessage_PClient(msg);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

            }
            return resMsg;
        }

        private void StopQueryMQ()
        {
            try
            {
                if (null != _resPoller)
                {
                    _resPoller.Stop();
                    _resPoller.Remove(_resSocket);
                    _resSocket.ReceiveReady -= _resSocket_ReceiveReady;
                    _resSocket.Dispose();
                    _resPoller.Dispose();
                    _resSocket = null;
                    _resPoller = null;
                }
            }
            catch (Exception ex)
            {


            }
        }
        private NetMQMessage CreateQueryMessage_PClient(NetMQMessage orgMsg)
        {
            NetMQMessage msg = new NetMQMessage();
            foreach (var frame in orgMsg)
            {
                msg.Append(frame.Duplicate());
            }
            msg.Append(string.Format("{0} -d {1}", selfPubZmqParams, "C:\\"));


            return msg;
        }


    }
}
