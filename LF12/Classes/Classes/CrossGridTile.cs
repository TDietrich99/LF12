using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace LF12.Classes.Classes
{
    public class CrossGridTile
    {
        #region Properties
        bool HasArrow { get; set; }
        bool HasQuestion { get; set; }
        [JsonPropertyName(nameof(PosX))]
        public int PosX { get; set; }

        [JsonPropertyName(nameof(PosY))]
        public int PosY { get; set; }

        [JsonPropertyName(nameof(FilePath))]
        public string? FilePath { get; set; }

        [JsonPropertyName(nameof(TileText))]
        public string? TileText { get; set; }

        #endregion

        #region Constructors
        public CrossGridTile() { }
        #endregion

        #region Async Methods
        public static CrossGridTile CreateInstance(int posx, int posy, string filepath)
        {
            var ret = new CrossGridTile();
            ret.FilePath = filepath;
            ret.PosX = posx;
            ret.PosY = posy;
            ret.SetText(filepath);
            return ret;
        }
        public static async Task<CrossGridTile> CreateInstanceAsync(int posx, int posy, string filepath)
        {
            
            var ret = new CrossGridTile();
            ret.FilePath = filepath;
            ret.PosX = posx;
            ret.PosY = posy;
            await ret.SetTextAsync(filepath);
            return ret;
        }
        public void SetText(string filepath)
        {
            this.TileText = ImageHelper.GetText(filepath);
        }
        public async Task SetTextAsync(string filepath)
        {
            string? text = await ImageHelper.GetTextAsync(filepath);
            this.TileText = text;
        }
        public void SetChar(char c)
        {
            this.TileText = c.ToString().ToUpper();
        }
        #endregion

        #region Helper Methods
        public override bool Equals(object? obj)
        {
            if(obj != null)
            {
                var cobj = obj as CrossGridTile;

                if(cobj != null && cobj.PosX.Equals(this.PosX) && cobj.PosY.Equals(this.PosY))
                {
                    return true;
                }
            }
            return false;
        }
        public char? GetChar()
        {
            if (string.IsNullOrWhiteSpace(this.TileText))
                return '_';
            if(this.TileText.Length > 1) 
                return null;
            return this.TileText[0];
        }
        #endregion

    }

}
