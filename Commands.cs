using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace UNO
{
    public class Commands : InteractionModuleBase<SocketInteractionContext>
    {
        public GameManager GameManager { get; set; }

        // /uno
        [SlashCommand("uno", "Host a new game")]
        public async Task UnoCommand() => await GameManager.TryToInitializeGame(Context.Interaction);

        // "View Cards" button
        [ComponentInteraction("showcardmenu")]
        public async Task TryToShowCardMenu() => await GameManager.TryToShowCardMenu((SocketMessageComponent)Context.Interaction);

        // "Click here to view your cards" button
        [ComponentInteraction("showcardprompt")]
        public async Task ShowCardPromptCommand() => await Context.Interaction.RespondAsync("Please click the button below 😀",
            components: new ComponentBuilder()
            .WithButton("Click here to view your cards", "showcardmenu", style: ButtonStyle.Secondary)
            .Build(), ephemeral: true);

        // "Draw Card" button
        [ComponentInteraction("drawcard")]
        public async Task TryToDrawCard() => await GameManager.TryToDrawCard((SocketMessageComponent)Context.Interaction);

        // "Cancel" button on Wild card color section
        [ComponentInteraction("cancelwild")]
        public async Task TryToCancelWildMenu() => await GameManager.TryToCancelWildMenu((SocketMessageComponent)Context.Interaction);

        // "Leave Game" button
        [ComponentInteraction("leaveduringgame")]
        public async Task TryToLeaveDuringGame() => await GameManager.TryToLeaveDuringGame((SocketMessageComponent)Context.Interaction);

        // "End Game" button
        [ComponentInteraction("endduringgame")]
        public async Task TryToEndDuringGame() => await GameManager.TryToEndDuringGame((SocketMessageComponent)Context.Interaction);

        // "Join Game" button
        [ComponentInteraction("join-*")]
        public async Task TryToJoinGame(string hostId) => await GameManager.TryToJoinGame((SocketMessageComponent)Context.Interaction, Convert.ToUInt64(hostId));

        // "Leave Game" button
        [ComponentInteraction("leave-*")]
        public async Task TryToLeaveGame(string hostId) => await GameManager.TryToLeaveGame((SocketMessageComponent)Context.Interaction, Convert.ToUInt64(hostId));

        // "Start Game" button
        [ComponentInteraction("start-*")]
        public async Task TroToStartGame(string hostId) => await GameManager.TryToStartGame((SocketMessageComponent)Context.Interaction, Convert.ToUInt64(hostId));

        // "Cancel Game" button
        [ComponentInteraction("cancel-*")]
        public async Task TryToCancelGame(string hostId) => await GameManager.TryToCancelGame((SocketMessageComponent)Context.Interaction, Convert.ToUInt64(hostId));

        // Card button on card menu ("Red 1", "Blue +2", etc)
        [ComponentInteraction("card*-*-*-*-*-*")]
        public async Task TryToPlayCard(string discriminator, string hostId, string color, string number, string special, string index) => await GameManager.TryToPlayCard((SocketMessageComponent)Context.Interaction, Convert.ToUInt64(hostId), color, number, special, Convert.ToInt32(index));

        // Wild card button on card menu (Wild, Wild+4)
        [ComponentInteraction("wild-*-*-*")]
        public async Task TryToPlayWildCard(string color, string special, string index) => await GameManager.TryToPlayWildCard((SocketMessageComponent)Context.Interaction, color, special, Convert.ToInt32(index));

        // "UNO!" button
        [ComponentInteraction("sayuno")]
        public async Task TryToSayUno() => await GameManager.TryToSayUno((SocketMessageComponent)Context.Interaction);

        [Group("admin", "UNO admin commands")]
        public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
        {
            public GameManager GameManager { get; set; }

            // /admin reset
            [SlashCommand("reset", "Reset/delete the game in this channel")]
            public async Task TryToResetGame() => await GameManager.TryToResetGame((SocketSlashCommand)Context.Interaction);
        }
    }
}