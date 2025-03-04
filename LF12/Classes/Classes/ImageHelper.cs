using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing;
using Tesseract;
using Microsoft.Extensions.Hosting;
using Emgu.CV.Util;
using System.Diagnostics;
using System.Security.AccessControl;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using System.IO;
using Emgu.CV.Features2D;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Threading.Tasks;

namespace LF12.Classes.Classes
{
    public class ImageHelper
    {
        public static int MinResolution = 25; //px 
        public static int NoiseReduction = 15;
        public static string SolutionPath = Path.Combine("Images", "Solution");
        #region Arrow Detect
        public static List<Arrow>? DetectArrows(CrossGridTile tile, CrossGrid g)
        {
            if(tile.FilePath == null) return null;
            Mat img = CvInvoke.Imread(tile.FilePath, ImreadModes.Grayscale);
            var arrs = DetectArrows(img);
            if(arrs == null) return null;
            List<Arrow> ret = new List<Arrow>();
            // Setzen der Tiles
            for(int i = 0; i< arrs.Count; i++)
            {
                Arrow tmp = arrs[i];
                tmp.ArrowTile = tile;
                CrossGridTile arrowOrg = null;
                try
                {

                    switch (tmp.OriginDirection)
                    {
                        case ArrowDirection.Left:
                            arrowOrg =  g.Get(tile.PosX - 1, tile.PosY); 
                            break;
                        case ArrowDirection.Right:
                            arrowOrg = g.Get(tile.PosX + 1, tile.PosY);
                            break;
                        case ArrowDirection.Up:
                            arrowOrg = g.Get(tile.PosX, tile.PosY - 1);
                            break;
                        case ArrowDirection.Down:
                            arrowOrg = g.Get(tile.PosX, tile.PosY + 1);
                            break;
                        case ArrowDirection.None:
                            throw new Exception("WTF");
                    }
                }catch(ArgumentOutOfRangeException e)
                {
                    Debug.WriteLine(e.Message);
                }
                tmp.Origin = arrowOrg;
                ret.Add(tmp);
            }
            return ret;
        }
        public static List<Arrow>? DetectArrows(Mat m)
        {
            int cutPixel = 6;
            int height = m.Height;
            int width = m.Width;
            Rectangle crop = new Rectangle(cutPixel, cutPixel, width - 2*cutPixel, height - 2*cutPixel);
            Mat cropped = new Mat(m, crop);
            cropped.Save(Path.Combine("Images", "Debug", $"SubImg_org.png"));
            int cellWidth = cropped.Width / 3;
            int cellHeight = cropped.Height / 3;
            // Bild in 3x3 Raster aufteilen
            // Array für die Teilbilder
            float[,] ratios = new float[3, 3];
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (col == row && col == 1)
                    {
                        ratios[col, row] = 0;

                    }
                    // ROI definieren
                    var roi = new System.Drawing.Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);

                    // Teilbild erstellen
                    Mat tmp = new Mat(cropped, roi);
                    tmp.Save(Path.Combine("Images", "Debug", $"SubImg_{row}_{col}.png"));
                    int weißePixel = CvInvoke.CountNonZero(tmp);
                    int pixel = tmp.Width * tmp.Height;
                    float ratio = 1-(float)weißePixel / pixel;
                    ratios[row, col] = ratio;
                }
            }
            
            return GetArrowsFromRatios(ratios);
        }
        public static List<Arrow>? GetArrowsFromRatios(float[,] ratios)
        {
            var ret = new List<Arrow>();
            // Pfeile können nur in 0,1 und 1,0 auftreten.
            // Prüfe 0,1
            if (ratios[0,1] > 0)
            {
                // Pfeil ist vorhanden und muss nach unten Zeigen
                // Links/Rechts schauen
                var arr = new Arrow();
                arr.Direction = ArrowDirection.Down;
                if (ratios[0, 0] > 0)
                {
                    arr.OriginDirection = ArrowDirection.Left;
                }
                else if (ratios[0, 2] > 0)
                {
                    arr.OriginDirection = ArrowDirection.Right;
                }
                else
                {
                    arr.OriginDirection = ArrowDirection.Up;
                }
                ret.Add(arr);
            }
            // Prüfe 1,0
            if (ratios[1,0] > 0)
            {
                // Pfeil ist vorhanden und muss nach Rechts Zeigen
                // Oben/Unten schauen
                var arr = new Arrow();
                arr.Direction = ArrowDirection.Right;
                if (ratios[0, 0] > 0)
                {
                    arr.OriginDirection = ArrowDirection.Up;
                }
                else if (ratios[2, 0] > 0)
                {
                    arr.OriginDirection = ArrowDirection.Down;
                }
                else
                {
                    arr.OriginDirection = ArrowDirection.Left;
                }
                ret.Add(arr);
            }
            if (ret.Count == 0)
                return null;
            return ret;
        }
        #endregion

        #region Border Detect
        private static List<Point> FilterPoints(List<Point> points)
        {
            var ret = new List<Point>(points);
            foreach(Point point in points)
            {
                var close = ret.Where(p => Math.Abs(p.X - point.X) < ImageHelper.NoiseReduction && Math.Abs(p.Y - point.Y) < ImageHelper.NoiseReduction).ToArray();
                if(close.Length != 0)
                {
                    int x = 0;
                    int y = 0;
                    foreach(var c in close)
                    {
                        ret.Remove(c);
                        x += c.X;
                        y += c.Y;
                    }
                    var bestpoint = new Point(
                        x/close.Length, 
                        y/close.Length
                        );
                    ret.Add(bestpoint);
                }
            }
            return ret;
        }
        private static double Distance(Point p1, Point p2)
        {
            // Satz des Pythagoras zur bestimmung der Distanz zwischen zwei Punkten
            // ( Vektortheorie ) 
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        private static List<Point[]> GetTrapezesFromPoints(List<Point> points)
        {
            var ret = new List<Point[]>();
            foreach(Point p in points)
            {
                Point[] toAdd = new Point[4];
                toAdd[0] = p;
                // Annahme: point ist die obere Linke Ecke des Trapezes

                // 1. Punkt rechts von p (X > p.X)
                var pointRight = points
                    .Where(pt => pt.X - p.X > ImageHelper.NoiseReduction && pt != p)
                    .OrderBy(pt => Distance(p, pt));

                // 2. Punkt unterhalb von p (Y > p.Y)
                var pointBelow = points
                    .Where(pt => pt.Y - p.Y > ImageHelper.NoiseReduction && pt != p)
                    .OrderBy(pt => Distance(p, pt));

                // 3. Punkt diagonal unten rechts von p (X > p.X und Y > p.Y)
                var pointDiagonal = points
                    .Where(pt => pt.X - p.X > ImageHelper.NoiseReduction && pt.Y - p.Y > ImageHelper.NoiseReduction && pt != p)
                    .OrderBy(pt => Distance(p, pt));
                
                // Passenden Punkte für ein Trapez gefunden
                if(pointRight.Count() != 0 && pointBelow.Count() != 0 && pointDiagonal.Count() != 0)
                {
                    toAdd[1] = pointRight.First();
                    toAdd[2] = pointBelow.First();
                    toAdd[3] = pointDiagonal.First();
                    ret.Add(toAdd);
                }
            }
            return ret;
        }
        private static Point GetTileCoordinates(Point[] currTile, List<Point[]> allTiles)
        {
            // Point[] an [0] ist der obere Linke Punkt Alle Punkte sind ein Mindestmaß voneinander entfernt

            int pointsWithSmallerX = 0;
            int pointsWithSmallerY = 0;
            pointsWithSmallerX = allTiles.Where(t => t[0].X < currTile[0].X && Math.Abs(t[0].Y - currTile[0].Y) < ImageHelper.NoiseReduction).Count();
            pointsWithSmallerY = allTiles.Where(t => t[0].Y < currTile[0].Y && Math.Abs(t[0].X - currTile[0].X) < ImageHelper.NoiseReduction).Count();
            return new Point(pointsWithSmallerX, pointsWithSmallerY);

        }
        public static void CreateTileImages(string imagePath, string totFiles)
        {
            var solutionPath = Path.Combine(SolutionPath, totFiles);
            if (!Directory.Exists(solutionPath))
            {
                Directory.CreateDirectory(solutionPath);
            }
            Mat img = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
            // 1. Bild laden und in Graustufen konvertieren
            // 2. Bild vorverarbeiten (Rauschen reduzieren, Kanten hervorheben)
            Mat blurred = new Mat();
            CvInvoke.GaussianBlur(img, blurred, new Size(5, 5), 0);
            Mat edges = new Mat();
            CvInvoke.Canny(blurred, edges, 50, 150);

            CvInvoke.Imwrite(Path.Combine(solutionPath,"RAW.png"), edges);
            #endregion
            // 3. Linien mit der Hough-Transformation erkennen
            LineSegment2D[] lines = CvInvoke.HoughLinesP(edges, 1, Math.PI / 180.0, 200, 50, 20);
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
            var filtered = FilterPoints(intersections);
            List<int> xCoords = new List<int>();
            List<int> yCoords = new List<int>();
            Mat imgO = CvInvoke.Imread(imagePath);
            foreach (var point in filtered)
            {
                CvInvoke.DrawMarker(imgO, point, new MCvScalar(0, 0, 255), MarkerTypes.TiltedCross, 5, 1);
                var xs = xCoords.Where(x => Math.Abs(point.X - x) < ImageHelper.NoiseReduction).ToArray();
                if(xs.Length == 0)
                {
                    xCoords.Add(point.X);
                }
                var ys = yCoords.Where(y => Math.Abs(point.Y - y) < ImageHelper.NoiseReduction).ToArray();
                if (ys.Length == 0)
                {
                    yCoords.Add(point.Y);
                }
            }
            CvInvoke.Imwrite(Path.Combine(solutionPath, "points.png"), imgO);

            xCoords.Sort();
            yCoords.Sort();
            // 5. Felder extrahieren und speichern
            List<string> errors = new List<string> ();
            var trapezesPoints = GetTrapezesFromPoints(filtered);
            foreach(var trapezPoints in trapezesPoints)
            {
                Rectangle boundingRect = CvInvoke.BoundingRectangle(new VectorOfPoint(trapezPoints));
                Mat cell = new Mat(img, boundingRect);
                Point coords = GetTileCoordinates(trapezPoints, trapezesPoints);
                string cellImagePath = $"cell_{PadLeft(coords.Y)}_{PadLeft(coords.X)}.png";
                SharpenImg(cell, Path.Combine(solutionPath, cellImagePath));
            }
            return;
        }
        static string PadLeft(int input, int digits = 3, char padding = '0')
        {
            string num = input.ToString();
            if (num.Length >= digits) return num;
            while (digits > num.Length)
            {
                num = padding + num;
            }
            return num;
        }
        static bool FindIntersection(LineSegment2D line1, LineSegment2D line2, out Point intersection)
        {
            // Nur waagerechte/senkrechte Linien prüfen
            intersection = Point.Empty;

            // Nur lang genuge Linien erlauben (Kästen)
            if (line1.Length < ImageHelper.MinResolution || line2.Length < ImageHelper.MinResolution)
                return false;

            float a1 = line1.P2.Y - line1.P1.Y;
            float b1 = line1.P1.X - line1.P2.X;
            float c1 = a1 * line1.P1.X + b1 * line1.P1.Y;

            float a2 = line2.P2.Y - line2.P1.Y;
            float b2 = line2.P1.X - line2.P2.X;
            float c2 = a2 * line2.P1.X + b2 * line2.P1.Y;

            float delta = a1 * b2 - a2 * b1;
            if (delta == 0)
            {
                return false; // Linien sind parallel
            }
            intersection.X = (int)((b2 * c1 - b1 * c2) / delta);
            intersection.Y = (int)((a1 * c2 - a2 * c1) / delta);
            return true;
        }

        #region Image Enhance
        public static Mat SharpenImg(Mat img, string filePath, bool ShowDebugImages = false)
        {
            // 2. Bild vergrößern (Upscaling)
            Mat upscaledImg = new Mat();
            CvInvoke.Resize(img, upscaledImg, new Size(img.Width * 4, img.Height * 4), interpolation: Inter.Cubic);

            // 3. Unschärfereduktion: Schärfen mit Unschärfemaske
            Mat blurredImg = new Mat();
            CvInvoke.GaussianBlur(upscaledImg, blurredImg, new Size(5, 5),0);
            Mat sharpenedImg = new Mat();
            CvInvoke.AddWeighted(upscaledImg, 1.6, blurredImg, -0.5, 0, sharpenedImg);

            // 4. Kontrasterhöhung und Helligkeit verbessern
            Mat contrastEnhancedImg = new Mat();
            CvInvoke.ConvertScaleAbs(sharpenedImg, contrastEnhancedImg, 1.1, 15);

            // 5. Binarisierung
            Mat binaryImg = new Mat();
            CvInvoke.Threshold(contrastEnhancedImg, binaryImg, 202, 255, ThresholdType.Binary);

            // Bild speichern, um es mit Tesseract zu verarbeiten
            CvInvoke.Imwrite(filePath, binaryImg);
            if (ShowDebugImages)
            {
                CvInvoke.Imwrite(filePath.Replace(".png", "_nonbinary.png"), contrastEnhancedImg);
            }

            // Cleanup
            img.Dispose();
            upscaledImg.Dispose();
            blurredImg.Dispose();
            sharpenedImg.Dispose();
            contrastEnhancedImg.Dispose();
            return binaryImg;
        }
        public static Mat SharpenImg(string imagePath, string filePath)
        {
            // 1. Bild laden
            Mat img = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);
            return SharpenImg(img, filePath);

        }
        #endregion

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
                if(rect.Height > ImageHelper.NoiseReduction)
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
                    if(!string.IsNullOrWhiteSpace(cell))
                        CvInvoke.Imwrite($"{cell.Replace(".png","")}_{i}.png", n);
                }
                catch(Exception ex)
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
                    if (Math.Abs(rect.Y - currentGroup.Last().Y) < ImageHelper.NoiseReduction)
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
            foreach(var group in rectGroups)
            {
                //Maximaler Y Wert in Gruppe
                int ysum = 0;
                int hsum = 0;
                int minX = ImageHelper.NoiseReduction;
                foreach(var rect in group)
                {
                    ysum += rect.Y;
                    hsum += rect.Height;
                    if(rect.X < minX)
                        minX = rect.X;
                }
                var rec = new Rectangle(minX, ysum / group.Count(), imgWidth - minX, hsum / group.Count());
                rec.Y -= ImageHelper.NoiseReduction;
                rec.Height += ImageHelper.NoiseReduction * 2;
                ret.Add(rec);
            }
            return ret;
        }
        public static string? GetText(string sourceFilePath)
            => GetText(sourceFilePath, "deu", false);
        public static string? GetText(string sourceFilePath, string lang = "deu")
            => GetText(sourceFilePath, lang, false);
        public static string? GetText(string sourceFilePath, string lang = "deu", bool debug = false)
        {
            var path = "/";
            string datapath = path + Path.DirectorySeparatorChar + "tessdata";
            datapath = @"C:\Users\Dietrich\Downloads\tessdata-4.1.0\tessdata-4.1.0";
            Mat fullImg = CvInvoke.Imread(sourceFilePath, ImreadModes.Color);
            Mat gray = new Mat();
            CvInvoke.CvtColor(fullImg, gray, ColorConversion.Bgr2Gray);
            Mat[] splitImgs;
            if(debug)
                splitImgs = GetImageWithText(gray, sourceFilePath);
            else
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
        public static async Task<string?> GetTextAsync(string sourceFilePath, string lang = "deu")
        {
            return await Task.Run(() =>
            {
                return GetText(sourceFilePath, lang);
            });
        }
        #endregion
    }
}
