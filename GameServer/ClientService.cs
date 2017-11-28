using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameServer
{
    public class ClientService
    {
        private readonly Socket _client;

        public ClientService(Socket client)
        {
            _client = client;
            new Task(Listen).Start();
        }

        private void Listen()
        {
            try
            {
                while (true)
                {
                    byte[] bytes = new byte[1024];
                    var receivedBytes = _client.Receive(bytes);

                    if (receivedBytes == 0) break;
                    var cmdJson = Encoding.ASCII.GetString(bytes, 0, receivedBytes);

                    if (!ValidateJson(cmdJson)) continue;
                    var cmd = JsonConvert.DeserializeObject<Command>(cmdJson);
                    Console.WriteLine(cmd.CommandTerm);
                    ExecuteCommand(cmd.CommandTerm, cmd.Data);
                }

                GameService.Games.Where(g => g.PlayerOne == _client).ToList().ForEach(g => GameService.Games.Remove(g));
                SocketService.ConnectedEndpoints.Remove(_client);
                _client.Close();
            }
            catch (Exception)
            {
                GameService.Games.Where(g => g.PlayerOne == _client).ToList().ForEach(g => GameService.Games.Remove(g));
                SocketService.ConnectedEndpoints.Remove(_client);
                _client.Close();
            }
        }

        public void ExecuteCommand(string command, dynamic data)
        {
            Game game;
            switch (command)
            {
                case "CREATE": _client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(GameService.CreateGame(_client))));
                    break;
                case "JOIN":
                    bool result = GameService.JoinGame(_client, data);
                    _client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(result)));
                    if (result)
                    {
                        game = GameService.GetActiveGame(data);
                        game.PlayerOne.Send(
                            Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Command {CommandTerm = "JOIN"})));
                    }
                    break;
                case "GETGAMES": _client.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(GameService.GetGameIds())));
                    break;
                case "LEAVE":
                    _client.Send(Encoding.ASCII.GetBytes("Leaving game"));
                    GameService.LeaveGame(_client, data);
                    break;
                case "START":
                    game = GameService.GetActiveGame(data);
                    game.PlayerOne.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Command{ CommandTerm = "START" })));
                    game.PlayerTwo.Send(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(new Command { CommandTerm = "START" })));
                    break;
            }
        }
        private static bool ValidateJson(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(json);
                return true;
            }
            catch // not valid
            {
                return false;
            }
        }
    }
}
