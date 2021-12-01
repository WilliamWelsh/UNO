using Discord;
using Discord.WebSocket;

namespace UNO
{
    public static class EmbedUtils
    {
        /// <summary>
        /// Print a success message
        /// </summary>
        public static async Task PrintSuccess(this SocketInteraction interaction, string message)
        => await interaction.RespondAsync(ephemeral: true, embed: new EmbedBuilder()
                .WithColor(Colors.Green)
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName("Success"))
                .WithDescription(message)
                .Build());

        /// <summary>
        /// Print an error
        /// </summary>
        public static async Task PrintError(this SocketInteraction interaction, string message)
        => await interaction.RespondAsync(ephemeral: true, embed: new EmbedBuilder()
                .WithColor(Colors.Red)
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName("Error"))
                .WithDescription(message)
                .Build());

        public static async Task PrintGeneric(this SocketSlashCommand command, string message) => await command.RespondAsync(embed: new EmbedBuilder()
                .WithColor(Colors.Red)
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName("UNO"))
                .WithDescription(message)
                .Build());
    }
}