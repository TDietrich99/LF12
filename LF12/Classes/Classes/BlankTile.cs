using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    [JsonDerivedType(typeof(ArrowTile), typeDiscriminator: nameof(ArrowTile))]
    public class BlankTile:Tile
    {
        [JsonPropertyName(nameof(Letter))]
        public char? Letter { get; set; }
    }
}
