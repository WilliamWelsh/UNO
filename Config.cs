namespace UNO
{
    public static class Config
    {
        /// <summary>
        /// Should the bot connect to the Top.GG API?
        /// </summary>
        public static bool USE_TOP_GG_API = false;

        /// <summary>
        /// Are we in debug mode? (Register commands to guild and use test bot token)
        /// </summary>
        public static bool IS_DEBUG = false;

        /// <summary>
        /// The Guild to register guild commands to when debugging
        /// </summary>
        public static ulong DEBUG_GUILD_ID = 735263201612005472;
    }
}