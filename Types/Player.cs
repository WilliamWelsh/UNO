using Discord;
using Discord.WebSocket;

namespace UNO.Types
{
    public class Player
    {
        /// <summary>
        /// SocketUser of this player
        /// </summary>
        public SocketUser User { get; set; }

        /// <summary>
        /// This player's cards
        /// </summary>
        public List<Card> Deck { get; set; }

        /// <summary>
        /// The game object this player belongs to
        /// </summary>
        public Game Game { get; set; }

        /// <summary>
        /// Random object used for generating cards
        /// </summary>
        private Random Random { get; set; }

        /// <summary>
        /// The ephemeral card menu message
        /// </summary>
        public SocketMessageComponent CardMenuMessage { get; set; }

        public Player(SocketUser user, Random rnd, Game game)
        {
            User = user;
            Deck = new List<Card>();
            Game = game;

            Random = rnd;

            // Give them 7 random cards
            for (int i = 0; i < 7; i++)
                AddNewCard();
        }

        private bool isItMyTurn()
        {
            return Game.Players[Game.CurrentPlayerIndex] == this;
        }

        /// <summary>
        /// Add a random card and sort the deck
        /// </summary>
        private void AddNewCard(Card newCard = null)
        {
            if (newCard == null)
                Deck.Add(new Card(Random));
            else
                Deck.Add(newCard);

            Deck = Deck.OrderBy(c => c.Color).ThenBy(c => c.Number).ThenBy(c => c.Special).ToList();
        }

        public override string ToString() => User.Username;

        /// <summary>
        /// Draw a card
        /// </summary>
        public async Task DrawCard(SocketMessageComponent command)
        {
            // Give them a random card
            var newCard = new Card(Random);
            AddNewCard(newCard);

            // Update the game info
            Game.SetInfoMessage($"{User.Username} drew a card");

            // If they hit the max, kick them out
            if (Deck.Count >= 24)
            {
                // Update the game info
                Game.SetInfoMessage($"{User.Username} hit the max cards (24) and was kicked");

                // Kick them out
                Game.Players.Remove(this);

                // Update
                await command.UpdateAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithColor(Colors.Red)
                        .WithDescription($"You hit the max cards (24) and were kicked. Better luck next time. 😔")
                        .Build();

                    m.Components = null;
                });

                // Do a turn
                await Game.DoTurn(Game.CurrentCard);
            }
            else
                await UpdateCardMenu(command, $"You drew a {newCard}.");
        }

        /// <summary>
        /// Draw multiple cards
        /// </summary>
        public async Task DrawCards(int count)
        {
            // Give them random cards
            for (int i = 0; i < count; i++)
                AddNewCard();

            var message = $"You drew {count} cards.";

            // If they hit the max, kick them out
            if (Deck.Count >= 24)
            {
                // Update the game info
                Game.SetInfoMessage($"{User.Username} hit the max cards (24) and was kicked");

                // Update player info
                Deck.Clear();
                message = $"You hit the max cards (24) and were kicked. Better luck next time. 😔";

                // Kick them out
                Game.Players.Remove(this);
            }

            await UpdateCardMenu(null, message);
        }

        /// <summary>
        /// Provides a random number to put in the customId in case the player has a duplicate card
        /// </summary>
        private int RandomDiscriminator() => Random.Next(0, 1000000000);

        /// <summary>
        /// When the player does /cards
        /// </summary>
        public async Task ShowInitialCardMenu(SocketSlashCommand command)
        {
            await command.RespondAsync(component: new ComponentBuilder()
                .WithButton("Click here to view your cards", "showcardmenu", style: ButtonStyle.Secondary)
                .Build());
        }

        /// <summary>
        /// Update the /cards menu
        /// </summary>
        public async Task UpdateCardMenu(SocketMessageComponent command, string extraMessage = "")
        {
            var buttons = new ComponentBuilder();

            var row = 0;
            var count = 0;
            var index = 0;

            foreach (var card in Deck)
            {
                buttons.WithButton(card.ToString(), $"card{RandomDiscriminator()}-{Game.Host.User.Id}-{card.Color}-{card.Number}-{card.Special}-{index}", style: ButtonStyle.Secondary, row: row, emote: card.GetColorEmoji(), disabled: !isItMyTurn() || !CheckIfCardCanBePlayed(card));

                count++;
                index++;

                if (count == 6)
                {
                    count = 0;
                    row++;
                }
            }

            // Add the draw card button
            buttons.WithButton("Draw Card", "drawcard", style: ButtonStyle.Secondary, row: row, disabled: !isItMyTurn());

            if (extraMessage != "")
                extraMessage = $"\n\n**{extraMessage}**";

            if (command == null)
            {
                try
                {
                    await CardMenuMessage.ModifyOriginalResponseAsync(m =>
                    {
                        m.Content = "";

                        m.Embed = new EmbedBuilder()
                            .WithColor(isItMyTurn() ? Colors.Green : Colors.Red)
                            .WithDescription($"You have {Deck.Count} cards.{extraMessage}{(isItMyTurn() ? "\n\nIt's your turn." : "")}")
                            .Build();

                        m.Components = buttons.Build();
                    });
                }
                // Ignore, they probably dismissed the message or something
                catch { }
            }
            else
            {
                CardMenuMessage = command;
                await CardMenuMessage.UpdateAsync(m =>
                    {
                        m.Content = "";

                        m.Embed = new EmbedBuilder()
                            .WithColor(isItMyTurn() ? Colors.Green : Colors.Red)
                            .WithDescription($"You have {Deck.Count} cards.{extraMessage}{(isItMyTurn() ? "\n\nIt's your turn." : "")}")
                            .Build();

                        m.Components = buttons.Build();
                    });
            }
        }

        /// <summary>
        /// Check if it's this players turn
        /// </summary>
        /// <returns>True if it's this player's turn</returns>
        public async Task<bool> CheckIfItsMyTurn(SocketMessageComponent command)
        {
            if (isItMyTurn())
                return true;

            await UpdateCardMenu(command, "It's not your turn");
            return false;
        }

        /// <summary>
        /// Check if this card can be played in the curent game
        /// </summary>
        public bool CheckIfCardCanBePlayed(Card inputCard)
        {
            // Special cards of the same color can be played
            if ((inputCard.Special != Special.None && inputCard.Color == Game.CurrentCard.Color) ||
            // Cards of the same color can be played
                inputCard.Color == Game.CurrentCard.Color ||
                // Wild Cards
                (inputCard.Special == Special.Wild || inputCard.Special == Special.WildPlusFour) ||
                // Special cards of the same type
                inputCard.Special == Game.CurrentCard.Special && inputCard.Special != Special.None ||
                // Cards of the same number can be played
                inputCard.Number == Game.CurrentCard.Number && inputCard.Number != "")
                return true;

            return false;
        }

        /// <summary>
        /// Play a valid card
        /// </summary>
        public async Task PlayCard(SocketMessageComponent command, Card inputCard, int index)
        {
            // Remove the card from the player's deck
            Deck.RemoveAt(index);

            // Play the card in the game
            await Game.DoTurn(inputCard);

            // Update the deck
            if (!Game.isGameOver)
                await UpdateCardMenu(command, $"You played a {inputCard.ToString()}");
        }

        /// <summary>
        /// Show the wild card menu
        /// </summary>
        public async Task ShowWildMenu(SocketMessageComponent command, Special special, int index)
        {
            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Green)
                    .WithDescription("Select a color for this Wild card.")
                    .Build();

                m.Components = new ComponentBuilder()
                    .WithButton("Red", $"wild-Red-{special}-{index}", style: ButtonStyle.Secondary, new Emoji("🟥"))
                    .WithButton("Green", $"wild-Green-{special}-{index}", style: ButtonStyle.Secondary, new Emoji("🟩"))
                    .WithButton("Blue", $"wild-Blue-{special}-{index}", style: ButtonStyle.Secondary, new Emoji("🟦"))
                    .WithButton("Yellow", $"wild-Yellow-{special}-{index}", style: ButtonStyle.Secondary, new Emoji("🟨"))
                    .WithButton("Cancel", "cancelwild", style: ButtonStyle.Secondary)
                    .Build();
            });
        }

        /// <summary>
        /// Removes the game cards because the game is over
        /// </summary>
        public async Task ShowEndGameCardMenu()
        {
            try
            {
                await CardMenuMessage.ModifyOriginalResponseAsync(x =>
                {
                    x.Content = "The game is over, I hope you had fun 😊";
                    x.Embed = null;
                    x.Components = new ComponentBuilder().Build();
                });
            }
            catch { }
        }
    }
}