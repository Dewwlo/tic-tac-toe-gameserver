using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class SocketServer
    {
        private readonly Socket _socket = SocketService.ServerSocket();
        
        public SocketServer()
        {
            new Task(Listen).Start();
            ShowServerEndpoint();
            Console.ReadLine();
        }

        private void Listen()
        {
            while (true)
            {
                _socket.Listen(1);
                var client = _socket.Accept();
                var clientService = new ClientService(client);
                SocketService.ConnectedEndpoints.Add(client);
                //client.Send(Encoding.ASCII.GetBytes($"You are now connected to server {((IPEndPoint)_socket.LocalEndPoint).Address}"));
                Console.WriteLine($"{client.LocalEndPoint} is now connected to the server -- Connected clients {SocketService.ConnectedEndpoints.Count}");
            }
        }

        private void ShowServerEndpoint()
        {
            Console.WriteLine("Listen to...");
            Console.WriteLine($"IPv4 Address: {((IPEndPoint)_socket.LocalEndPoint).Address}");
            Console.WriteLine($"Assigned port: {((IPEndPoint)_socket.LocalEndPoint).Port}");
        }
    }
}
