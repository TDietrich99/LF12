using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Text;
using Tesseract;

namespace LF12.Classes.Classes
{
    public class TextHelper
    {
        #region Text Recognition
        public static string CorrectionString(string input)
        {
            string output = input;
            List<Tuple<Regex, string>> corrections = new List<Tuple<Regex, string>>()
            {
                Tuple.Create(new Regex(@"\n+")," "),
                Tuple.Create(new Regex(@"[_|\\]"),""),
                Tuple.Create(new Regex(@"-\s+"),""),
                Tuple.Create(new Regex(@"\s{2,}")," ")
            };
            foreach (var c in corrections)
            {
                output = c.Item1.Replace(output, c.Item2);
            }
            return output.Trim();
        }
        private static Mat[] GetImageWithText(Mat img, string cell = "")
        {
            var ret = new List<Mat>();
            // Schritt 4: Konturen im Bild finden
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hierarchy = new Mat();
            CvInvoke.FindContours(img, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxSimple);

            // Schritt 5: Rechtecke um die erkannten Textblöcke zeichnen
            List<Rectangle> contoursRect = new List<Rectangle>();
            for (int i = 0; i < contours.Size; i++)
            {
                // Bounding-Box um die Kontur erstellen
                Rectangle rect = CvInvoke.BoundingRectangle(contours[i]);
                if (rect.Height > ImageHelp.NoiseReduction)
                {
                    contoursRect.Add(rect);
                    if (!string.IsNullOrWhiteSpace(cell))
                    {
                        Mat tmp = new Mat(img, rect);
                        CvInvoke.Imwrite(cell.Replace(".png", $"_cont_{i}.png"), tmp);
                    }
                }
            }
            var grps = GroupByY(contoursRect);
            var bb = GetBoundingBoxes(grps, img.Width);
            for (int i = 0; i < bb.Count(); i++)
            {
                Rectangle curr = bb[i];
                try
                {
                    if (curr.Y < 0)
                        curr.Y = 0;
                    if (curr.Height + curr.Y > img.Height)
                        curr.Height = img.Height - curr.Y;

                    Mat n = new Mat(img, curr);

                    Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(2, 2), new Point(-1, -1));
                    CvInvoke.MorphologyEx(n, n, MorphOp.Open, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                    CvInvoke.GaussianBlur(n, n, new Size(5, 5), 0);
                    ret.Add(n);
                    if (!string.IsNullOrWhiteSpace(cell))
                        CvInvoke.Imwrite($"{cell.Replace(".png", "")}_{i}.png", n);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message.ToString());
                }
            }
            return ret.ToArray();
        }
        private static List<List<Rectangle>> GroupByY(List<Rectangle> rectangles)
        {
            // Sortiere die Rechtecke nach ihrer Y-Koordinate
            var sortedRectangles = rectangles.OrderBy(r => r.Y).ToList();

            List<List<Rectangle>> groups = new List<List<Rectangle>>();
            List<Rectangle> currentGroup = new List<Rectangle>();

            foreach (var rect in sortedRectangles)
            {
                if (currentGroup.Count == 0)
                {
                    // Falls die aktuelle Gruppe leer ist, füge das Rechteck hinzu
                    currentGroup.Add(rect);
                }
                else
                {
                    // Prüfe, ob das Rechteck zur aktuellen Gruppe passt (basierend auf Y-Koordinate und Toleranz)
                    if (Math.Abs(rect.Y - currentGroup.Last().Y) < ImageHelp.NoiseReduction)
                    {
                        currentGroup.Add(rect);
                    }
                    else
                    {
                        // Füge die aktuelle Gruppe zur Liste der Gruppen hinzu und beginne eine neue Gruppe
                        groups.Add(new List<Rectangle>(currentGroup));
                        currentGroup.Clear();
                        currentGroup.Add(rect);
                    }
                }
            }
            // Füge die letzte Gruppe hinzu, falls vorhanden
            if (currentGroup.Count > 0)
            {
                groups.Add(currentGroup);
            }
            return groups.Where(g => g.Count > 2).ToList();
        }
        private static List<Rectangle> GetBoundingBoxes(List<List<Rectangle>> rectGroups, int imgWidth)
        {
            var ret = new List<Rectangle>();
            imgWidth--;
            foreach (var group in rectGroups)
            {
                //Maximaler Y Wert in Gruppe
                int ysum = 0;
                int hsum = 0;
                int minX = ImageHelp.NoiseReduction;
                foreach (var rect in group)
                {
                    ysum += rect.Y;
                    hsum += rect.Height;
                    if (rect.X < minX)
                        minX = rect.X;
                }
                var rec = new Rectangle(minX, ysum / group.Count(), imgWidth - minX, hsum / group.Count());
                rec.Y -= ImageHelp.NoiseReduction;
                rec.Height += ImageHelp.NoiseReduction * 2;
                ret.Add(rec);
            }
            return ret;
        }
        public static string? GetText(Mat fullImg)
            => GetText(fullImg, "deu");
        public static string? GetText(Mat fullImg, string lang = "deu")
        {
            var path = "/";
            string datapath = path + Path.DirectorySeparatorChar + "tessdata";
            datapath = Directory.GetCurrentDirectory() + @"\tessdata-4.1.0\tessdata-4.1.0";
            Mat gray = new Mat();
            gray = fullImg;
            //CvInvoke.CvtColor(fullImg, gray, ColorConversion.Bgr2Gray);
            Mat[] splitImgs;
            splitImgs = GetImageWithText(gray);
            var engine = new TesseractEngine(datapath, lang, EngineMode.TesseractAndLstm);
            StringBuilder sb = new StringBuilder();
            foreach (Mat img in splitImgs)
            {
                MemoryStream memStrean = new MemoryStream();
                Bitmap btm = img.ToBitmap();
                btm.Save(memStrean, System.Drawing.Imaging.ImageFormat.Bmp);
                memStrean.Position = 0;
                Pix subimg = Pix.LoadFromMemory(memStrean.ToArray());

                var page = engine.Process(subimg, PageSegMode.SingleLine);
                string t = page.GetText();
                if (string.IsNullOrWhiteSpace(t))
                    return null;
                // text variable contains a string with all words found
                sb.Append(t);
                memStrean.Dispose();
                page.Dispose();
            }
            string ret = sb.ToString();
            return CorrectionString(ret);
        }
        public static async Task<string?> GetTextAsync(Mat fullImg, string lang = "deu")
        {
            return await Task.Run(() =>
            {
                return GetText(fullImg, lang);
            });
        }
        #endregion
    }
}
