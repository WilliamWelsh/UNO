using Discord;
using Discord.WebSocket;

namespace UNO
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        private GameManager _gameManager;

        public async Task MainAsync()
        {
            _gameManager = new GameManager();

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                GatewayIntents = GatewayIntents.Guilds
            });

            _client.Log += LogAsync;

            await _client.LoginAsync(TokenType.Bot, System.Environment.GetEnvironmentVariable("UnoDiscordBotToken"));
            await _client.StartAsync();

            await _client.SetGameAsync("/uno");

            _client.InteractionCreated += OnInteractionCreated;
            _client.Ready += OnReady;
            _client.JoinedGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeftGuild;

            await Task.Delay(Timeout.Infinite);
        }

        private async Task UpdateBotStatus() => await _client.SetGameAsync($"/uno on {_client.Guilds.Count} servers");

        private async Task OnLeftGuild(SocketGuild arg) => await UpdateBotStatus();

        private async Task OnJoinedGuild(SocketGuild arg) => await UpdateBotStatus();

        private async Task OnReady()
        {
            await UpdateBotStatus();

            // Check if /uno is registered
            // and register it if not
            if ((await _client.GetGlobalApplicationCommandsAsync()).Count != 1)
                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("uno")
                    .WithDescription("Host a new game")
                    .Build());
        }

        private async Task OnInteractionCreated(SocketInteraction interaction)
        {
            switch (interaction)
            {
                // Slash Commands
                case SocketSlashCommand slashCommand:

                    switch (slashCommand.CommandName)
                    {
                        case "uno":
                            await _gameManager.TryToInitializeGame(slashCommand);
                            break;

                        default:
                            break;
                    }
                    break;

                // Button Commands
                case SocketMessageComponent messageCommand:

                    switch (messageCommand.Data.CustomId)
                    {
                        // Initial button on "View Cards" button
                        // I have this extra button to force a reference to the
                        // menu to be created
                        case "showcardmenu":
                            await _gameManager.TryToShowCardMenu(messageCommand);
                            break;

                        case "showcardprompt":
                            await messageCommand.RespondAsync("Please click the button below 😀", component: new ComponentBuilder()
                                .WithButton("Click here to view your cards", "showcardmenu", style: ButtonStyle.Secondary)
                                .Build(), ephemeral: true);
                            break;

                        // Draw Card Button
                        case "drawcard":
                            await _gameManager.TryToDrawCard(messageCommand);
                            break;

                        // Cancel Wild Menu Button
                        case "cancelwild":
                            await _gameManager.TryToCancelWildMenu(messageCommand);
                            break;

                        // Leave Game Button (during the game)
                        case "leaveduringgame":
                            await _gameManager.TryToLeaveDuringGame(messageCommand);
                            break;

                        // // End Game Button (during the game)
                        case "endduringgame":
                            await _gameManager.TryToEndDuringGame(messageCommand);
                            break;

                        default:
                            break;
                    }

                    // Join Game Button
                    if (messageCommand.Data.CustomId.StartsWith("join-"))
                        await _gameManager.TryToJoinGame(messageCommand);

                    // Leave Game Button
                    else if (messageCommand.Data.CustomId.StartsWith("leave-"))
                        await _gameManager.TryToLeaveGame(messageCommand);

                    // Start Game Button
                    else if (messageCommand.Data.CustomId.StartsWith("start-"))
                        await _gameManager.TroToStartGame(messageCommand);

                    // Cancel Game Button
                    else if (messageCommand.Data.CustomId.StartsWith("cancel-"))
                        await _gameManager.TryToCancelGame(messageCommand);

                    // Card Buttons in /cards
                    else if (messageCommand.Data.CustomId.StartsWith("card"))
                        await _gameManager.TryToPlayCard(messageCommand);

                    // Wild Card Menu Button
                    else if (messageCommand.Data.CustomId.StartsWith("wild-"))
                        await _gameManager.TryToPlayWildCard(messageCommand);

                    break;
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}