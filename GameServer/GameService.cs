using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace GameServer
{
    public static class GameService
    {
        public static List<Game> Games = new List<Game>();

        public static int CreateGame(Socket playerOne)
        {
            var previousGame = Games.OrderByDescending(g => g.GameId).FirstOrDefault();
            var newGame = new Game
            {
                GameId = previousGame?.GameId + 1 ?? 1,
                PlayerOne = playerOne
            };
            Games.Add(newGame);

            return newGame.GameId;
        }

        public static bool JoinGame(Socket playerTwo, string gameId)
        {
            var game = Games.SingleOrDefault(g => g.GameId == int.Parse(gameId));

            if (game != null && game.PlayerTwo == null)
            {
                game.PlayerTwo = playerTwo;
                return true;
            }

            return false;
        }

        public static Game GetActiveGame(string gameId)
        {
            return Games.SingleOrDefault(g => g.GameId == int.Parse(gameId) && g.IsRunning);
        }

        public static List<int> GetGameIds()
        {
            return Games.Where(g => g.IsRunning == false).Select(g => g.GameId).ToList();
        }

        public static void LeaveGame(Socket player, string gameId)
        {
            var game = Games.SingleOrDefault(g => g.GameId == int.Parse(gameId));
            if (game != null)
            {
                if (game.PlayerOne == player)
                    Games.Remove(game);
                else
                    game.PlayerTwo = null;
            }
        }
    }
}
