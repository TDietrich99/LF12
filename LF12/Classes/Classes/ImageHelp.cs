using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Diagnostics;
using System.Drawing;

namespace LF12.Classes.Classes
{
    public struct Position
    {
        public int x;
        public int y;
        public Position(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    public class ImageHelp
    {

        public static string SolutionPath = Path.Combine("Images", "Solution");
        public static List<Tuple<Mat,Position>> CreateTileImages(Mat fullImage, Guid CrossGridId)
        {
            if(!Directory.Exists(Path.Combine(SolutionPath, CrossGridId.ToString())))
            {
                Directory.CreateDirectory(Path.Combine(SolutionPath, CrossGridId.ToString()));
            }
            var ret = new List<Tuple<Mat,Position>>();
            // 1. Bild laden und in Graustufen konvertieren
            // 2. Bild vorverarbeiten (Rauschen reduzieren, Kanten hervorheben)
            Mat blurred = new Mat();
            CvInvoke.GaussianBlur(fullImage, blurred, new Size(5, 5), 0);
            Mat edges = new Mat();
            CvInvoke.Canny(blurred, edges, 50, 150);

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
            foreach (var point in filtered)
            {
                var xs = xCoords.Where(x => Math.Abs(point.X - x) < ImageHelper.NoiseReduction).ToArray();
                if (xs.Length == 0)
                {
                    xCoords.Add(point.X);
                }
                var ys = yCoords.Where(y => Math.Abs(point.Y - y) < ImageHelper.NoiseReduction).ToArray();
                if (ys.Length == 0)
                {
                    yCoords.Add(point.Y);
                }
            }

            xCoords.Sort();
            yCoords.Sort();
            // 5. Felder extrahieren und speichern
            List<string> errors = new List<string>();
            var trapezesPoints = GetTrapezesFromPoints(filtered);
            foreach (var trapezPoints in trapezesPoints)
            {
                Rectangle boundingRect = CvInvoke.BoundingRectangle(new VectorOfPoint(trapezPoints));
                Mat cell = new Mat(fullImage, boundingRect);
                Point coords = GetTileCoordinates(trapezPoints, trapezesPoints);
                string cellImageFileName = $"cell_{PadLeft(coords.Y)}_{PadLeft(coords.X)}.png";
                Mat cellSharp = SharpenImage(cell);
                cellSharp.Save(Path.Combine(SolutionPath, CrossGridId.ToString(), cellImageFileName));
                ret.Add(Tuple.Create(cellSharp, new Position(coords.X, coords.Y)));
            }
            return ret;
        }
        public static Mat SharpenImage(Mat img)
        {
            // 2. Bild vergrößern (Upscaling)
            Mat upscaledImg = new Mat();
            CvInvoke.Resize(img, upscaledImg, new Size(img.Width * 4, img.Height * 4), interpolation: Inter.Cubic);

            // 3. Unschärfereduktion: Schärfen mit Unschärfemaske
            Mat blurredImg = new Mat();
            CvInvoke.GaussianBlur(upscaledImg, blurredImg, new Size(5, 5), 0);
            Mat sharpenedImg = new Mat();
            CvInvoke.AddWeighted(upscaledImg, 1.6, blurredImg, -0.5, 0, sharpenedImg);

            // 4. Kontrasterhöhung und Helligkeit verbessern
            Mat contrastEnhancedImg = new Mat();
            CvInvoke.ConvertScaleAbs(sharpenedImg, contrastEnhancedImg, 1.1, 15);

            // 5. Binarisierung
            Mat binaryImg = new Mat();
            CvInvoke.Threshold(contrastEnhancedImg, binaryImg, 202, 255, ThresholdType.Binary);

            // Cleanup
            img.Dispose();
            upscaledImg.Dispose();
            blurredImg.Dispose();
            sharpenedImg.Dispose();
            contrastEnhancedImg.Dispose();
            return binaryImg;
        }

        #region Helper
        private static bool FindIntersection(LineSegment2D line1, LineSegment2D line2, out Point intersection)
        {
            // Nur waagerechte/senkrechte Linien prüfen
            intersection = Point.Empty;

            // Nur lang genuge Linien erlauben (Kästen)
            if (line1.Length<ImageHelper.MinResolution || line2.Length<ImageHelper.MinResolution)
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
            intersection.X = (int) ((b2* c1 - b1* c2) / delta);
            intersection.Y = (int) ((a1* c2 - a2* c1) / delta);
            return true;
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
        private static double Distance(Point p1, Point p2)
        {
            // Satz des Pythagoras zur bestimmung der Distanz zwischen zwei Punkten
            // ( Vektortheorie ) 
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        private static List<Point[]> GetTrapezesFromPoints(List<Point> points)
        {
            var ret = new List<Point[]>();
            foreach (Point p in points)
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
                if (pointRight.Count() != 0 && pointBelow.Count() != 0 && pointDiagonal.Count() != 0)
                {
                    toAdd[1] = pointRight.First();
                    toAdd[2] = pointBelow.First();
                    toAdd[3] = pointDiagonal.First();
                    ret.Add(toAdd);
                }
            }
            return ret;
        }
        private static string PadLeft(int input, int digits = 3, char padding = '0')
        {
            string num = input.ToString();
            if (num.Length >= digits) return num;
            while (digits > num.Length)
            {
                num = padding + num;
            }
            return num;
        }
        private static List<Point> FilterPoints(List<Point> points)
        {
            var ret = new List<Point>(points);
            foreach (Point point in points)
            {
                var close = ret.Where(p => Math.Abs(p.X - point.X) < ImageHelper.NoiseReduction && Math.Abs(p.Y - point.Y) < ImageHelper.NoiseReduction).ToArray();
                if (close.Length != 0)
                {
                    int x = 0;
                    int y = 0;
                    foreach (var c in close)
                    {
                        ret.Remove(c);
                        x += c.X;
                        y += c.Y;
                    }
                    var bestpoint = new Point(
                        x / close.Length,
                        y / close.Length
                        );
                    ret.Add(bestpoint);
                }
            }
            return ret;
        }
        #endregion

        #region TileDetection
        public static Tile GetTile(Tuple<Mat,Position> tileImg_Pos)
        {
            string? textInTile = TextHelper.GetText(tileImg_Pos.Item1);
            if (!string.IsNullOrWhiteSpace(textInTile))
            {
                return new QuestionTile
                {
                    PosX = tileImg_Pos.Item2.x,
                    PosY = tileImg_Pos.Item2.y,
                    Question = textInTile
                };

            }
            else
            {
                var arrs = FindArrows(tileImg_Pos.Item1, tileImg_Pos.Item2);
                if(arrs.Count > 0)
                {
                    return new ArrowTile
                    {

                        PosX = tileImg_Pos.Item2.x,
                        PosY = tileImg_Pos.Item2.y,
                        AllArrows = arrs
                    };
                }
            }
            return new BlankTile
            {
                PosX = tileImg_Pos.Item2.x,
                PosY = tileImg_Pos.Item2.y
            };
        }
        #endregion

        #region Arrows
        public static List<Arrows> FindArrows(Mat m, Position currentTilePos)
        {
            var ret = new List<Arrows>();
            int cutPixel = 6;
            int height = m.Height;
            int width = m.Width;
            Rectangle crop = new Rectangle(cutPixel, cutPixel, width - 2 * cutPixel, height - 2 * cutPixel);
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
                    int weißePixel = CvInvoke.CountNonZero(tmp);
                    int pixel = tmp.Width * tmp.Height;
                    float ratio = 1 - (float)weißePixel / pixel;
                    ratios[row, col] = ratio;
                }
            }

            // Pfeile können nur in 0,1 und 1,0 auftreten.
            // Prüfe 0,1
            if (ratios[0, 1] > 0)
            {
                // Pfeil ist vorhanden und muss nach unten Zeigen
                // Links/Rechts schauen
                var arr = new Arrows();
                arr.ArrowDirection = ArrowDirection.Down;
                if (ratios[0, 0] > 0)
                {
                    arr.ArrowOrigin = new Position
                    {
                        x = currentTilePos.x - 1,
                        y = currentTilePos.y
                    };
                }
                else if (ratios[0, 2] > 0)
                {
                    arr.ArrowOrigin = new Position
                    {
                        x = currentTilePos.x + 1,
                        y = currentTilePos.y
                    };
                }
                else
                {
                    arr.ArrowOrigin = new Position
                    {
                        x = currentTilePos.x, 
                        y = currentTilePos.y - 1
                    };
                }
                ret.Add(arr);
            }
            // Prüfe 1,0
            if (ratios[1, 0] > 0)
            {
                // Pfeil ist vorhanden und muss nach Rechts Zeigen
                // Oben/Unten schauen
                var arr = new Arrows();
                arr.ArrowDirection = ArrowDirection.Right;
                if (ratios[0, 0] > 0)
                {
                    arr.ArrowOrigin = new Position
                    {
                        x = currentTilePos.x,
                        y = currentTilePos.y - 1
                    };
                }
                else if (ratios[2, 0] > 0)
                {
                    arr.ArrowOrigin = new Position
                    {
                        x = currentTilePos.x,
                        y = currentTilePos.y + 1
                    };
                }
                else
                {
                    arr.ArrowOrigin = new Position
                    {
                        x = currentTilePos.x - 1,
                        y = currentTilePos.y
                    };
                }
                ret.Add(arr);
            }
            return ret;
        }
        #endregion
    }
}
