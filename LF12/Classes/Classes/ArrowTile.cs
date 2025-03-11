using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    public class ArrowTile : BlankTile
    {
        [JsonPropertyName(nameof(AllArrows))]
        public List<Arrows> AllArrows { get; set; }
        public void SetArrowTileForQuestion(List<Tile> tiles)
        {
            foreach(Arrows arrs in this.AllArrows)
            {
                Tile? tile = tiles.Where(t => t.PosX == arrs.ArrowOrigin.x && t.PosY == arrs.ArrowOrigin.y).FirstOrDefault();
                if(tile == null || tile is not QuestionTile)
                {
                    throw new Exception("....");
                }
                QuestionTile qtile = tile as QuestionTile;
                qtile.ArrowTile = this;
            }
        }
    }
}
