﻿using System;
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

        private void SendToClient(string data)
        {
            _client.Send(Encoding.UTF8.GetBytes(data));
        }

        private void SendToSocket(Socket socket, string data)
        {
            socket.Send(Encoding.UTF8.GetBytes(data));
        }

        private void Listen()
        {
            while (true)
            {
                var data = new byte[1024];
                var recv = 0;

                try
                {
                    recv = _client.Receive(data);
                }
                catch (Exception)
                {
                    GameService.Games.Where(g => g.PlayerOne == _client).ToList()
                        .ForEach(g => GameService.Games.Remove(g));
                    SocketService.ConnectedEndpoints.Remove(_client);
                    _client.Close();
                }

                if (recv == 0) break;

                var receivedData = Encoding.UTF8.GetString(data, 0, recv).Split(';');
                ParseCommand(receivedData[0], receivedData[1]);
            }

            GameService.Games.Where(g => g.PlayerOne == _client).ToList().ForEach(g => GameService.Games.Remove(g));
            SocketService.ConnectedEndpoints.Remove(_client);
            Console.WriteLine($"A client has disconnected from the server -- Connected clients {SocketService.ConnectedEndpoints.Count}.");
            _client.Close();
        }

        public void ParseCommand(string command, string data)
        {
            Game game;
            switch (command)
            {
                case "CREATE":
                    SendToClient($"CREATE;{GameService.CreateGame(_client)}");
                    PrintConsoleMessage(ConsoleColor.Blue, $"{_client.LocalEndPoint} Created a game.", null);
                    break;
                case "JOIN":
                    var result = GameService.JoinGame(_client, data);
                    SendToClient($"JOIN;{result}");
                    if (!result) break;
                    if (GameService.GetActiveGame(data).IsStarted)
                        SendToClient(
                            $"SENDGAMESTATUS;{JsonConvert.SerializeObject(GameService.GameStatuses.SingleOrDefault(g => g.GameId == int.Parse(data)))}");
                    game = GameService.GetActiveGame(data);
                    SendToSocket(game.PlayerOne, "OPPONENTJOIN;");
                    PrintConsoleMessage(ConsoleColor.Cyan, $"{_client.LocalEndPoint} Joined game {data}.", null);
                    break;
                case "GETGAMES":
                    SendToClient($"GETGAMES;{JsonConvert.SerializeObject(GameService.GetGameIds())}");
                    PrintConsoleMessage(ConsoleColor.Gray, $"{_client.LocalEndPoint} Requested game list.", null);
                    break;
                case "LEAVE":
                    SendToClient("LEAVE;");
                    GameService.LeaveGame(_client, data);
                    PrintConsoleMessage(ConsoleColor.Yellow, $"{_client.LocalEndPoint} Has left game {data}.", null);
                    break;
                case "START":
                    game = GameService.GetActiveGame(data);
                    var newGame = InitGamePlay(game);
                    GameService.GameStatuses.Add(newGame);
                    SendToSocket(game.PlayerOne, $"START;{JsonConvert.SerializeObject(newGame)}");
                    SendToSocket(game.PlayerTwo, $"START;{JsonConvert.SerializeObject(newGame)}");
                    PrintConsoleMessage(ConsoleColor.Green, $"{_client.LocalEndPoint} Started game {data}.", null);
                    break;
                case "TURN":
                    var gameStatus = JsonConvert.DeserializeObject<GameStatus>(data);
                    game = GameService.GetActiveGame(gameStatus.GameId.ToString());
                    game.ScoreGrid = gameStatus.Grid;
                    GameService.GameStatuses.Remove(GameService.GameStatuses.Find(g => g.GameId == gameStatus.GameId));
                    GameService.GameStatuses.Add(gameStatus);
                    SendToSocket(_client == game.PlayerOne ? game.PlayerTwo : game.PlayerOne,
                        $"TURN;{JsonConvert.SerializeObject(gameStatus)}");
                    PrintConsoleMessage(ConsoleColor.White,
                        $"{_client.LocalEndPoint} Played a round in game {game.GameId}.", null);
                    if (CheckVictoryCondition(gameStatus.Grid))
                    {
                        GameOver(game.PlayerOne, game.PlayerTwo, gameStatus.Turn == "X" ? "O" : "X");
                        PrintConsoleMessage(ConsoleColor.Magenta,
                            $"{_client.LocalEndPoint} Has won game {game.GameId}!", null);
                    }
                    else if (gameStatus.Grid.Cast<string>().All(f => f != null))
                    {
                        GameOver(game.PlayerOne, game.PlayerTwo, null);
                        PrintConsoleMessage(ConsoleColor.Magenta, $"{_client.LocalEndPoint} Game {game.GameId} ended with a draw.", null);
                    }
                    break;
            }
        }

        public GameStatus InitGamePlay(Game game)
        {
            game.IsStarted = true;
            return new GameStatus
            {
                GameId = game.GameId,
                Grid = game.ScoreGrid,
                PlayerOne = new PlayerInfo {PlayerColor = "Red", PlayerSign = "X"},
                PlayerTwo = new PlayerInfo {PlayerColor = "Blue", PlayerSign = "O"},
                Turn = new Random().Next(1, 3) == 1 ? "X" : "O"
            };
        }

        public void GameOver(Socket playerOne, Socket playerTwo, string winner)
        {
            SendToSocket(playerOne, $"GAMEOVER;{winner}");
            SendToSocket(playerTwo, $"GAMEOVER;{winner}");
        }

        public bool CheckVictoryCondition(string[,] gameGrid)
        {
            var resultCheck = new List<bool>
            {
                AllAreEqual(gameGrid[0, 0], gameGrid[0, 1], gameGrid[0, 2]),
                AllAreEqual(gameGrid[0, 0], gameGrid[1, 1], gameGrid[2, 2]),
                AllAreEqual(gameGrid[1, 0], gameGrid[1, 1], gameGrid[1, 2]),
                AllAreEqual(gameGrid[2, 0], gameGrid[2, 1], gameGrid[2, 2]),
                AllAreEqual(gameGrid[0, 0], gameGrid[1, 0], gameGrid[2, 0]),
                AllAreEqual(gameGrid[0, 1], gameGrid[1, 1], gameGrid[2, 1]),
                AllAreEqual(gameGrid[0, 2], gameGrid[1, 2], gameGrid[2, 2]),
                AllAreEqual(gameGrid[0, 2], gameGrid[1, 1], gameGrid[2, 0])
            };

            return resultCheck.Any(t => t);
        }

        static bool AllAreEqual<T>(params T[] args)
        {
            if (args.All(a => a != null))
                return args.Distinct().ToArray().Length == 1;

            return false;
        }

        public static void PrintConsoleMessage(ConsoleColor foreColor, string message, ConsoleColor? backColor)
        {
            Console.ForegroundColor = foreColor;
            if (backColor != null)
                Console.BackgroundColor = (ConsoleColor) backColor;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}