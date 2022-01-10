using System.Text;
using Discord;
using Discord.WebSocket;

namespace UNO.Types
{
    public class Game
    {
        /// <summary>
        /// The Host of the game
        /// </summary>
        public Player Host { get; set; }

        /// <summary>
        /// The list of players (INCLUDING the host)
        /// </summary>
        public List<Player> Players { get; set; }

        /// <summary>
        /// The random object used to generate cards
        /// </summary>
        private Random rnd { get; set; }

        /// <summary>
        /// The channel's ID that the game is in
        /// </summary>
        public ulong ChannelId { get; set; }

        /// <summary>
        /// Has the game started?
        /// </summary>
        public bool hasStarted { get; set; }

        /// <summary>
        /// Whose turn is it?
        /// </summary>
        public int CurrentPlayerIndex { get; set; }

        /// <summary>
        /// What is the current card?
        /// </summary>
        public Card CurrentCard { get; set; }

        /// <summary>
        /// The message that the game is in
        /// </summary>
        public SocketUserMessage GameMessage { get; set; }

        /// <summary>
        /// Timestamp of the last action
        /// </summary>
        public DateTime LastActionTimestamp { get; set; }

        /// <summary>
        /// Some helpful info fo something that recently happened
        /// </summary>
        public string InfoMessage { get; set; }

        /// <summary>
        /// How many cards does the next person have to pick up?
        /// </summary>
        public int StackToPickUp { get; set; }

        public bool isGameOver { get; set; }

        private bool isReversed { get; set; }

        public Game() { }

        public Game(SocketUser host, ulong channelId)
        {
            // Make a new random object
            rnd = new Random();

            // Assign the host
            Host = new Player(host, rnd, this);

            // Initialize player list and add the host
            Players = new List<Player>();
            Players.Add(Host);

            // The host goes first
            CurrentPlayerIndex = 0;

            // First card is random
            CurrentCard = new Card(rnd);

            // We don't want the first card to be a Special
            while (CurrentCard.Special != Special.None)
                CurrentCard = new Card(rnd);

            ChannelId = channelId;

            hasStarted = false;

            InfoMessage = "";

            StackToPickUp = 0;

            isGameOver = false;

            isReversed = false;

            UpdateTimestamp();
        }

        /// <summary>
        /// Has this game been inactive for 10 minutes?
        /// </summary>
        public bool isGameInActive() => (DateTime.Now - LastActionTimestamp).TotalMinutes > 10;

        public void UpdateTimestamp() => LastActionTimestamp = DateTime.Now;

        public string ListPlayers(bool highlightCurrent = false)
        {
            var result = new StringBuilder();

            foreach (var player in Players)
                result.AppendLine($"{(player == Host ? "👑" : "👤")} {player.User.Username} {(player.Deck.Count == 1 ? "**UNO!**" : $"- {player.Deck.Count} cards")}");

            if (highlightCurrent)
                result.Replace(Players[CurrentPlayerIndex].User.Username, $"**{Players[CurrentPlayerIndex].User.Username}**");

            return result.ToString();
        }

        public void SetInfoMessage(string message) => InfoMessage = $"\n\n{message}";

        /// <summary>
        /// Add a new player to this game
        /// </summary>
        public void AddPlayer(SocketUser user) => Players.Add(new Player(user, rnd, this));

        /// <summary>
        /// Start the game
        /// </summary>
        public async Task DoInitialTurn(SocketMessageComponent command)
        {
            UpdateTimestamp();
            var currentPlayer = Players[CurrentPlayerIndex];
            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(CurrentCard.GetDiscordColor())
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"{currentPlayer.User.Username}'s Turn")
                        .WithIconUrl(currentPlayer.User.GetAvatarUrl() ?? currentPlayer.User.GetDefaultAvatarUrl()))
                    .WithDescription($"It's {currentPlayer.User.Username}'s turn.\n\n**Press the `View Cards` button below to view your cards.**{InfoMessage}")
                    .WithThumbnailUrl(CurrentCard.GetImageUrl())
                    .WithFields(new EmbedFieldBuilder[]
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = "Players",
                            Value = ListPlayers(true),
                        }
                    })
                    .Build();

                m.Components = new ComponentBuilder()
                    .WithButton("View Cards", $"showcardprompt", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("Leave Game", $"leaveduringgame", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("End Game", $"endduringgame", row: 0, style: ButtonStyle.Secondary)
                    .Build();
            });
        }

        /// <summary>
        /// Start the game
        /// </summary>
        public async Task DoTurn(Card inputCard)
        {
            UpdateTimestamp();

            await CheckForWinner();
            if (isGameOver)
                return;

            var previousPlayer = Players[CurrentPlayerIndex];

            var lastCard = CurrentCard;

            CurrentCard = inputCard;

            // Increment the turn
            if (CurrentCard.Special == Special.Reverse)
            {
                isReversed = !isReversed;
                IncrementTurn();
            }
            else if (CurrentCard.Special == Special.Skip)
            {
                IncrementTurn();
                IncrementTurn();
            }
            else
                IncrementTurn();

            // Add pickup cards to the stack
            if (CurrentCard.Special == Special.WildPlusTwo)
                StackToPickUp += 2;
            else if (CurrentCard.Special == Special.WildPlusFour)
                StackToPickUp += 4;

            // Check if this player has to pick up cards
            if (StackToPickUp > 0 && (lastCard.Special == Special.WildPlusTwo || lastCard.Special == Special.WildPlusFour) && CurrentCard.Special != Special.WildPlusTwo && CurrentCard.Special != Special.WildPlusFour)
            {
                SetInfoMessage($"{previousPlayer.User.Username} had to pick up {StackToPickUp} cards 😂🤡");
                await previousPlayer.DrawCards(StackToPickUp);
                StackToPickUp = 0;
            }

            var stackText = StackToPickUp > 0 ? $"\n\nPickup Stack: {StackToPickUp}" : "";

            var currentPlayer = Players[CurrentPlayerIndex];

            // Enable the card buttons on the current player
            await currentPlayer.UpdateCardMenu(null);

            await GameMessage.ModifyAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(CurrentCard.GetDiscordColor())
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"{currentPlayer.User.Username}'s Turn")
                        .WithIconUrl(currentPlayer.User.GetAvatarUrl() ?? currentPlayer.User.GetDefaultAvatarUrl()))
                    .WithDescription($"{previousPlayer.User.Username} played a {CurrentCard.ToString()}.{stackText}{InfoMessage}")
                    .WithThumbnailUrl(CurrentCard.GetImageUrl())
                    .WithFields(new EmbedFieldBuilder[]
                    {
                        new EmbedFieldBuilder()
                        {
                            Name = $"Players {(isReversed ? "🔃" : "")}",
                            Value = ListPlayers(true),
                        }
                    })
                    .Build();

                m.Components = new ComponentBuilder()
                    .WithButton("View Cards", $"showcardprompt", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("Leave Game", $"leaveduringgame", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("End Game", $"endduringgame", row: 0, style: ButtonStyle.Secondary)
                    .Build();
            });
        }

        /// <summary>
        /// When someone clicks the "Leave Game" button during the game
        /// </summary>
        public async Task RemovePlayerDuringGame(SocketMessageComponent command)
        {
            var player = Players.First(p => p.User.Id == command.User.Id);

            Players.Remove(player);

            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithDescription("You left the game")
                    .Build();

                m.Components = null;
            });

            SetInfoMessage($"{player.User.Username} left the game");
            await CheckForWinner();
        }

        /// <summary>
        /// Check if anyone has won the game yet
        /// </summary>
        /// <returns>True if someone has won, false otherwise</returns>
        private async Task CheckForWinner()
        {
            Player winner = null;

            if (Players.Count == 1)
            {
                winner = Players[0];
                isGameOver = true;
            }

            else if (Players.Any(p => p.Deck.Count == 0))
            {
                winner = Players.Where(p => p.Deck.Count == 0).First();
                isGameOver = true;
            }

            if (isGameOver)
            {
                await GameMessage.ModifyAsync(m =>
                {
                    m.Embed = new EmbedBuilder()
                        .WithColor(CurrentCard.GetDiscordColor())
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(winner.User.Username)
                            .WithIconUrl(winner.User.GetAvatarUrl() ?? winner.User.GetDefaultAvatarUrl()))
                        .WithDescription($"{winner.User.Username} has won!{InfoMessage}")
                        .WithThumbnailUrl(CurrentCard.GetImageUrl())
                        .Build();

                    m.Components = null;
                });

                foreach (var player in Players)
                    await player.ShowEndGameCardMenu();
            }
        }

        private void IncrementTurn()
        {
            if (isReversed)
            {
                CurrentPlayerIndex--;
                if (CurrentPlayerIndex < 0)
                    CurrentPlayerIndex = Players.Count - 1;
            }
            else
            {
                CurrentPlayerIndex++;
                if (CurrentPlayerIndex >= Players.Count)
                    CurrentPlayerIndex = 0;
            }
        }
    }
}