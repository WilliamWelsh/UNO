namespace UNO
{
    public class Whitelisted
    {
        public ulong GuildId { get; set; }
        public List<ulong> ChannelIds { get; set; }

        public Whitelisted()
        {
            ChannelIds = new List<ulong>();
        }
    }
}