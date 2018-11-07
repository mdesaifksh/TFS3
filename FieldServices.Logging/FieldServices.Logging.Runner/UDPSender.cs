using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FieldServices.Logging.Runner
{
    public static class UDPSender
    {
        private static string _udpAddress;
        public static string UDPAddress
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_udpAddress))
                {
                    _udpAddress = System.Configuration.ConfigurationManager.AppSettings["LogStash.UDPAddress"];
                }
                if (string.IsNullOrWhiteSpace(_udpAddress))
                {
                    throw new ArgumentNullException("Config UDP Address");
                }
                return _udpAddress;
            }
            set { _udpAddress = value; }
        }

        private static int _udpPort;
        public static int UDPPort
        {
            get
            {
                if (_udpPort<=0)
                {
                    var port = System.Configuration.ConfigurationManager.AppSettings["LogStash.UDPPort"];
                    if (!int.TryParse(port, out _udpPort))
                    {
                        throw new ArgumentException($"Failed to parse Port: Input:{port};");
                    }
                }
                if (_udpPort <= 0)
                {
                    throw new ArgumentNullException("Config UDP Port");
                }
                return _udpPort;
            }
            set { _udpPort = value; }
        }

        public static void Send(List<LogMessage> messages)
        {            
            using (UdpClient c = new UdpClient(UDPAddress, UDPPort))
            {
                foreach (var message in messages)
                {
                    var json = message.ToJson();
                    var send_buffer = Encoding.ASCII.GetBytes(json);
                    c.Send(send_buffer, send_buffer.Length);
                }
            }
        }       
    }
}
