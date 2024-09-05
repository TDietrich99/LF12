using Microsoft.AspNetCore.Identity;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

namespace LF12.Classes.Classes
{
    public class CrossGrid
    {
        #region Properties
        [JsonPropertyName(nameof(Id))]
        public Guid Id { get; set; }

        [JsonPropertyName(nameof(Tiles))]
        public List<CrossGridTile> Tiles { get; set; } = new List<CrossGridTile>();
        [JsonIgnore]
        private CrossGridTile[,] _Grid = null;
        [JsonIgnore]
        public CrossGridTile[,] Grid
        {
            get
            {
                if(this._Grid == null)
                {
                    this._Grid = new CrossGridTile[this.DimensionX, this.DimensionY];
                    foreach (var tile in this.Tiles)
                    {
                        this._Grid[tile.PosX, tile.PosY] = tile;
                    }
                }
                return this._Grid;
            }
            set
            {
                this._Grid = value;
            }
        }

        [JsonPropertyName(nameof(DimensionX))]
        public int DimensionX { get; set; }

        [JsonPropertyName(nameof(DimensionY))]
        public int DimensionY { get; set; }

        [JsonPropertyName(nameof(Arrows))]
        public List<Arrow> Arrows { get; set; }

        [JsonIgnore]
        private List<CrossGridTile> _Questions = null;
        [JsonIgnore]
        public List<CrossGridTile> Questions
        {
            get
            {
                if(this._Questions == null)
                {
                    this._Questions = new List<CrossGridTile>();
                    foreach(var tile in this.Tiles)
                    {
                        if(tile.GetChar() == null)
                        {
                            this._Questions.Add(tile);
                        }
                    }
                }
                return this._Questions;
            }
        }
        #endregion

        #region Constructor
        public CrossGrid()
        {
            this.Id = GetUniqueGuid();
        }
        #endregion

        #region Data Methods
        public void SetData()
        {

            ImageHelper.CreateTileImages(Path.Combine("Images", "Uploaded", this.Id.ToString().ToUpper() + "_RAW.png"), this.Id.ToString().ToUpper());
            string filePath = Path.Combine(ImageHelper.SolutionPath, this.Id.ToString().ToUpper());
            SetDimensions(filePath);
            for(int i = 0; i<  this.Tiles.Count; i++)
            {
                var tile = this.Tiles[i];
                if(tile != null && string.IsNullOrWhiteSpace(tile.TileText))
                {
                    var a = ImageHelper.DetectArrows(tile, this);
                    if(a != null)
                    {
                        if(this.Arrows == null)
                        {
                            this.Arrows = new List<Arrow>(); 
                        }
                        this.Arrows.AddRange(a);
                    }
                }
            }
            SaveJson();
        }
        private void DeleteTemporaryImageFiles()
        {
            var path = Path.Combine(ImageHelper.SolutionPath, this.Id.ToString().ToUpper());
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        private void SetDimensions(string filePath)
        {
            var files = Directory.GetFiles(filePath);
            var pos = Tuple.Create(0, 0);
            var tasks = new List<Task<CrossGridTile>>();    
            foreach (var file in files)
            {
                pos = GetPos(file);
                var cgt = CrossGridTile.CreateInstance(pos.Item1, pos.Item2, file);
                tasks.Add(cgt);
            }
            Task.WaitAll(tasks.ToArray());
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    this.Tiles.Add(task.Result);
                }
            }

            // +1 Wegen 0 Based Index
            this.DimensionX = pos.Item1 + 1; 
            this.DimensionY = pos.Item2 + 1;
        }
        private Tuple<int,int> GetPos(string fileName)
        {
            // Tuple ( PosX , PosY)
            var pattern = new Regex("cell_(\\d+)_(\\d+)");
            var match = pattern.Match(fileName);
            if (match.Success && match.Groups.Count >= 2)
            {
                return Tuple.Create(int.Parse(match.Groups[2].Value), int.Parse(match.Groups[1].Value));
            }
            return Tuple.Create(0, 0);
        }
        #endregion

        #region Helper Methods
        private Guid GetUniqueGuid()
        {
            var ret = Guid.NewGuid();
            while (Directory.Exists(Path.Combine("Jsons", ret.ToString().ToUpper())))
            {
                ret = Guid.NewGuid();
            }
            return ret;
        }
        #endregion

        #region Json Methods
        public static CrossGrid? FromJson(string json)
        {
            CrossGrid? ret = JsonSerializer.Deserialize<CrossGrid>(json);
            return ret;
        }
        private void SaveJson()
        {
            string path = Path.Combine("Jsons", this.Id.ToString().ToUpper());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(path, this.Id.ToString().ToUpper() + "_RAW.json");
            File.WriteAllText(path, this.ToJson());
            DeleteTemporaryImageFiles();
        }
        private string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
        #endregion

        #region Solve Methods
        private string[] GetSolutions(string question, string template)
        {
            return new string[4];
        }
        public CrossGrid Solve()
        {
            var ret = this;
            foreach (var q in this.Questions)
            {
                var template = GetTemplate(q);
                if(template != null)
                {
                    if(q.TileText == null)
                    {
                        throw new Exception("Textlose Frage");
                    }
                    var posSolutions = GetSolutions(q.TileText, template);
                    if(posSolutions.Count() == 0)
                    {
                        throw new Exception("Unbekannte Frage");
                    }
                    if(posSolutions.Count() == 1)
                    {
                        SetSolution(GetArrow(q).ArrowTile,posSolutions.First());
                    }
                }
            }
            return ret;
        }
        private void SetSolution(CrossGridTile arrowtile, string answer)
        {
            var nexttile = arrowtile;
            for(int i=0; i< answer.Length; i++)
            {
                var solChar = answer[i];
                if (nexttile.GetChar().Equals('_'))
                {
                    nexttile.SetChar(solChar);
                }
            }
        }
        private CrossGridTile? GetNextTile(CrossGridTile tile, ArrowDirection dir)
        {
            switch (dir)
            {
                case ArrowDirection.Right:
                    if(tile.PosX < this.DimensionX - 1)
                    {
                        return this.Grid[tile.PosX + 1, tile.PosY];
                    }
                    return null;
                case ArrowDirection.Down:
                    if(tile.PosY < this.DimensionY - 1)
                    {
                        return this.Grid[tile.PosX, tile.PosY + 1];
                    }
                    return null;
                case ArrowDirection.Left:
                case ArrowDirection.Up:
                default:
                    throw new Exception("Ungültiger Wert für Pfeile");
            }
        }
        private string GetTemplate(CrossGridTile tile)
        {
            // Heranziehen des Pfeiles
            var arr = GetArrow(tile);
            string ret = "";
            CrossGridTile? nextTile = arr.ArrowTile;
            while(nextTile != null && nextTile.GetChar() != null)
            {
                ret += nextTile.GetChar();
                nextTile = GetNextTile(nextTile, arr.Direction);
            }
            if(ret.Count() == 0)
            {
                throw new Exception("0 Lange Antwortmöglichkeit");
            }
            return ret;
        }
        private Arrow GetArrow(CrossGridTile tile)
        {
            var ret = this.Arrows.Where(a => a.Origin.Equals(tile));
            if(ret.Count() == 1)
            {
                return ret.First();
            }
            throw new Exception("Eine Frage hat keinen Pfeil?");
        }
        #endregion
    }
}
