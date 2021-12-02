using Discord;
using Discord.WebSocket;

namespace UNO
{
    public class GameManager
    {
        public List<Types.Game> ActiveGames;

        public GameManager() => ActiveGames = new List<Types.Game>();

        /// <summary>
        /// Check if the user is already playing a game, or if they are hosting one already, or if there isn't one in this channel
        /// </summary>
        public async Task<bool> CanWeStartANewgame(SocketSlashCommand command)
        {
            // Check if there isn't an active game in this channel already
            if (ActiveGames.Any(g => g.ChannelId == command.Channel.Id))
            {
                var game = ActiveGames.Where(g => g.ChannelId == command.Channel.Id).First();

                // Check if the game is old
                if (game.isGameInActive())
                {
                    ActiveGames.Remove(game);
                    return true;
                }

                Console.WriteLine(game.isGameOver);

                // Check if the game is over
                if (game.isGameOver)
                {
                    ActiveGames.Remove(game);
                    return true;
                }

                await command.PrintError($"There is already an active game in this channel, please wait until that one is finished, or ask the Host, {game.Host.User.Mention}, to end it.");
                return false;
            }

            // Check if they're hosting any games
            if (ActiveGames.Any(g => g.Host.User.Id == command.User.Id))
            {
                await command.PrintError("You are already hosting a game. Either finish that game, or close it.\n\nYou can end a game by pressing the \"End Game\" button.");
                return false;
            }

            // Check if they're playing any games
            if (ActiveGames.Any(g => g.Players.Any(p => p.User.Id == command.User.Id)))
            {
                await command.PrintError("You are already playing a game. Either finish that game, or leave it.\n\nYou can leave a game by pressing the \"Leave Game\" button on.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialize a new game
        /// </summary>
        public async Task TryToInitializeGame(SocketSlashCommand command)
        {
            // Check if they are able to host a new game
            if (!(await CanWeStartANewgame(command)))
                return;

            var game = new Types.Game(command.User, command.Channel.Id);

            await command.RespondAsync(embed: new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName("UNO"))
                    .WithDescription($"{game.Host.User.Username} has started a game of UNO! Click the button below to join!\n\n{game.ListPlayers()}")
                    .Build(),
                component: new ComponentBuilder()
                    .WithButton("Start Game", $"start-{command.User.Id}", row: 0, style: ButtonStyle.Secondary, disabled: true)
                    .WithButton("Cancel Game", $"cancel-{command.User.Id}", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("Join Game", $"join-{command.User.Id}", row: 1, style: ButtonStyle.Secondary)
                    .WithButton("Leave Game", $"leave-{command.User.Id}", row: 1, style: ButtonStyle.Secondary)
                    .Build());

            ActiveGames.Add(game);
        }

        /// <summary>
        /// Try to join a game
        /// </summary>
        public async Task TryToJoinGame(SocketMessageComponent command)
        {
            // Get the Hot's id
            var hostId = Convert.ToUInt64(command.Data.CustomId.Split('-')[1]);

            // Check if that game is still valid
            if (!ActiveGames.Any(g => g.Host.User.Id == hostId))
            {
                await command.PrintError("This game does not exist.");
                return;
            }

            // Get the game
            var game = ActiveGames.Where(g => g.Host.User.Id == hostId).First();

            // Check if this game has started already
            if (game.hasStarted)
            {
                await command.PrintError("This game started already.");
                return;
            }

            // Check if the user is already in the game
            else if (game.Players.Any(p => p.User.Id == command.User.Id))
            {
                await command.PrintError("You are already in this game. Click the \'Leave Game\" button if you want to leave it.");
                return;
            }

            // Check if the user trying to join is the host
            else if (game.Host.User.Id == command.User.Id)
            {
                await command.PrintError("You cannot join your own game. 😂");
                return;
            }

            // Check if the game already has 4 players
            else if (game.Players.Count >= 4)
            {
                await command.PrintError("This game is full.");
                return;
            }

            game.AddPlayer(command.User);

            // Update the player list
            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName("UNO"))
                    .WithDescription($"{game.Host.User.Username} has started a game of UNO! Click the button below to join!\n\n{game.ListPlayers()}\n\n*{command.User.Username} just joined*")
                    .Build();

                m.Components = new ComponentBuilder()
                    .WithButton("Start Game", $"start-{game.Host.User.Id}", row: 0, style: ButtonStyle.Secondary, disabled: game.Players.Count == 0)
                    .WithButton("Cancel Game", $"cancel-{game.Host.User.Id}", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("Join Game", $"join-{game.Host.User.Id}", row: 1, style: ButtonStyle.Secondary, disabled: game.Players.Count >= 3)
                    .WithButton("Leave Game", $"leave-{game.Host.User.Id}", row: 1, style: ButtonStyle.Secondary)
                    .Build();
            });

            game.UpdateTimestamp();
        }

        /// <summary>
        /// Try to leave a game
        /// </summary>
        public async Task TryToLeaveGame(SocketMessageComponent command)
        {
            // Get the Hot's id
            var hostId = Convert.ToUInt64(command.Data.CustomId.Split('-')[1]);

            // Check if that game is still valid
            if (!ActiveGames.Any(g => g.Host.User.Id == hostId))
            {
                await command.PrintError("This game does not exist.");
                return;
            }

            // Get the game
            var game = ActiveGames.Where(g => g.Host.User.Id == hostId).First();

            // Check if the user is the host
            if (game.Host.User.Id == command.User.Id)
            {
                await command.PrintError("You're the host. If you want to leave, use the \"Cancel Game\" button.");
                return;
            }

            // Check if the user is actually in the game
            else if (!game.Players.Any(p => p.User.Id == command.User.Id))
            {
                await command.PrintError("You're not in this game. 😂");
                return;
            }

            // Remove the player
            game.Players.Remove(game.Players.Where(p => p.User.Id == command.User.Id).First());

            // Update the player list
            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName("UNO"))
                    .WithDescription($"{game.Host.User.Username} has started a game of UNO! Click the button below to join!\n\n{game.ListPlayers()}\n\n*{command.User.Username} just left*")
                    .Build();

                m.Components = new ComponentBuilder()
                    .WithButton("Start Game", $"start-{game.Host.User.Id}", row: 0, style: ButtonStyle.Secondary, disabled: game.Players.Count == 0)
                    .WithButton("Cancel Game", $"cancel-{game.Host.User.Id}", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("Join Game", $"join-{game.Host.User.Id}", row: 1, style: ButtonStyle.Secondary, disabled: game.Players.Count >= 4)
                    .WithButton("Leave Game", $"leave-{game.Host.User.Id}", row: 1, style: ButtonStyle.Secondary)
                    .Build();
            });

            game.UpdateTimestamp();
        }

        /// <summary>
        /// Cancel the game creation
        /// </summary>
        public async Task TryToCancelGame(SocketMessageComponent command)
        {
            var canCancel = true;

            // Get the Hot's id
            var hostId = Convert.ToUInt64(command.Data.CustomId.Split('-')[1]);

            // Check if that game is still valid
            if (!ActiveGames.Any(g => g.Host.User.Id == hostId))
            {
                await command.PrintError("This game does not exist.");
                canCancel = false;
            }

            // Get the game
            var game = ActiveGames.Where(g => g.Host.User.Id == hostId).First();

            if (game.Host.User.Id != command.User.Id)
            {
                await command.PrintError("You're not the host. If you want to leave, use the \"Leave Game\" button.");
                canCancel = false;
            }

            if (!canCancel)
                return;

            // Update the player list
            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName("UNO"))
                    .WithDescription($"{game.Host.User.Username} has cancelled the game.\n\nIf you want to start a new game in this channel, do `/uno`")
                    .Build();

                m.Components = null;
            });

            // Remove the game
            ActiveGames.Remove(ActiveGames.Where(g => g.Host.User.Id == command.User.Id).First());
        }

        /// <summary>
        /// Start the game
        /// </summary>
        public async Task TroToStartGame(SocketMessageComponent command)
        {
            // Get the Host's id
            var hostId = Convert.ToUInt64(command.Data.CustomId.Split('-')[1]);

            // Check if that game is still valid
            if (!ActiveGames.Any(g => g.Host.User.Id == hostId))
            {
                await command.PrintError("This game does not exist.");
                return;
            }

            // Get the game
            var game = ActiveGames.Where(g => g.Host.User.Id == hostId).First();

            if (game.Host.User.Id != command.User.Id)
            {
                await command.PrintError("Only the host can start the game");
                return;
            }

            game.hasStarted = true;

            game.GameMessage = command.Message;

            await game.DoInitialTurn(command);
        }

        /// <summary>
        /// Try to play a card during the game
        /// </summary>
        public async Task TryToPlayCard(SocketMessageComponent command)
        {
            // Check if there's a game in this channel
            if (!await command.CheckForGameInThisChannel(ActiveGames))
                return;

            // Get the game object
            var game = command.GetGameInThisChannel(ActiveGames);

            // Check if player is in the game
            if (!await game.CheckIfPlayerIsInThisGame(command))
                return;

            // Check if the game has started yet
            if (!await game.CheckIfGameHasStarted(command))
                return;

            // Get the player
            var player = game.GetPlayerFromCommand(command);

            // Get the data from the customId
            var args = command.Data.CustomId.Split("-");
            var hostId = args[1];

            var inputCard = new Types.Card(args[2], args[3], args[4]);

            // Check if it's this player's turn
            if (!await player.CheckIfItsMyTurn(command))
                return;

            // Check if this card be played
            if (!player.CheckIfCardCanBePlayed(inputCard))
            {
                await player.UpdateCardMenu(command, "That card cannot be played. Please select a different card");
                return;
            }

            // If it's a Wild card, then show the menu to select a color
            if (inputCard.Special == Types.Special.Wild || inputCard.Special == Types.Special.WildPlusFour)
            {
                // Show the wild card menu
                await player.ShowWildMenu(command, (Types.Special)Enum.Parse(typeof(Types.Special), args[4]), Convert.ToInt32(args[5]));
                return;
            }

            // Play the card
            await player.PlayCard(command, inputCard, Convert.ToInt32(args[5]));
        }

        /// <summary>
        /// Try to draw a card
        /// </summary>
        public async Task TryToDrawCard(SocketMessageComponent command)
        {
            // Check if there's a game in this channel
            if (!await command.CheckForGameInThisChannel(ActiveGames))
                return;

            // Get the game object
            var game = command.GetGameInThisChannel(ActiveGames);

            // Check if player is in the game
            if (!await game.CheckIfPlayerIsInThisGame(command))
                return;

            // Check if the game has started yet
            if (!await game.CheckIfGameHasStarted(command))
                return;

            // Get the player
            var player = game.GetPlayerFromCommand(command);

            // Check if it's this player's turn
            if (!await player.CheckIfItsMyTurn(command))
                return;

            // Have them draw a card
            await player.DrawCard(command);
        }

        /// <summary>
        /// Try to play a wild card
        /// </summary>
        public async Task TryToPlayWildCard(SocketMessageComponent command)
        {
            // Check if there's a game in this channel
            if (!await command.CheckForGameInThisChannel(ActiveGames))
                return;

            // Get the game
            var game = command.GetGameInThisChannel(ActiveGames);

            // Check if player is in the game
            if (!await game.CheckIfPlayerIsInThisGame(command))
                return;

            // Check if the game has started yet
            if (!await game.CheckIfGameHasStarted(command))
                return;

            // Get the player
            var player = game.GetPlayerFromCommand(command);

            // Check if it's this player's turn
            if (!await player.CheckIfItsMyTurn(command))
                return;

            var args = command.Data.CustomId.Split("-");

            var inputCard = new Types.Card(args[1], "", args[2]);
            Console.WriteLine(inputCard.Special);

            // Check if this card be played
            if (!player.CheckIfCardCanBePlayed(inputCard))
            {
                await player.UpdateCardMenu(command, "That card cannot be played. Please select a different card");
                return;
            }

            // Play the card
            await player.PlayCard(command, inputCard, Convert.ToInt32(args[3]));
        }

        /// <summary>
        /// Try to show a wild card menu
        /// </summary>
        public async Task TryToCancelWildMenu(SocketMessageComponent command)
        {
            // Check if there's a game in this channel
            if (!await command.CheckForGameInThisChannel(ActiveGames))
                return;

            // Get the game object
            var game = command.GetGameInThisChannel(ActiveGames);

            // Check if player is in the game
            if (!await game.CheckIfPlayerIsInThisGame(command))
                return;

            // Check if the game has started yet
            if (!await game.CheckIfGameHasStarted(command))
                return;

            // Get the player
            var player = game.GetPlayerFromCommand(command);

            // Check if it's this player's turn
            if (!await player.CheckIfItsMyTurn(command))
                return;

            // Show their regular cards
            await player.UpdateCardMenu(command);
        }

        /// <summary>
        /// "Leave Game" button (during the game)
        /// </summary>
        public async Task TryToLeaveDuringGame(SocketMessageComponent command)
        {
            // Check if there's a game in this channel
            if (!await command.CheckForGameInThisChannel(ActiveGames))
                return;

            // Get the game object
            var game = command.GetGameInThisChannel(ActiveGames);

            // Check if player is in the game
            if (!await game.CheckIfPlayerIsInThisGame(command))
                return;

            // Remove the player
            await game.RemovePlayerDuringGame(command);

            if (game.isGameOver)
                ActiveGames.Remove(game);
        }

        /// <summary>
        /// "End Game" button (during the game)
        /// </summary>
        public async Task TryToEndDuringGame(SocketMessageComponent command)
        {
            // Check if there's a game in this channel
            if (!await command.CheckForGameInThisChannel(ActiveGames))
                return;

            // Get the game object
            var game = command.GetGameInThisChannel(ActiveGames);

            // Check if player is in the game
            if (!await game.CheckIfPlayerIsInThisGame(command))
                return;

            // Get the player
            var player = game.GetPlayerFromCommand(command);

            // Check if they're host
            if (game.Host.User.Id != player.User.Id)
            {
                await command.PrintError($"Only the host ({game.Host.User.Username}) can end the game.");
                return;
            }

            // End the game
            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"UNO"))
                    .WithDescription($"{game.Host.User.Username} has ended the game.\n\nIf you want to start a new game in this channel, do `/uno`")
                    .Build();

                m.Components = null;
            });

            ActiveGames.Remove(game);
        }

        /// <summary>
        /// Try to show a card menu
        /// </summary>
        public async Task TryToShowCardMenu(SocketMessageComponent command)
        {
            // Check if there's a game in this channel
            if (!await command.CheckForGameInThisChannel(ActiveGames))
                return;

            // Get the game object
            var game = command.GetGameInThisChannel(ActiveGames);

            // Check if player is in the game
            if (!await game.CheckIfPlayerIsInThisGame(command))
                return;

            // Check if the game has started yet
            if (!await game.CheckIfGameHasStarted(command))
                return;

            // Show their regular cards
            await game.GetPlayerFromCommand(command).UpdateCardMenu(command);
        }
    }
}