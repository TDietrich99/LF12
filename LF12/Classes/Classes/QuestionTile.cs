using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    public class QuestionTile : Tile
    {
        [JsonPropertyName(nameof(Question))]
        public string Question { get; set; }
        [JsonPropertyName(nameof(ArrowTile))]
        public Position ArrowTile { get; set; }
        public short Length { get; set; }
        public void SetQuestionLength(List<Tile> tiles)
        {
            ArrowTile? arrTile = tiles.Where(t => t.PosX == this.ArrowTile.x && t.PosY == this.ArrowTile.y).First() as ArrowTile;
            if(arrTile == null)
            {
                throw new Exception("WTF warum ist das hier null????");
            }
            short i = 1;
            ArrowDirection dir = arrTile.AllArrows.Where(a => a.ArrowOrigin.Equals(this.Pos)).First().ArrowDirection;
            var tmp_pos = new Position(arrTile.PosX, arrTile.PosY);
            while (true)
            {
                Tile? tileatnext = null;
                if (dir == ArrowDirection.Down)
                {
                    tileatnext = tiles.Where(t => t.PosX == arrTile.PosX && t.PosY == arrTile.PosY + i).FirstOrDefault();
                }
                else if( dir == ArrowDirection.Right)
                {
                    tileatnext = tiles.Where(t => t.PosX == arrTile.PosX && t.PosY + i == arrTile.PosY).FirstOrDefault();
                }
                else
                {
                    throw new Exception("??????");
                }
                if(tileatnext == null)
                {
                    break;
                }
                i++;
            }
            this.Length = i;
        }
    }
}
