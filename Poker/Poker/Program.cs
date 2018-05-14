using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Poker.Core;

namespace Poker
{
    class Program
    {
        static void Main(string[] args)
        {
            var deck = new Deck();
            var table = new Table();
            var nick = new Player("Nick");
            var frank = new Player("Frank");
            var game = new Game(deck, table, nick, frank);

            var repeat = true;
            while (repeat)
            {
                deck.Reset();
                deck.Shuffle();
                nick.ClearHand();
                frank.ClearHand();
                table.ClearCards();

                game.Loop();
                
                Console.WriteLine($"Nick: {nick}");
                Console.WriteLine($"Frank: {frank}");
                Console.WriteLine($"Table: {table}");

                var winner = game.DetermineWinner();
                var hand = game.DetermineOptimalHand(winner);
                Console.WriteLine($"Winner: {winner.Name} ({hand.Type})");

                Console.WriteLine("Repeat? y/n");
                if (Console.ReadKey().KeyChar != 'y') repeat = false;
                Console.WriteLine("");
                Console.WriteLine("---");
                Console.WriteLine("");
            }
        }
    }
}
