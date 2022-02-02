namespace UNO
{
    public static class Config
    {
        /// <summary>
        /// Should the bot connect to the Top.GG API?
        /// </summary>
        public static bool USE_TOP_GG_API = true;

        /// <summary>
        /// Are we in debug mode? (Register commands to guild and use test bot token)
        /// </summary>
        public static bool IS_DEBUG = false;

        /// <summary>
        /// The Guild to register guild commands to when debugging
        /// </summary>
        public static ulong DEBUG_GUILD_ID = 735263201612005472;

        /// <summary>
        /// The ID of the bot
        /// </summary>
        public static ulong BOT_CLIENT_ID = 914696129067757608;

        /// <summary>
        /// It's recommended to have 1 shard per 1500-2000 guilds your bot is in.
        /// </summary>
        public static int DISCORD_SHARD_COUNT = 3;
    }
}