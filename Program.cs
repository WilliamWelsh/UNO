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

        private async Task OnLeftGuild(SocketGuild arg) => await UpdateBotStatus();

        private async Task OnJoinedGuild(SocketGuild arg) => await UpdateBotStatus();

        private async Task OnReady()
        {
            await UpdateBotStatus();

            // Check if /uno and /cards are registered
            // and register them if they aren't
            if ((await _client.GetGlobalApplicationCommandsAsync()).Count != 2)
            {
                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("uno")
                    .WithDescription("Host a new game")
                    .Build());

                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                .WithName("cards")
                .WithDescription("View your cards and options")
                .Build());
            }
        }

        private async Task UpdateBotStatus() => await _client.SetGameAsync($"/uno on {_client.Guilds.Count} servers");

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

                        case "cards":
                            await _gameManager.TryToShowCardMenu(slashCommand);
                            break;

                        default:
                            break;
                    }
                    break;

                // Button Commands
                case SocketMessageComponent messageCommand:

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

                    // Card Buttons
                    else if (messageCommand.Data.CustomId.StartsWith("card"))
                        await _gameManager.TryToPlayCard(messageCommand);

                    // Draw Card Button
                    else if (messageCommand.Data.CustomId == "drawcard")
                        await _gameManager.TryToDrawCard(messageCommand);

                    // Cancel Wild Menu Button
                    else if (messageCommand.Data.CustomId == "cancelwild")
                        await _gameManager.TryToCancelWildMenu(messageCommand);

                    // Wild Card Menu Button
                    else if (messageCommand.Data.CustomId.StartsWith("wild-"))
                        await _gameManager.TryToPlayWildCard(messageCommand);

                    // Leave Game Button (during the game)
                    else if (messageCommand.Data.CustomId == "leaveduringgame")
                        await _gameManager.TryToLeaveDuringGame(messageCommand);

                    // End Game Button (during the game)
                    else if (messageCommand.Data.CustomId == "endduringgame")
                        await _gameManager.TryToEndDuringGame(messageCommand);

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