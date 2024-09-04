using System.Text;
using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    public class CrossGridTile
    {
        [JsonPropertyName(nameof(PosX))]
        public int PosX { get; set; }

        [JsonPropertyName(nameof(PosY))]
        public int PosY { get; set; }

        [JsonPropertyName(nameof(TileText))]
        public string? TileText { get; set; }

        [JsonPropertyName(nameof(FilePath))]
        public string? FilePath { get; set; }
        public CrossGridTile() { }

        public static async Task<CrossGridTile> CreateInstance(int posx, int posy, string filepath)
        {
            
            var ret = new CrossGridTile();
            ret.FilePath = filepath;
            ret.PosX = posx;
            ret.PosY = posy;
            await ret.SetText(filepath);
            return ret;
        }
        public async Task SetText(string filepath)
        {
            string? text = await ImageHelper.GetText(filepath);
            this.TileText = text;
        }
    }
}
