namespace UNO.Types
{
    /// <summary>
    /// An object that represents the current player's command and the game they belong to (if any)
    /// </summary>
    public class RetrievedGame
    {
        public Game Game { get; set; }

        public Player Player { get; set; }

        public bool hasValidGameAndPlayer { get; set; }

        public RetrievedGame() { }

        public RetrievedGame(Game game)
        {
            Game = game;
        }

        public void SetPlayer(Player player)
        {
            Player = player;

            // We only get here if we set the Game object already
            hasValidGameAndPlayer = true;
        }
    }
}