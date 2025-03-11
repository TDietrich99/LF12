using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    public class QuestionTile : Tile
    {
        [JsonPropertyName(nameof(Question))]
        public string? Question { get; set; }
        [JsonPropertyName(nameof(Solution))]
        public string Solution { get; set; }
        [JsonPropertyName(nameof(ArrowTileCoord))]
        public Position ArrowTileCoord
        {
            get
            {
                return this.ArrowTile.Pos;
            }
        }
        public ArrowTile ArrowTile { get; set; }
        public short Length { get; set; }
        private ArrowDirection? _ArrowDirection = null;
        public ArrowDirection ArrowDirection
        {
            get
            {
                if (!this._ArrowDirection.HasValue)
                {
                    this._ArrowDirection = this.ArrowTile.AllArrows.Where(a => a.ArrowOrigin.Equals(this.Pos)).First().ArrowDirection;
                }
                return this._ArrowDirection.Value;
            }
        }

        public string[] GetSolutions(List<Tile> tiles)
        {
            var blank = GetBlankAnswer(tiles);

            return new string[4];
        }
        public void SetSolution(string solution, List<Tile> tiles)
        { 
            if(solution.Length != this.Length)
            {
                throw new Exception("Lösung hat falsche Länge");
            }
            for (int i = 0; i < this.Length; i++)
            {
                Position nextPos = this.ArrowTile.Pos;
                switch (this.ArrowDirection)
                {
                    case ArrowDirection.Down:
                        nextPos.y++;
                        break;
                    case ArrowDirection.Right:
                        nextPos.x++;
                        break;
                }
                BlankTile nextTile = (BlankTile)tiles.First(t => t.Pos.Equals(nextPos));
                nextTile.Letter = solution[i];
            }
            this.Solution = solution;
        }
        private string GetBlankAnswer(List<Tile> tiles)
        {
            StringBuilder ret = new StringBuilder();
            for(int i = 0; i < this.Length; i++)
            {
                Position nextPos = this.ArrowTile.Pos;
                switch (this.ArrowDirection)
                {
                    case ArrowDirection.Down:
                        nextPos.y++;
                        break;
                    case ArrowDirection.Right:
                        nextPos.x++;
                        break;
                }
                BlankTile nextTile = (BlankTile)tiles.First(t => t.Pos.Equals(nextPos));
                ret.Append(nextTile.Letter.HasValue ? nextTile.Letter.Value : "_");
            }
            return ret.ToString();
        }
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
                if(tileatnext == null || tileatnext is QuestionTile)
                {
                    break;
                }
                i++;
            }
            this.Length = i;
        }
    }
}
