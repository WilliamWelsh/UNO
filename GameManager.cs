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
        private async Task<bool> CanWeStartANewgame(SocketInteraction command)
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

                // Check if the game is over
                if (game.isGameOver)
                {
                    ActiveGames.Remove(game);
                    return true;
                }

                await command.PrintError($"There is already an active game in this channel, please wait until that one is finished, or ask the Host, {game.Host.User.Mention}, to end it. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
                return false;
            }

            // Check if they're hosting any games
            if (ActiveGames.Any(g => g.Host.User.Id == command.User.Id))
            {
                var game = ActiveGames.Where(g => g.Host.User.Id == command.User.Id).First();

                // Check if the game is old
                if (game.isGameInActive())
                {
                    ActiveGames.Remove(game);
                    return true;
                }

                // Check if the game is over
                if (game.isGameOver)
                {
                    ActiveGames.Remove(game);
                    return true;
                }

                await command.PrintError("You are already hosting a game. Either finish that game, or close it.\n\nYou can end a game by pressing the \"End Game\" button. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
                return false;
            }

            // Check if they're playing any games
            if (ActiveGames.Any(g => g.Players.Any(p => p.User.Id == command.User.Id)))
            {
                var game = ActiveGames.Where(g => g.Host.User.Id == command.User.Id).First();

                // Check if the game is old
                if (game.isGameInActive())
                {
                    ActiveGames.Remove(game);
                    return true;
                }

                // Check if the game is over
                if (game.isGameOver)
                {
                    ActiveGames.Remove(game);
                    return true;
                }

                await command.PrintError("You are already playing a game. Either finish that game, or leave it.\n\nYou can leave a game by pressing the \"Leave Game\" button on. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Initialize a new game
        /// </summary>
        public async Task TryToInitializeGame(SocketInteraction command)
        {
            // Check if they are able to host a new game
            if (!(await CanWeStartANewgame(command)))
                return;

            var game = new Types.Game(command.User, command.Channel.Id);

            await command.RespondAsync("Started a new game", embed: new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName("UNO"))
                    .WithDescription($"{game.Host.User.Username} has started a game of UNO! Click the button below to join!\n\n{game.ListPlayers(listCardCount: false)}")
                    .Build(),
                components: new ComponentBuilder()
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
        public async Task TryToJoinGame(SocketMessageComponent command, ulong hostId)
        {
            // Check if that game is still valid
            if (!ActiveGames.Any(g => g.Host.User.Id == hostId))
            {
                await command.PrintError("This game does not exist. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
                return;
            }

            // Get the game
            var game = ActiveGames.Where(g => g.Host.User.Id == hostId).First();

            // Check if this game has started already
            if (game.hasStarted)
            {
                await command.PrintError("This game started already. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
                return;
            }

            // Check if the user is already in the game
            else if (game.Players.Any(p => p.User.Id == command.User.Id))
            {
                await command.PrintError("You are already in this game. Click the \'Leave Game\" button if you want to leave it. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
                return;
            }

            // Check if the user trying to join is the host
            else if (game.Host.User.Id == command.User.Id)
            {
                await command.PrintError("You cannot join your own game. 😂");
                return;
            }

            // Check if the game already has 4 players
            else if (game.Players.Count >= game.MaxPlayers)
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
                    .WithDescription($"{game.Host.User.Username} has started a game of UNO! Click the button below to join!\n\n{game.ListPlayers(listCardCount: false)}\n\n*{command.User.Username} just joined*")
                    .Build();

                m.Components = new ComponentBuilder()
                    .WithButton("Start Game", $"start-{game.Host.User.Id}", row: 0, style: ButtonStyle.Secondary, disabled: game.Players.Count == 0)
                    .WithButton("Cancel Game", $"cancel-{game.Host.User.Id}", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("Join Game", $"join-{game.Host.User.Id}", row: 1, style: ButtonStyle.Secondary, disabled: game.Players.Count >= game.MaxPlayers)
                    .WithButton("Leave Game", $"leave-{game.Host.User.Id}", row: 1, style: ButtonStyle.Secondary)
                    .Build();
            });

            game.UpdateTimestamp();
        }

        /// <summary>
        /// Try to leave a game
        /// </summary>
        public async Task TryToLeaveGame(SocketMessageComponent command, ulong hostId)
        {
            // Check if that game is still valid
            if (!ActiveGames.Any(g => g.Host.User.Id == hostId))
            {
                await command.PrintError("This game does not exist. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
                return;
            }

            // Get the game
            var game = ActiveGames.Where(g => g.Host.User.Id == hostId).First();

            // Check if the user is the host
            if (game.Host.User.Id == command.User.Id)
            {
                await command.PrintError("You're the host. If you want to leave, use the \"Cancel Game\" button. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
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
                    .WithDescription($"{game.Host.User.Username} has started a game of UNO! Click the button below to join!\n\n{game.ListPlayers(listCardCount: false)}\n\n*{command.User.Username} just left*")
                    .Build();

                m.Components = new ComponentBuilder()
                    .WithButton("Start Game", $"start-{game.Host.User.Id}", row: 0, style: ButtonStyle.Secondary, disabled: game.Players.Count == 0)
                    .WithButton("Cancel Game", $"cancel-{game.Host.User.Id}", row: 0, style: ButtonStyle.Secondary)
                    .WithButton("Join Game", $"join-{game.Host.User.Id}", row: 1, style: ButtonStyle.Secondary, disabled: game.Players.Count >= game.MaxPlayers)
                    .WithButton("Leave Game", $"leave-{game.Host.User.Id}", row: 1, style: ButtonStyle.Secondary)
                    .Build();
            });

            game.UpdateTimestamp();
        }

        /// <summary>
        /// Cancel the game creation
        /// </summary>
        public async Task TryToCancelGame(SocketMessageComponent command, ulong hostId)
        {
            var canCancel = true;

            // Check if that game is still valid
            if (!ActiveGames.Any(g => g.Host.User.Id == hostId))
            {
                await command.PrintError("This game does not exist. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
                canCancel = false;
            }

            // Get the game
            var game = ActiveGames.Where(g => g.Host.User.Id == hostId).First();

            if (game.Host.User.Id != command.User.Id)
            {
                await command.PrintError("You're not the host. If you want to leave, use the \"Leave Game\" button. If there is an issue, an admin can use `/admin` commands to reset or respawn the game.");
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
        public async Task TryToStartGame(SocketMessageComponent command, ulong hostId)
        {
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
        public async Task TryToPlayCard(SocketMessageComponent command, ulong hostId, string color, string number, string special, int index)
        {
            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            var inputCard = new Types.Card(color, number, special);

            // Check if it's this player's turn
            if (!await retrievedGame.Player.CheckIfItsMyTurn(command))
                return;

            // Check if this card be played
            if (!retrievedGame.Player.CheckIfCardCanBePlayed(inputCard))
            {
                await retrievedGame.Player.UpdateCardMenu(command, "That card cannot be played. Please select a different card");
                return;
            }

            // If it's a Wild card, then show the menu to select a color
            if (inputCard.Special == Types.Special.Wild || inputCard.Special == Types.Special.WildPlusFour)
            {
                // Show the wild card menu
                await retrievedGame.Player.ShowWildMenu(command, (Types.Special)Enum.Parse(typeof(Types.Special), special), index);
                return;
            }

            // Play the card
            await retrievedGame.Player.PlayCard(command, inputCard, index);
        }

        /// <summary>
        /// Try to draw a card
        /// </summary>
        public async Task TryToDrawCard(SocketMessageComponent command)
        {
            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            // Check if it's this player's turn
            if (!await retrievedGame.Player.CheckIfItsMyTurn(command))
                return;

            // Have them draw a card
            await retrievedGame.Player.DrawCard(command);
        }

        /// <summary>
        /// Try to play a wild card
        /// </summary>
        public async Task TryToPlayWildCard(SocketMessageComponent command, string color, string special, int index)
        {
            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            // Check if it's this player's turn
            if (!await retrievedGame.Player.CheckIfItsMyTurn(command))
                return;

            var args = command.Data.CustomId.Split("-");

            var inputCard = new Types.Card(args[1], "", args[2]);

            // Check if this card be played
            if (!retrievedGame.Player.CheckIfCardCanBePlayed(inputCard))
            {
                await retrievedGame.Player.UpdateCardMenu(command, "That card cannot be played. Please select a different card");
                return;
            }

            // Play the card
            await retrievedGame.Player.PlayCard(command, inputCard, Convert.ToInt32(args[3]));
        }

        /// <summary>
        /// Try to show a wild card menu
        /// </summary>
        public async Task TryToCancelWildMenu(SocketMessageComponent command)
        {
            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            // Check if it's this player's turn
            if (!await retrievedGame.Player.CheckIfItsMyTurn(command))
                return;

            // Show their regular cards
            await retrievedGame.Player.UpdateCardMenu(command);
        }

        /// <summary>
        /// "Leave Game" button (during the game)
        /// </summary>
        public async Task TryToLeaveDuringGame(SocketMessageComponent command)
        {
            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            // Remove the player
            await retrievedGame.Game.RemovePlayerDuringGame(command);

            if (retrievedGame.Game.isGameOver)
                ActiveGames.Remove(retrievedGame.Game);
        }

        /// <summary>
        /// "End Game" button (during the game)
        /// </summary>
        public async Task TryToEndDuringGame(SocketMessageComponent command)
        {
            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            // Check if they're host
            if (retrievedGame.Game.Host.User.Id != retrievedGame.Player.User.Id)
            {
                await command.PrintError($"Only the host ({retrievedGame.Game.Host.User.Username}) can end the game.");
                return;
            }

            // End the game
            await command.UpdateAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"UNO"))
                    .WithDescription($"{retrievedGame.Game.Host.User.Username} has ended the game.\n\nIf you want to start a new game in this channel, do `/uno`")
                    .Build();

                m.Components = null;
            });

            ActiveGames.Remove(retrievedGame.Game);
        }

        /// <summary>
        /// Try to show a card menu
        /// </summary>
        public async Task TryToShowCardMenu(SocketMessageComponent command)
        {
            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            // Show their regular cards
            await retrievedGame.Player.UpdateCardMenu(command);
        }

        /// <summary>
        /// /admin reset
        /// </summary>
        public async Task TryToResetGame(SocketSlashCommand command)
        {
            // Has to be an admin
            if (!((SocketGuildUser)command.User).GuildPermissions.Administrator)
            {
                await command.PrintError("You must have the Administrator permission to use this command.");
                return;
            }

            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            // Update the game message
            await retrievedGame.Game.GameMessage.ModifyAsync(m =>
            {
                m.Embed = new EmbedBuilder()
                    .WithColor(Colors.Red)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"UNO"))
                    .WithDescription($"Game was manually reset by {command.User.Username}.")
                    .Build();

                m.Components = null;
            });

            // Delete the game
            ActiveGames.Remove(retrievedGame.Game);
            foreach (var player in retrievedGame.Game.Players)
                await player.RemoveAllPlayerCardMenusWithMessage($"{command.User.Username} has manually reset the game in this channel.\n\nIf you want to start a new game in this channel, do `/uno`");

            // Respond to the interaction
            await command.RespondAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Red)
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"UNO"))
                .WithDescription($"{command.User.Username} has manually reset the game in this channel.\n\nIf you want to start a new game in this channel, do `/uno`")
                .Build());

            ActiveGames.Remove(retrievedGame.Game);
        }

        /// <summary>
        /// "UNO!" button
        /// </summary>
        public async Task TryToSayUno(SocketMessageComponent command)
        {
            // Try to find a valid game in this channel with this suer
            var retrievedGame = await command.TryToFindGameInThisChannelWithUser(ActiveGames);

            if (!retrievedGame.hasValidGameAndPlayer)
                return;

            // See if anyone has two cards
            if (!retrievedGame.Game.Players.Any(p => p.CanSomeoneSayUno))
            {
                await command.PrintError("You were too late! 🐢🐢");
                return;
            }

            // Someone said UNO! successfully, who was it?
            var playerWithOneCard = retrievedGame.Game.Players.Where(p => p.CanSomeoneSayUno).First();

            // WOO! They're safe 😎
            if (playerWithOneCard.User.Id == command.User.Id)
            {
                playerWithOneCard.CanSomeoneSayUno = false;
                await retrievedGame.Game.UpdateInfoMessage($"{command.User.Username} said UNO before anyone else did and didn't have to pick up any cards.", true);
                await command.PrintSuccess("You said UNO before anyone else did, so you don't have to pick up any cards 😎. Congrats ⚡");
                return;
            }

            // Uh oh... someone has to pick up 2 cards.. 🤡🤡
            await playerWithOneCard.DrawCards(2);

            await retrievedGame.Game.UpdateInfoMessage($"{command.User.Username} said UNO before {playerWithOneCard.User.Username} did so they had to pick up 2 cards 😂", true);

            await command.PrintSuccess($"You said UNO so {playerWithOneCard.User.Username} had to pick up 2 cards. Congrats ⚡");
        }
    }
}