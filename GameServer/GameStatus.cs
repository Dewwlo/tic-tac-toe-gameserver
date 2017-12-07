using System;
using System.Collections.Generic;
using System.Text;

namespace GameServer
{
    public class GameStatus
    {
        public int GameId { get; set; }
        public string[,] Grid { get; set; }
        public string Turn { get; set; }
        public PlayerInfo PlayerOne { get; set; }
        public PlayerInfo PlayerTwo { get; set; }
    }
}
