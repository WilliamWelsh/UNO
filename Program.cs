using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using DiscordBotsList.Api;
using DiscordBotsList.Api.Objects;

namespace UNO
{
    class Program
    {
        static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        private InteractionService _commands;

        private IServiceProvider _services;

        private IDblSelfBot _dblApi;

        public async Task MainAsync()
        {
            _services = ConfigureServices();

            _client = _services.GetRequiredService<DiscordSocketClient>();

            _commands = _services.GetRequiredService<InteractionService>();

            // Initialize the Top.gg (Discord Bots List api)
            if (Config.USE_TOP_GG_API)
            {
                var discordBotList = new AuthDiscordBotListApi(Config.BOT_CLIENT_ID, System.Environment.GetEnvironmentVariable("UnoDblToken"));
                _dblApi = await discordBotList.GetMeAsync();
            }

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            _client.Log += LogAsync;
            _commands.Log += LogAsync;

            await _client.LoginAsync(TokenType.Bot, Config.IS_DEBUG ? System.Environment.GetEnvironmentVariable("TestBotToken") : System.Environment.GetEnvironmentVariable("UnoDiscordBotToken"));
            await _client.StartAsync();

            _client.InteractionCreated += OnInteractionCreated;
            _client.Ready += OnReady;
            _client.JoinedGuild += OnJoinedGuild;
            _client.LeftGuild += OnLeftGuild;

            await Task.Delay(Timeout.Infinite);
        }

        private async Task UpdateBotStatus()
        {
            // Update Bot Status
            await _client.SetGameAsync($"/uno on {_client.Guilds.Count} servers");

            // Update Top.gg Server Count
            if (Config.USE_TOP_GG_API)
                await _dblApi.UpdateStatsAsync(_client.Guilds.Count);
        }

        private async Task OnLeftGuild(SocketGuild arg) => await UpdateBotStatus();

        private async Task OnJoinedGuild(SocketGuild arg) => await UpdateBotStatus();

        private async Task OnReady()
        {
            await UpdateBotStatus();

            // Uncomment this to register commands (should only be run once, not everytime it starts)
            // Comment it out again after registering the commands
            //await _commands.RegisterCommandsGloballyAsync();

            if (Config.IS_DEBUG)
            {
                Console.WriteLine("\n\nWARNING: Running in debug mode.\n\n");
                await _commands.RegisterCommandsToGuildAsync(Config.DEBUG_GUILD_ID);
            }
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