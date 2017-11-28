using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace GameServer
{
    public class Game
    {
        public int GameId { get; set; }
        public Socket PlayerOne { get; set; }
        public Socket PlayerTwo { get; set; }
        public bool IsRunning => PlayerOne != null && PlayerTwo != null;
    }
}
