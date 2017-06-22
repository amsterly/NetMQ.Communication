using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;

namespace NetMQ.Extension
{
    public class BeaconNode
    {
        public BeaconNode()
        {
            this.HostApp = AppDomain.CurrentDomain.FriendlyName;
        }

        public BeaconNode(string address)
            : this()
        {
            this.Address = address;
        }

        public Guid Guid { get; set; }

        private string _address;

        public string Address
        {
            get { return _address; }
            set
            {
                _address = value;
                try
                {
                    //this.HostName = Dns.GetHostEntry(Address).HostName;
                }
                catch
                {
                    //this.HostName = string.Empty;
                }
            }
        }

        public string HostName { get; set; }

        public string Group { get; set; }

        public string Name { get; set; }

        public string HostApp { get; set; }

        private string _arguments;

        public string Arguments
        {
            get { return _arguments; }
            set
            {
                if (_arguments == value) return;
                _arguments = value;
                OnArgumentsChanged();
            }
        }

        public event Action<BeaconNode> ArgumentsChanged;

        private void OnArgumentsChanged()
        {
            if (ArgumentsChanged != null) this.ArgumentsChanged(this);
        }

        public byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                //bw.Write(string.IsNullOrEmpty(Address) ? string.Empty : Address);
                bw.Write(this.Guid.ToString());
                bw.Write(string.IsNullOrEmpty(Group) ? string.Empty : Group);
                bw.Write(string.IsNullOrEmpty(Name) ? string.Empty : Name);
                bw.Write(string.IsNullOrEmpty(HostApp) ? string.Empty : HostApp);
                bw.Write(string.IsNullOrEmpty(HostName) ? string.Empty : HostName);
                bw.Write(string.IsNullOrEmpty(Arguments) ? string.Empty : Arguments);
                return ms.ToArray();
            }
        }

        public void Deserialize(byte[] buffer)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(buffer))
                using (BinaryReader bw = new BinaryReader(ms))
                {
                    //string value = bw.ReadString();
                    //Address = bw.ReadString();
                    try
                    {
                        Guid = new Guid(bw.ReadString());
                    }
                    catch (Exception)
                    {
                    }
                    Group = bw.ReadString();
                    Name = bw.ReadString();
                    HostApp = bw.ReadString();
                    HostName = bw.ReadString();
                    Arguments = bw.ReadString();
                }
            }
            catch (Exception)
            {
            }
        }

        protected bool Equals(BeaconNode other)
        {
            return string.Equals(Address, other.Address) && string.Equals(Name, other.Name) && Group == other.Group && Name == other.Name && Arguments == other.Arguments;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BeaconNode)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return this.ToString().GetHashCode();
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3}", Address, Group, Name, Arguments);
        }
    }
}