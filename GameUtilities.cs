using Discord.WebSocket;
using UNO.Types;

namespace UNO
{
    public static class GameUtilities
    {
        /// <summary>
        /// Try to find a game in this channel
        /// </summary>
        /// <returns>Returns true if there's a game in this channel</returns>
        public static async Task<bool> CheckForGameInThisChannel(this SocketInteraction interaction, List<Game> activeGames)
        {
            if (!activeGames.Any(g => g.ChannelId == interaction.Channel.Id))
            {
                await interaction.PrintError("There is no game is this channel.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get the game object for this channel
        /// </summary>
        public static Game GetGameInThisChannel(this SocketInteraction interaction, List<Game> activeGames) => activeGames.Where(g => g.ChannelId == interaction.Channel.Id).First();
    }
}