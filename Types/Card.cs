using Discord;

namespace UNO.Types
{
    public class Card
    {
        public Color Color { get; set; }

        public Special Special { get; set; }

        public string Number { get; set; }

        public Card(Random rnd)
        {
            // Assign a random color
            var colors = Enum.GetValues(typeof(Color));
            Color = (Color)colors.GetValue(rnd.Next(colors.Length - 1)); // Minus 1 because we don't want to randomly assign the "None" color

            // Assign a random number
            Number = rnd.Next(0, 15).ToString();

            // Numbers 10-14 are assigned for Special cards
            switch (Number)
            {
                case "10":
                    AssignSpecial(Special.Reverse);
                    break;

                case "11":
                    AssignSpecial(Special.Skip);
                    break;

                case "12":
                    Color = Color.None;
                    AssignSpecial(Special.Wild);
                    break;

                case "13":
                    AssignSpecial(Special.WildPlusTwo);
                    break;

                case "14":
                    Color = Color.None;
                    AssignSpecial(Special.WildPlusFour);
                    break;

                default:
                    break;
            }
        }

        public Card(string color, string number, string special)
        {
            Color = (Color)Enum.Parse(typeof(Color), color);
            Number = number;
            Special = (Special)Enum.Parse(typeof(Special), special);
        }

        public void AssignSpecial(Special special)
        {
            Special = special;
            Number = "";
        }

        /// <summary>
        /// Get the DISCORD color of this card
        /// </summary>
        public Discord.Color GetDiscordColor()
        {
            switch (Color)
            {
                case Color.Red:
                    return Colors.UnoRed;

                case Color.Green:
                    return Colors.UnoGreen;

                case Color.Blue:
                    return Colors.UnoBlue;

                case Color.Yellow:
                    return Colors.UnoYellow;

                default:
                    return Colors.Black;
            }
        }

        /// <summary>
        /// Get the image Url of this card
        /// </summary>
        public string GetImageUrl() => $"https://raw.githubusercontent.com/WilliamWelsh/UNO/main/images/{Color}{Number}{(Special == Special.None ? "" : Special)}.png";

        public override string ToString() => $"{Color.ToString().Replace("None", "")} {Number}{(Special == Special.None ? "" : Special.ToString().Replace("Plus", "+").Replace("Four", "4").Replace("Wild+Two", "+2"))} {GetSpecialEmoji()}";

        /// <summary>
        /// Convert the color to an emoji
        /// </summary>
        public Emoji GetColorEmoji()
        {
            switch (Color)
            {
                case Color.Red:
                    return new Emoji("🟥");

                case Color.Green:
                    return new Emoji("🟩");

                case Color.Blue:
                    return new Emoji("🟦");

                case Color.Yellow:
                    return new Emoji("🟨");

                default:
                    return new Emoji("🎨");

            }
        }

        /// <summary>
        /// Convert the Special type to an emoji
        /// </summary>
        public string GetSpecialEmoji()
        {
            switch (Special)
            {
                case Special.Reverse:
                    return "🔃";

                case Special.Skip:
                    return "🚫";
            }

            return "";
        }
    }
}