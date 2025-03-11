using System.Text.Json;
using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    [JsonDerivedType(typeof(BlankTile), typeDiscriminator: nameof(BlankTile))]
    [JsonDerivedType(typeof(QuestionTile), typeDiscriminator:nameof(QuestionTile))]
    [JsonDerivedType(typeof(ArrowTile), typeDiscriminator: nameof(ArrowTile))]
    public class Tile
    {
        public int PosX { get; set; }
        public int PosY { get; set; }
        [JsonPropertyName(nameof(Pos))]
        public Position Pos
        {
            get
            {
                return new Position(PosX, PosY);
            }
        }
    }
}
