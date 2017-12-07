using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace GameServer
{
    public class Game
    {
        public int GameId { get; set; }
        public string[,] ScoreGrid { get; set; } = new string[3,3];
        public Socket PlayerOne { get; set; }
        public Socket PlayerTwo { get; set; }
        public bool IsRunning => PlayerOne != null && PlayerTwo != null;
        public bool IsStarted { get; set; }
    }
}
