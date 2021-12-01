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
        private SocketMessageComponent CardMenuMessage { get; set; }

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
                await UpdateCardMenu(command, $"You drew a {newCard}. It's still your turn.");
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
        /// Show the player's cards and options
        /// </summary>
        public async Task ShowCardMenu(SocketSlashCommand command)
        {
            var buttons = new ComponentBuilder();

            var row = 0;
            var count = 0;
            var index = 0;

            foreach (var card in Deck)
            {
                buttons.WithButton(card.ToString(), $"card{RandomDiscriminator()}-{Game.Host.User.Id}-{card.Color}-{card.Number}-{card.Special}-{index}", style: ButtonStyle.Secondary, row: row, emote: card.GetColorEmoji());

                count++;
                index++;

                if (count == 6)
                {
                    count = 0;
                    row++;
                }
            }

            // Add the draw card button
            buttons.WithButton("Draw Card", "drawcard", style: ButtonStyle.Secondary, row: row);

            await command.RespondAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Red)
                .WithDescription($"You have {Deck.Count} cards.")
                .Build(),
                ephemeral: true,
                component: buttons.Build());
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
                buttons.WithButton(card.ToString(), $"card{RandomDiscriminator()}-{Game.Host.User.Id}-{card.Color}-{card.Number}-{card.Special}-{index}", style: ButtonStyle.Secondary, row: row, emote: card.GetColorEmoji());

                count++;
                index++;

                if (count == 6)
                {
                    count = 0;
                    row++;
                }
            }

            // Add the draw card button
            buttons.WithButton("Draw Card", "drawcard", style: ButtonStyle.Secondary, row: row);

            if (extraMessage != "")
                extraMessage = $"\n\n**{extraMessage}**";

            if (command == null)
            {
                try
                {
                    await CardMenuMessage.ModifyOriginalResponseAsync(m =>
                    {
                        m.Embed = new EmbedBuilder()
                            .WithColor(Colors.Red)
                            .WithDescription($"You have {Deck.Count} cards.{extraMessage}")
                            .Build();

                        m.Components = buttons.Build();
                    });
                }
                catch (Exception e)
                {
                    // Ignore
                    // They probably dismissed the message or something
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                CardMenuMessage = command;
                await CardMenuMessage.UpdateAsync(m =>
                    {
                        m.Embed = new EmbedBuilder()
                            .WithColor(Colors.Red)
                            .WithDescription($"You have {Deck.Count} cards.{extraMessage}")
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
            if (Game.Players[Game.CurrentPlayerIndex].User.Id != command.User.Id)
            {
                await UpdateCardMenu(command, "It's not your turn");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if this card can be played in the curent game
        /// </summary>
        public async Task<bool> CheckIfCardCanBePlayed(SocketMessageComponent command, Card inputCard)
        {
            // Special cards of the same color can be played
            if ((inputCard.Special != Special.None && inputCard.Color == Game.CurrentCard.Color) ||
            // Cards of the same color can be played
                inputCard.Color == Game.CurrentCard.Color ||
                // Wild Cards
                (inputCard.Special == Special.Wild || inputCard.Special == Special.WildPlusFour) ||
                // Cards of the same number can be played
                inputCard.Number == Game.CurrentCard.Number)
                return true;

            await UpdateCardMenu(command, "That card cannot be played. Please select a different card");

            return false;
        }

        /// <summary>
        /// Play a valid card
        /// </summary>
        public async Task PlayCard(SocketMessageComponent command, Card inputCard, int index)
        {
            // Remove the card from the player's deck
            Deck.RemoveAt(index);

            // Update the deck
            await UpdateCardMenu(command, $"You played a {inputCard.ToString()}");

            // Play the card in the game
            await Game.DoTurn(inputCard);
        }

        /// <summary>
        /// Show the wild card menu
        /// </summary>
        public async Task ShowWildMenu(SocketMessageComponent command, Special special, int index)
        {
            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithDescription("Select a color for this Wild card.")
                    .Build();

                m.Components = new ComponentBuilder()
                    .WithButton("Red", $"wild-Red-{special}-{index}", style: ButtonStyle.Danger)
                    .WithButton("Green", $"wild-Green-{special}-{index}", style: ButtonStyle.Success)
                    .WithButton("Blue", $"wild-Blue-{special}-{index}", style: ButtonStyle.Primary)
                    .WithButton("Yellow", $"wild-Yellow-{special}-{index}", style: ButtonStyle.Secondary, emote: new Emoji("🟨"))
                    .WithButton("Cancel", "cancelwild", style: ButtonStyle.Secondary)
                    .Build();
            });
        }
    }
}