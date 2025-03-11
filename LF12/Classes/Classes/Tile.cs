using System.Text.Json;
using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    public class Tile
    {
        public int PosX { get; set; }
        public int PosY { get; set; }
        [JsonPropertyName(nameof(Pos))]
        protected Position Pos
        {
            get
            {
                return new Position(PosX, PosY);
            }
        }
    }
}
