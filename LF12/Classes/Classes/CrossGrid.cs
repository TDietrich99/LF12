using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LF12.Classes.Classes
{
    public class CrossGrid
    {
        [JsonPropertyName(nameof(Id))]
        public Guid Id { get; set; }

        [JsonPropertyName(nameof(Tiles))]
        public List<CrossGridTile> Tiles { get; set; } = new List<CrossGridTile>();

        [JsonPropertyName(nameof(DimensionX))]
        public int DimensionX { get; set; }

        [JsonPropertyName(nameof(DimensionY))]
        public int DimensionY { get; set; }

        public CrossGrid()
        {
            this.Id = GetUniqueGuid();
        }

        public void SetData()
        {
            string filePath = Path.Combine(ImageHelper.SolutionPath, this.Id.ToString().ToUpper());
            SetDimensions(filePath);
            SaveJson();
        }
        private Guid GetUniqueGuid()
        {
            return Guid.NewGuid(); 
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
            var pattern = new Regex("cell_(\\d+)_(\\d+)");
            var match = pattern.Match(fileName);
            if (match.Success && match.Groups.Count >= 2)
            {
                return Tuple.Create(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
            }
            return Tuple.Create(0, 0);
        }
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
        public CrossGrid Solve()
        {
            return new CrossGrid();
        }
        
        
    }
}
