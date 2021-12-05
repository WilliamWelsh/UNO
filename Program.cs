using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace UNO
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        private InteractionService _commands;

        private IServiceProvider _services;

        private GameManager _gameManager;

        public async Task MainAsync()
        {
            _services = ConfigureServices();

            _gameManager = new GameManager();

            _client = _services.GetRequiredService<DiscordSocketClient>();

            _commands = _services.GetRequiredService<InteractionService>();

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.Log += LogAsync;
            _commands.Log += LogAsync;

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

            // Uncomment this to register commands (should only be run once, not everytime it starts)
            //await _commands.RegisterCommandsGloballyAsync();
        }

        private async Task OnInteractionCreated(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of
                // the InteractionModuleBase<T> module
                var ctx = new SocketInteractionContext(_client, interaction);
                await _commands.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>(x => new DiscordSocketClient(new DiscordSocketConfig
                {
                    LogLevel = LogSeverity.Verbose,
                    GatewayIntents = GatewayIntents.Guilds
                }))
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<GameManager>()
                .BuildServiceProvider();
        }
    }
}