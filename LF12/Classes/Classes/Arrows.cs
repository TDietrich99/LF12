using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    public class Arrows
    {
        [JsonPropertyName(nameof(ArrowOrigin))]
        public Position ArrowOrigin { get; set; }
        [JsonPropertyName(nameof(ArrowDirection))]
        public ArrowDirection ArrowDirection { get; set; }
    }
    public enum ArrowDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4
    }
}
