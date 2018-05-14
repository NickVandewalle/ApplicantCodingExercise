using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker.Core
{
    public enum Suit
    {
        Diamonds,
        Hearts,
        Spades,
        Clubs,
    }

    public enum Value
    {
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
        Ace,
    }

    /// <summary>
    /// An immutable representation of a card.
    /// </summary>
    public class Card : IComparable<Card>, IComparable
    {
        public Suit Suit { get; }
        public Value Value { get; }

        public Card(Suit suit, Value value)
        {
            Suit = suit;
            Value = value;
        }

        #region Equality

        public override bool Equals(object obj) 
            => base.Equals(obj);

        protected bool Equals(Card other) 
            => Suit == other.Suit && Value == other.Value;

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Suit * 397) ^ (int) Value;
            }
        }
        
        #endregion Equality

        #region Comparable

        public int CompareTo(object obj) => CompareTo(obj as Card);

        public int CompareTo(Card other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;

            return Value.CompareTo(other.Value);
        }

        #endregion Comparable

        public override string ToString()
        {
            return $"{Value} of {Suit}";
        }
    }

    public class Deck : IEnumerable<Card>
    {
        private Stack<Card> _cards = new Stack<Card>();

        public Deck()
        {
            Reset();
            Shuffle();
        }

        /// <summary>
        /// Pop a card off the stack of cards.
        /// </summary>
        public Card Deal() => _cards.Pop();

        /// <summary>
        /// Randomize the current collection of cards.
        /// </summary>
        public void Shuffle()
        {
            var random = new Random();

            var currentCards = new List<Card>(_cards);
            var newCards = new List<Card>();

            while (currentCards.Any())
            {
                var nextRandom = random.Next(currentCards.Count);
                newCards.Add(currentCards[nextRandom]);
                currentCards.RemoveAt(nextRandom);
            }

            _cards = new Stack<Card>(newCards);
        }

        /// <summary>
        /// Will reset the collection of cards to its initial state.
        /// </summary>
        public void Reset()
        {
            var newCards = new Stack<Card>();

            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
                foreach (Value value in Enum.GetValues(typeof(Value)))
                    newCards.Push(new Card(suit, value));

            _cards = new Stack<Card>(newCards);
        }

        #region IEnumerable

        public IEnumerator<Card> GetEnumerator() => _cards.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable
    }

    public class Player
    {
        private readonly List<Card> _hand = new List<Card>();

        public string Name { get; }
        public IReadOnlyList<Card> Hand => _hand;

        public Player(string name)
        {
            Name = name;
        }

        public void AddCard(Card card) => _hand.Add(card);

        public void ClearHand() => _hand.Clear();

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach (var card in Hand)
                stringBuilder.Append($"[{card}]");

            return stringBuilder.ToString();
        }
    }

    public class Table
    {
        private readonly List<Card> _cards = new List<Card>();
        
        public IReadOnlyList<Card> Cards => _cards;

        public void AddCard(Card card) => _cards.Add(card);

        public void ClearCards() => _cards.Clear();

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach (var card in Cards)
                stringBuilder.Append($"[{card}]");

            return stringBuilder.ToString();
        }
    }

    public class Game
    {
        private readonly Deck _deck;
        private readonly Table _table;
        private readonly Player[] _players;

        public Game(Deck deck, Table table, params Player[] players)
        {
            _deck = deck;
            _table = table;
            _players = players;
        }

        public void Loop()
        {
            // Deal each player a card, twice.
            foreach (var player in _players)
                player.AddCard(_deck.Deal());
            foreach (var player in _players)
                player.AddCard(_deck.Deal());

            // Flop
            _table.AddCard(_deck.Deal());
            _table.AddCard(_deck.Deal());
            _table.AddCard(_deck.Deal());
            // Turn
            _table.AddCard(_deck.Deal());
            // River
            _table.AddCard(_deck.Deal());
        }

        public Player DetermineWinner()
        {
            var bestHandsInOrder = _players
                .Select(x => new { Player = x, OptimalHand = DetermineOptimalHand(x) })
                .OrderByDescending(x => x.OptimalHand)
                .ToList();

            return bestHandsInOrder.First().Player;
        } 
        
        public OptimalHand DetermineOptimalHand(Player player)
        {
            var availableCards = player.Hand.Union(_table.Cards)
                .OrderByDescending(x => x.Value)
                .ToList();

            return GetStraightFlush(availableCards)
                   ?? GetFourOfAKind(availableCards)
                   ?? GetFullHouse(availableCards)
                   ?? GetFlush(availableCards)
                   ?? GetStraight(availableCards)
                   ?? GetThreeOfAKind(availableCards)
                   ?? GetTwoPair(availableCards)
                   ?? GetPair(availableCards)
                   ?? GetHighCard(availableCards);
        }

        private OptimalHand GetStraightFlush(IReadOnlyList<Card> orderedCards)
        {
            var optimalCards = new List<Card>();

            var groups = orderedCards
                .GroupBy(x => x.Suit)
                .OrderByDescending(x => x.Count())
                .ToList();

            var flush = groups.SingleOrDefault(x => x.Count() >= 5);
            if (flush != null)
            {
                var straight = GetStraight(flush.ToList());

                if (straight != null)
                {
                    return new OptimalHand(TypeOfHand.StraightFlush, straight.Cards);
                }
            }

            return null;
        }

        private OptimalHand GetFourOfAKind(IReadOnlyList<Card> orderedCards)
        {
            var optimalCards = new List<Card>();

            var groups = orderedCards
                .GroupBy(x => x.Value)
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => x.Key)
                .ToList();

            var quads = groups.SingleOrDefault(x => x.Count() == 4);
            if (quads != null)
            {
                optimalCards.AddRange(quads);
                optimalCards.AddRange(orderedCards.Except(optimalCards).Take(5 - optimalCards.Count));
                return new OptimalHand(TypeOfHand.FourOfAKind, optimalCards);
            }

            return null;
        }

        private OptimalHand GetFullHouse(IReadOnlyList<Card> orderedCards)
        {
            var optimalCards = new List<Card>();

            var groups = orderedCards
                .GroupBy(x => x.Value)
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => x.Key)
                .ToList();

            var trips = groups.Where(x => x.Count() == 3).ToList();
            var pair = groups.Where(x => x.Count() == 2).ToList();
            if (trips.Any() && pair.Any())
            {
                optimalCards.AddRange(trips.First());
                optimalCards.AddRange(pair.First());
                return new OptimalHand(TypeOfHand.FullHouse, optimalCards);
            }

            return null;
        }

        private OptimalHand GetFlush(IReadOnlyList<Card> orderedCards)
        {
            var optimalCards = new List<Card>();

            var groups = orderedCards
                .GroupBy(x => x.Suit)
                .OrderByDescending(x => x.Count())
                .ToList();

            var flush = groups.SingleOrDefault(x => x.Count() >= 5);
            if (flush != null)
            {
                optimalCards.AddRange(flush.Take(5));
                return new OptimalHand(TypeOfHand.Flush, optimalCards);
            }

            return null;
        }

        private OptimalHand GetStraight(IReadOnlyList<Card> orderedCards)
        {
            var possibleStraights = new List<List<Card>>();
            for (int toSkip = 0; toSkip <= orderedCards.Count - 5; toSkip++)
                possibleStraights.Add(orderedCards.Skip(toSkip).Take(5).ToList());

            // Edgecase: Adds the bottom straight (containing an Ace)
            var bottomStraight = orderedCards.Skip(orderedCards.Count-4).Take(4).ToList();
            bottomStraight.Add(orderedCards.First());
            possibleStraights.Add(bottomStraight);

            foreach (var possibleStraight in possibleStraights)
            {
                if (isStraight(possibleStraight)) return new OptimalHand(TypeOfHand.Straight, possibleStraight);
            }

            return null;
        }

        private bool isStraight(IReadOnlyList<Card> orderedCards)
        {
            if (orderedCards.Count != 5) return false;

            for (int index = 0; index < orderedCards.Count-1; index++)
            {
                var lValue = (int) orderedCards[index].Value;
                var rValue = orderedCards[index + 1].Value != Value.Ace ? (int) orderedCards[index + 1].Value : 1; // Edgecase: An Ace should be considered both the highest and lowest value.

                if (lValue - rValue != 1) return false;
            }

            return true;
        }

        private OptimalHand GetThreeOfAKind(IReadOnlyList<Card> orderedCards)
        {
            var optimalCards = new List<Card>();

            var groups = orderedCards
                .GroupBy(x => x.Value)
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => x.Key)
                .ToList();

            var trips = groups.Where(x => x.Count() == 3).ToList();
            if (trips.Any())
            {
                optimalCards.AddRange(trips.First());
                optimalCards.AddRange(orderedCards.Except(optimalCards).Take(5 - optimalCards.Count));
                return new OptimalHand(TypeOfHand.ThreeOfAKind, optimalCards);
            }

            return null;
        }

        private OptimalHand GetTwoPair(IReadOnlyList<Card> orderedCards)
        {
            var optimalCards = new List<Card>();

            var groups = orderedCards
                .GroupBy(x => x.Value)
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => x.Key)
                .ToList();

            var pairs = groups.Where(x => x.Count() == 2).ToList();
            if (pairs.Count == 2)
            {
                optimalCards.AddRange(pairs[0]);
                optimalCards.AddRange(pairs[1]);
                optimalCards.AddRange(orderedCards.Except(optimalCards).Take(5 - optimalCards.Count));
                return new OptimalHand(TypeOfHand.TwoPair, optimalCards);
            }

            return null;
        }

        private OptimalHand GetPair(IReadOnlyList<Card> orderedCards)
        {
            var optimalCards = new List<Card>();

            var groups = orderedCards
                .GroupBy(x => x.Value)
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => x.Key)
                .ToList();

            var pair = groups.Where(x => x.Count() == 2).ToList();
            if (pair.Any())
            {
                optimalCards.AddRange(pair.First());
                optimalCards.AddRange(orderedCards.Except(optimalCards).Take(5 - optimalCards.Count));
                return new OptimalHand(TypeOfHand.Pair, optimalCards);
            }

            return null;
        }

        private OptimalHand GetHighCard(IReadOnlyList<Card> orderedCards)
        {
            return new OptimalHand(TypeOfHand.HighCard, orderedCards.Take(5));
        }
    }

    public enum TypeOfHand
    {
        HighCard,
        Pair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush
    }

    public class OptimalHand : IComparable<OptimalHand>, IComparable
    {
        public TypeOfHand Type { get; }
        public IReadOnlyList<Card> Cards { get; }

        public OptimalHand(TypeOfHand type, IEnumerable<Card> cards)
        {
            Type = type;
            Cards = new List<Card>(cards);
        }

        #region IComparable

        public int CompareTo(object obj) => CompareTo(obj as OptimalHand);

        public int CompareTo(OptimalHand other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;

            // Check whether the type of hand is different and if it is we already know which hand is better.
            var typeComparison = Type.CompareTo(other.Type);
            if (typeComparison != 0) return typeComparison;

            // If both hands are of the same type we should compare individual cards until we find a difference.
            for(int index = 0; index < Cards.Count; index++)
            {
                var lCard = Cards[index];
                var rCard = other.Cards[index];

                var cardComparison = lCard.CompareTo(rCard);
                if (cardComparison != 0) return cardComparison;
            }

            // Both hands are identical.
            return 0;
        }

        #endregion IComparable

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{Type}: ");
            foreach (var card in Cards)
                stringBuilder.Append($"[{card}]");

            return stringBuilder.ToString();
        }
    }
}