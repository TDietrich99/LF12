using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.AspNetCore.Identity;
using System.Drawing;
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
            foreach(var tile in tilesSorted)
            {
                var t = ImageHelp.GetTile(tile);
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
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(this, options);
        }
        #endregion

        #region Solve Methods
        private string[] GetSolutions(string question, string template)
        {
            return new string[4];
        }
        #endregion
    }
}
