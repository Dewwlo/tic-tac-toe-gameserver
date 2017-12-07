using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GameServer
{
    public static class SocketService
    {
        public static List<Socket> ConnectedEndpoints = new List<Socket>();
        
        public static Socket ServerSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(EndPoint());
            return socket;
        }

        private static IPEndPoint EndPoint()
        {
            return new IPEndPoint(GetLocalIpAddress(), 8080);
        }

        private static IPAddress GetLocalIpAddress()
        {
            var localIPv4Address = Dns.GetHostAddresses(Dns.GetHostName())
                .First(a => a.AddressFamily == AddressFamily.InterNetwork);
            if (localIPv4Address != null)
                return localIPv4Address;

            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}
