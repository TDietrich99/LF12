using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    public enum ArrowDirection
    {
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4
    }
    public class Arrow
    {
        [JsonPropertyName(nameof(Origin))]
        public CrossGridTile Origin { get; set; }

        [JsonPropertyName(nameof(ArrowTile))]
        public CrossGridTile ArrowTile { get; set; }

        [JsonPropertyName(nameof(Direction))]
        public ArrowDirection Direction { get; set; }
        public Arrow()
        {
        }
    }
}
