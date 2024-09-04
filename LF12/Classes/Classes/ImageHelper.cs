using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using Tesseract;
using Microsoft.Extensions.Hosting;

namespace LF12.Classes.Classes
{
    public class ImageHelper
    {
        public static string SolutionPath = Path.Combine("Images", "Solution");
        #region Border Detect
        public static void CreateTileImages(string imagePath, string totFiles)
        {
            var solutionPath = Path.Combine(SolutionPath, totFiles);
            if (!Directory.Exists(solutionPath))
            {
                Directory.CreateDirectory(solutionPath);
            }
            // 1. Bild laden und in Graustufen konvertieren
            Mat img = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
            // 2. Bild vorverarbeiten (Rauschen reduzieren, Kanten hervorheben)
            Mat blurred = new Mat();
            CvInvoke.GaussianBlur(img, blurred, new Size(5, 5), 0);
            Mat edges = new Mat();
            CvInvoke.Canny(blurred, edges, 50, 150);

            // 3. Linien mit der Hough-Transformation erkennen
            LineSegment2D[] lines = CvInvoke.HoughLinesP(edges, 1, Math.PI / 180.0, 100, 50, 10);

            // 4. Schnittpunkte der Linien (Gitter) identifizieren
            List<Point> intersections = new List<Point>();
            for (int i = 0; i < lines.Length; i++)
            {
                for (int j = i + 1; j < lines.Length; j++)
                {
                    if (FindIntersection(lines[i], lines[j], out Point intersection))
                    {
                        intersections.Add(intersection);
                    }
                }
            }

            // Sortiere die Schnittpunkte und finde die Zellen
            // Hier sortieren wir die Punkte und finden die einzigartigen X- und Y-Koordinaten, um die Zellen zu definieren.
            List<int> xCoords = new List<int>();
            List<int> yCoords = new List<int>();

            foreach (var point in intersections)
            {
                if (!xCoords.Contains(point.X)) xCoords.Add(point.X);
                if (!yCoords.Contains(point.Y)) yCoords.Add(point.Y);
            }

            xCoords.Sort();
            yCoords.Sort();
            // 5. Felder extrahieren und speichern
            int y = 0;
            for (int i = 0; i < xCoords.Count - 1; i++)
            {
                int x = 0;
                for (int j = 0; j < yCoords.Count - 1; j++)
                {
                    Rectangle cellRect = new Rectangle(
                        xCoords[i], yCoords[j],
                        xCoords[i + 1] - xCoords[i],
                        yCoords[j + 1] - yCoords[j]);
                    //Nur Quadrate Zulassen
                    if (Math.Abs(cellRect.Height - cellRect.Width) > 2 || cellRect.Height < 50)
                    {
                        continue;
                    }
                    Mat cell = new Mat(img, cellRect);
                    string cellImagePath = $"cell_{PadLeft(y.ToString())}_{PadLeft(x.ToString())}.png";
                    x++;
                    SharpenImg(cell, Path.Combine(solutionPath, cellImagePath));
                }
                if (x > 0) y++;
            }
            return;
        }
        static string PadLeft(string num, int digits = 3, char padding = '0')
        {
            if (num.Length >= digits) return num;
            while (digits > num.Length)
            {
                num = padding + num;
            }
            return num;
        }
        static bool FindIntersection(LineSegment2D line1, LineSegment2D line2, out Point intersection)
        {
            float a1 = line1.P2.Y - line1.P1.Y;
            float b1 = line1.P1.X - line1.P2.X;
            float c1 = a1 * line1.P1.X + b1 * line1.P1.Y;

            float a2 = line2.P2.Y - line2.P1.Y;
            float b2 = line2.P1.X - line2.P2.X;
            float c2 = a2 * line2.P1.X + b2 * line2.P1.Y;

            float delta = a1 * b2 - a2 * b1;
            if (delta == 0)
            {
                intersection = Point.Empty;
                return false; // Linien sind parallel
            }

            intersection = new Point(
                (int)((b2 * c1 - b1 * c2) / delta),
                (int)((a1 * c2 - a2 * c1) / delta)
            );

            return true;
        }
        #endregion

        #region Image Enhance
        static void SharpenImg(Mat img, string filePath)
        {
            // 2. Bild vergrößern (Upscaling)
            Mat upscaledImg = new Mat();
            CvInvoke.Resize(img, upscaledImg, new Size(img.Width * 4, img.Height * 4), interpolation: Inter.Linear);

            // 3. Unschärfereduktion: Schärfen mit Unschärfemaske
            Mat blurredImg = new Mat();
            CvInvoke.GaussianBlur(upscaledImg, blurredImg, new Size(5, 5), 0);
            Mat sharpenedImg = new Mat();
            CvInvoke.AddWeighted(upscaledImg, 1.5, blurredImg, -0.5, 0, sharpenedImg);

            // 4. Kontrasterhöhung und Helligkeit verbessern
            Mat contrastEnhancedImg = new Mat();
            CvInvoke.ConvertScaleAbs(sharpenedImg, contrastEnhancedImg, 1.2, 20);

            // 5. Binarisierung
            Mat binaryImg = new Mat();
            CvInvoke.Threshold(contrastEnhancedImg, binaryImg, 128, 255, ThresholdType.Binary | ThresholdType.Otsu);

            // Optional: Bild speichern, um es mit Tesseract zu verarbeiten
            CvInvoke.Imwrite(filePath, binaryImg);

            // Cleanup
            img.Dispose();
            upscaledImg.Dispose();
            blurredImg.Dispose();
            sharpenedImg.Dispose();
            contrastEnhancedImg.Dispose();
            binaryImg.Dispose();
        }
        public static void SharpenImg(string imagePath, string filePath)
        {
            // 1. Bild laden
            Mat img = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
            SharpenImg(img, filePath);

        }
        #endregion

        #region Text Recognition
        public static async Task<string?> GetText(string sourceFilePath, string lang = "deu")
        {
            var path = "/";
            string datapath = path + Path.DirectorySeparatorChar + "tessdata";
            datapath = @"C:\Users\Dietrich\Downloads\tessdata-4.1.0\tessdata-4.1.0";
            return await Task.Run(() =>
            {
                using (var engine = new TesseractEngine(datapath, lang, EngineMode.TesseractAndLstm))
                {
                    var img = Pix.LoadFromFile(sourceFilePath);
                    using (img)
                    {
                        using (var page = engine.Process(img))
                        {
                            string t = page.GetText();
                            if (string.IsNullOrWhiteSpace(t))
                                return null;
                            // text variable contains a string with all words found

                            return t.Replace("-\n","").Replace("\n", " ").Replace("\t", "").Replace("\r", "").Trim();
                        }
                    }
                }
            });

        }
        #endregion
    }
}
