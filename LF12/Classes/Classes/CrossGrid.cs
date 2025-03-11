using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Microsoft.AspNetCore.Identity;
using System.Drawing;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using System.Xml;

namespace LF12.Classes.Classes
{
    public class CrossGrid
    {
        #region Properties
        [JsonPropertyName(nameof(Id))]
        public Guid Id { get; set; }

        [JsonPropertyName(nameof(DimensionX))]
        public int DimensionX { get; set; }

        [JsonPropertyName(nameof(DimensionY))]
        public int DimensionY { get; set; }

        [JsonPropertyName(nameof(Tiles))]
        public List<Tile> Tiles { get; set; }
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
            Mat full = CvInvoke.Imread(Path.Combine("Images", "Uploaded", this.Id.ToString().ToUpper() + "_RAW.png"), ImreadModes.Grayscale);
            var ti = ImageHelp.CreateTileImages(full, this.Id);
            var tilesSorted = ti.OrderBy(y => y.Item2.y).ThenBy(x => x.Item2.x).ToList();
            this.Tiles = new List<Tile>();
            this.DimensionX = tilesSorted.Max(t => t.Item2.x) + 1;
            this.DimensionY = tilesSorted.Max(t => t.Item2.y) + 1;
            Position dim = new Position(this.DimensionX, this.DimensionY);
            foreach(var tile in tilesSorted)
            {
                var t = ImageHelp.GetTile(tile, dim);
                this.Tiles.Add(t);
            }
            //----//----//----//----//----//----//----//----//----//----//----//----//----//
            foreach(Tile t in this.Tiles)
            {
                if(t is not ArrowTile)
                {
                    continue;
                }
                ArrowTile arrtile = t as ArrowTile;
                arrtile.SetArrowTileForQuestion(this.Tiles);
            }
            foreach (Tile t in this.Tiles)
            {
                if (t is not QuestionTile)
                {
                    continue;
                }
                QuestionTile qtile = t as QuestionTile;
                qtile.SetQuestionLength(this.Tiles);
            }
            SaveJson();
            Solve();
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
            //DeleteTemporaryImageFiles();
        }
        private string ToJson()
        {
            var options = new JsonSerializerOptions { WriteIndented = true, TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
            return JsonSerializer.Serialize(this, options);
        }
        #endregion

        #region Solve Methods
        private void Solve()
        {
            // Max 10 durchläufe um die Lösung zu erhalten
            for(int i = 0; i < 10; i++)
            {
                foreach(Tile tile in this.Tiles)
                {
                    if(tile is not QuestionTile || ((QuestionTile)tile).Solution != null)
                    {
                        continue;
                    }
                    QuestionTile qtile = (QuestionTile)tile;
                    string[] sols = qtile.GetSolutions(this.Tiles);
                    if(sols.Length == 1)
                    {
                        qtile.SetSolution(sols[0], this.Tiles);
                    }
                }
            }
        }
        #endregion
    }
}
