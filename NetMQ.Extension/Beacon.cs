using NetMQ;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace NetMQ.Extension
{
    public class Beacon : IDisposable
    {
        private readonly BeaconNode _selfNode = new BeaconNode() { Guid = Guid.NewGuid() };
        private NetMQBeacon _beacon;//= new NetMQBeacon();
        private NetMQPoller _poller;
        private readonly Dictionary<BeaconNode, DateTime> _nodes = new Dictionary<BeaconNode, DateTime>();
        private readonly TimeSpan m_deadNodeTimeout = TimeSpan.FromSeconds(5);
        private NetMQTimer timer;

        public Beacon(string group, string name)
        {
            this.Group = group;
            this.Name = name;

            Initialize();
        }

        public string Name
        {
            get { return _selfNode.Name; }
            private set { _selfNode.Name = value; }
        }

        public string Group
        {
            get { return _selfNode.Group; }
            private set { _selfNode.Group = value; }
        }

        public bool IsRunning
        {
            get { return _poller != null && _poller.IsRunning; }
        }

        public BeaconNode SelfNode
        {
            get { return _selfNode; }
        }

        public int BeaconNodeCount
        {
            get { return this._nodes.Count; }
        }

        public List<BeaconNode> GetBeacons()
        {
            return _nodes.Keys.ToList();
        }

        private void Initialize()
        {
            if (_beacon == null)
            {
                _beacon = new NetMQBeacon();
                _beacon.Configure(9999, GetAvalibleAddress());
                _beacon.Subscribe("");
                _beacon.ReceiveReady += OnBeaconReady;

                _selfNode.Address = _beacon.BoundTo;
                _selfNode.HostName = _beacon.HostName;
                _selfNode.ArgumentsChanged += OnBeaconArgumentsChanged;
            }
        }

        private string GetAvalibleAddress()
        {
            //modified by tudh 2016-05-27， 严禁使用*，若使用*,而其他Beacon不使用*，则可能会出现发送msg，对方收不到的情况！
            string address = "*";
            string configAddress = ConfigurationManager.AppSettings["BeaconAddress"];
            if (configAddress != "*" && configAddress != "127.0.0.1" && GetAddress().Any() &&
                GetAddress().All(e => e.Address.ToString() != configAddress))
                configAddress = GetAddress().First().Address.ToString();

            if (!string.IsNullOrEmpty(configAddress))
            {
                address = configAddress;
            }
            else
            {
                var addressQuery = GetAddress();
                if (addressQuery.Any())
                    address = addressQuery.First().Address.ToString();
            }

            return address;
        }

        private IEnumerable<UnicastIPAddressInformation> GetAddress()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up &&
                            i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            i.NetworkInterfaceType != NetworkInterfaceType.Ppp);

            // From that, get all the UnicastAddresses.
            var addresses = interfaces
                .SelectMany(i => i.GetIPProperties().UnicastAddresses
                                  .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork));

            return addresses;
        }

        public void Start()
        {
            Initialize();

            if (IsRunning) return;

            _beacon.Subscribe("");
            _beacon.Publish(_selfNode.Serialize(), TimeSpan.FromSeconds(1));

            timer = new NetMQTimer(TimeSpan.FromSeconds(1));
            timer.Elapsed += ClearDeadNodes;

            _poller = new NetMQPoller { _beacon, timer };
            _poller.RunAsync();
        }

        public void Stop()
        {
            if (!IsRunning) return;

            this._nodes.Clear();

            _poller.Stop();
            _beacon.Unsubscribe();
            _poller.Remove(_beacon);
            _poller.Remove(timer);

            _poller.Dispose();
            _poller = null;

            _beacon.Dispose();
            _beacon = null;

            timer = null;
        }

        private void OnBeaconReady(object sender, NetMQBeaconEventArgs e)
        {
            try
            {
                var message = e.Beacon.Receive();
                if (message.Bytes == null || message.Bytes.Length == 0) return;
                
                BeaconNode node = new BeaconNode(message.PeerHost);
                node.Deserialize(message.Bytes);

                if (!_nodes.ContainsKey(node))
                {
                    _nodes.Add(node, DateTime.Now);

                    OnNodeConnected(node);
                }
                else
                {
                    _nodes[node] = DateTime.Now;
                }
            }
            catch
            {
            }
        }

        private void ClearDeadNodes(object sender, NetMQTimerEventArgs e)
        {
            // create an array with the dead nodes
            var deadNodes = _nodes.
                Where(n => DateTime.Now > n.Value + m_deadNodeTimeout)
                .Select(n => n.Key).ToArray();

            if (!deadNodes.Any()) return;

            // remove all the dead nodes from the nodes list and disconnect from the publisher
            foreach (var node in deadNodes)
            {
                _nodes.Remove(node);

                OnNodeDisConnected(node);
            }
        }

        private void OnBeaconArgumentsChanged(BeaconNode obj)
        {
            if (this.IsRunning)
                _beacon.Publish(_selfNode.Serialize(), TimeSpan.FromSeconds(1));
        }

        public event Action<Beacon, BeaconNode> NodeConnected;

        public event Action<Beacon, BeaconNode> NodeDisConnected;

        private void OnNodeConnected(BeaconNode node)
        {
            if (node != null && this.NodeConnected != null) this.NodeConnected(this, node);
        }

        private void OnNodeDisConnected(BeaconNode node)
        {
            if (node != null && this.NodeDisConnected != null) this.NodeDisConnected(this, node);
        }

        public void Dispose()
        {
            _nodes.Clear();

            if (_beacon != null) _beacon.Dispose();

            if (_poller != null) _poller.Dispose();
        }
    }
}