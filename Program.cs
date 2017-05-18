using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.DelegateAllocation

namespace Mandelbrot
{
    /// <summary>
    /// </summary>
    internal class Display : IDisposable
    {
        /// <summary>
        /// </summary>
        private Image _bmp;

        /// <exception cref="ArgumentException">The <paramref name="filePath" /> does not indicate a valid file.-or-The <paramref name="filePath" /> indicates a Universal Naming Convention (UNC) path.</exception>
        /// <exception cref="Exception">The specified control is a top-level control, or a circular control reference would result if this control were added to the control collection. </exception>
        /// <exception cref="InvalidOperationException">The form was closed while a handle was being created. </exception>
        /// <exception cref="ObjectDisposedException">You cannot call this method from the <see cref="E:System.Windows.Forms.Form.Activated" /> event when <see cref="P:System.Windows.Forms.Form.WindowState" /> is set to <see cref="F:System.Windows.Forms.FormWindowState.Maximized" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is <see langword="null"/></exception>
        /// <exception cref="OverflowException">The number of elements in <paramref name="source" /> is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> is not in the correct format. </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is less than 0.-or-<paramref name="start" /> + <paramref name="count" /> -1 is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
        /// <exception cref="AggregateException">At least one of the <see cref="T:System.Threading.Tasks.Task" /> instances was canceled. If a task was canceled, the <see cref="T:System.AggregateException" /> exception contains an <see cref="T:System.OperationCanceledException" /> exception in its <see cref="P:System.AggregateException.InnerExceptions" /> collection.-or-An exception was thrown during the execution of at least one of the <see cref="T:System.Threading.Tasks.Task" /> instances. </exception>
        /// <exception cref="IndexOutOfRangeException"><paramref name="dimension" /> is less than zero.-or-<paramref name="dimension" /> is equal to or greater than <see cref="P:System.Array.Rank" />.</exception>
        [STAThread]
        public static void Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            if (args == null) throw new ArgumentNullException(nameof(args));

            var tt = new List<Tuple<double, Color>>
            {
                new Tuple<double, Color>(0.0, Color.FromArgb(255, 0, 7, 100)),
                new Tuple<double, Color>(0.16, Color.FromArgb(255, 32, 107, 203)),
                new Tuple<double, Color>(0.42, Color.FromArgb(255, 237, 255, 255)),
                new Tuple<double, Color>(0.6425, Color.FromArgb(255, 255, 170, 0)),
                new Tuple<double, Color>(0.8575, Color.FromArgb(255, 0, 2, 0)),
                new Tuple<double, Color>(1.0, Color.FromArgb(255, 0, 7, 100))
            };
            int width = 1840, height = 1000;
            if (args.Length > 0)
            {
                width = int.Parse(args[0]);
                height = int.Parse(args[1]);
            }
            var gradient = new Program.Gradient(256, 0);
            var palette = Palette.GenerateColorPalette(tt, 768);
            var display = new Display
            {
                _bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb)
            };
            var bmp = (Bitmap)display._bmp;
            var img = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, display._bmp.PixelFormat);
            var scan = img.Width;
            var depth = Image.GetPixelFormatSize(img.PixelFormat) / 8; //bytes per pixel
            var buffer = new byte[img.Width * img.Height * depth];
            buffer = Program.DrawMandelbrot(new Size(width, height),
                MathUtilities.PackBounds(-2.5, 1, -1, 1),
                1000, palette, buffer, scan, gradient, 1E10);
            CopyArrayToBitmap(width, height, depth, buffer, img);
            bmp.UnlockBits(img);
            stopWatch.Stop();
            var p = new PictureBox { Size = display._bmp.Size };
            var form = new Form
            {
                Name = "Mandelbrot Display",
                Visible = false,
                AutoSize = true,
                Size = display._bmp.Size,
                KeyPreview = true
            };
            form.KeyPress += display.Form_KeyPress;
            form.KeyDown += display.Form_KeyDown;
            EventHandler resizeHandler = (sender, e) =>
            {
                var size = form.Size;
                form.Visible = false;
                form.Close();
                Main(new[] { size.Width.ToString(), size.Height.ToString() });
            };
            form.ResizeEnd += resizeHandler;
            p.KeyPress += display.Form_KeyPress;
            p.KeyDown += display.Form_KeyDown;
            p.Image = display._bmp;
            form.Controls.Add(p);
            form.Icon = Icon.ExtractAssociatedIcon("Image.ico");
            form.Text = $"Mandelbrot Set - Rendered in : {stopWatch.Elapsed}";
            form.ShowDialog();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.S) return;
            SaveBitmap();
            e.Handled = true;
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != 'S') return;
            SaveBitmap();
            e.Handled = true;
        }

        /// <summary>
        /// </summary>
        private void SaveBitmap()
        {
            _bmp.Save("Image.png", ImageFormat.Png);
        }

        /// <summary>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="depth"></param>
        /// <param name="buffer"></param>
        /// <param name="img"></param>
        private static void CopyArrayToBitmap(int width, int height, int depth, byte[] buffer, BitmapData img)
        {
            var arrRowLength = width * depth;
            var ptr = img.Scan0;
            for (var i = 0; i < height; i++)
            {
                Marshal.Copy(buffer, i * arrRowLength, ptr, arrRowLength);
                ptr += img.Stride;
            }
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            ((IDisposable)_bmp).Dispose();
        }

        #endregion
    }

    /// <summary>
    /// </summary>
    public static class MathUtilities
    {
        /// <summary>
        /// </summary>
        public enum ExtrapolationType
        {
            Linear,
            Constant,
            None
        }

        /// <summary>
        /// </summary>
        /// <param name="height"></param>
        /// <param name="region"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Tuple<int, int, int, int> ConvertRegionToPixelCoordinates(int width, int height, Program.Region region)
        {
            var area = UnpackRegion(region);
            double rMin = area.Item1, rMax = area.Item2, iMin = area.Item3, iMax = area.Item4;
            var scale = Scale(width, height, rMin, rMax, iMin, iMax);
            var startPixelCoordinates = ArgandToPixelCoordinates(region.Min, scale.Item1, scale.Item2, rMin, iMin);
            var endPixelCoordinates = ArgandToPixelCoordinates(region.Max, scale.Item1, scale.Item2, rMin, iMin);
            return new Tuple<int, int, int, int>(startPixelCoordinates.Item1, endPixelCoordinates.Item1, startPixelCoordinates.Item1, endPixelCoordinates.Item2);
        }
        /// <summary>
        /// </summary>
        /// <param name="matrix"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"><paramref name="dimension" /> is less than zero.-or-<paramref name="dimension" /> is equal to or greater than <see cref="P:System.Array.Rank" />.</exception>
        /// <exception cref="OverflowException">The array is multidimensional and contains more than <see cref="F:System.Int32.MaxValue" /> elements.</exception>
        public static T[] Linearize<T>(T[,] matrix)
        {
            var flat = new T[matrix.Length];
            for (int j = 0, k = 0; j < matrix.GetLength(0); ++j)
            {
                for (var i = 0; i < matrix.GetLength(1); ++i)
                {
                    flat[k++] = matrix[j, i];
                }
            }
            return flat;
        }
        /// <summary>
        /// Generates the coordinates bounding the rectangle a thread should
        /// render.
        /// </summary>
        /// <param name="startX">The overall starting x coordinate</param>
        /// <param name="endX">The overall ending x coordinate</param>
        /// <param name="startY">The overall starting y coordinate</param>
        /// <param name="endY">The overall ending y coordinate</param>
        /// <param name="nx">The number of threads in the x direction</param>
        /// <param name="ix">The index of the current thread in the x direction
        /// </param>
        /// <param name="ny">The number of threads in the x direction</param>
        /// <param name="iy">The index of the current thread in the y direction
        /// </param>
        /// <returns>(startX, endX, startY, endY)</returns>
        public static Tuple<int, int, int, int> StartEndCoordinates(int startX, int endX, int startY, int endY, int nx, int ix, int ny, int iy)
        {
            //for multithreading purposes
            int xDist = (int)Math.Round((float)(endX - startX) / nx), yDist = (int)Math.Round((float)(endY - startY) / ny);
            if (ix == (nx - 1))
            {
                startX += (nx - 1) * xDist;
            }
            else
            {
                startX += ix * xDist;
                endX = (ix + 1) * xDist;
            }
            if (iy == (ny - 1))
            {
                startY += (ny - 1) * yDist;
            }
            else
            {
                startY += iy * yDist;
                endY = (iy + 1) * yDist;
            }
            return new Tuple<int, int, int, int>(startX, endX, startY, endY);
        }

        /// <summary>
        /// </summary>
        /// <param name="argand"></param>
        /// <param name="iScale"></param>
        /// <param name="rMin"></param>
        /// <param name="rScale"></param>
        /// <param name="iMin"></param>
        /// <returns>(x,y)</returns>
        public static Tuple<int, int> ArgandToPixelCoordinates(Complex argand, double rScale, double iScale, double rMin, double iMin) =>
            new Tuple<int, int>((int)((argand.Real - rMin) / rScale), (int)((argand.Imaginary - iMin) / iScale));

        /// <summary>
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Program.Region ConvertPixelCoordinatesToRegion(Tuple<int, int, int, int> rectangle, Complex scale) =>
            ConvertPixelCoordinatesToRegion(rectangle, scale, Complex.Zero);

        /// <summary>
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="scale"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Program.Region ConvertPixelCoordinatesToRegion(Tuple<int, int, int, int> rectangle, Complex scale, Complex offset)
        {
            int startX = rectangle.Item1, endX = rectangle.Item2, startY = rectangle.Item3, endY = rectangle.Item4;
            double rMin = startX * scale.Real + offset.Real,
                rMax = endX * scale.Real + offset.Real,
                iMin = startY * scale.Imaginary + offset.Imaginary,
                iMax = endY * scale.Imaginary + offset.Imaginary;
            return PackBounds(rMin, rMax, iMin, iMax);
        }
        /// <summary>
        /// </summary>
        public const double Tolerance = 1E-15;

        /// <exception cref="ArgumentException">Condition.</exception>
        /// <exception cref="OverflowException">The array is multidimensional and contains more than <see cref="F:System.Int32.MaxValue" /> elements.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than 0.-or-<paramref name="index" /> is equal to or greater than <see cref="P:System.Collections.Generic.List`1.Count" />. </exception>
        public static Func<double, double> CreateInterpolant(double[] xs, double[] ys,
            ExtrapolationType extrapolationType)
        {
            var length = xs.Length;
            if (length != ys.Length)
            {
                throw new ArgumentException(message: $"length of xs ({length}) must be equal to length of ys ({ys.Length})");
            }
            if (length == 0)
            {
                return x => 0.0;
            }
            if (length == 1)
            {
                return x => ys[0];
            }
            var indices = Enumerable.Range(0, length).ToList();
            indices.Sort((a, b) => xs[a] < xs[b] ? -1 : 1);
            double[] newXs = new double[xs.Length], newYs = new double[ys.Length];
            for (var i = 0; i < length; i++)
            {
                newXs[i] = xs[indices[i]];
                newYs[i] = ys[indices[i]];
            }
            // Get consecutive differences and slopes
            double[] dxs = new double[length - 1], ms = new double[length - 1];
            for (var i = 0; i < length - 1; i++)
            {
                double dx = newXs[i + 1] - newXs[i], dy = newYs[i + 1] - newYs[i];
                dxs[i] = dx;
                ms[i] = dy / dx;
            }
            // Get degree-1 coefficients
            var c1S = new double[dxs.Length + 1];
            c1S[0] = ms[0];
            for (var i = 0; i < dxs.Length - 1; i++)
            {
                double m = ms[i], mNext = ms[i + 1];
                if (m * mNext <= 0)
                {
                    c1S[i + 1] = 0;
                }
                else
                {
                    double dx = dxs[i], dxNext = dxs[i + 1], common = dx + dxNext;
                    c1S[i + 1] = (3 * common / ((common + dxNext) / m + (common + dx) / mNext));
                }
            }
            c1S[c1S.Length - 1] = (ms[ms.Length - 1]);
            // Get degree-2 and degree-3 coefficients
            double[] c2S = new double[c1S.Length - 1], c3S = new double[c1S.Length - 1];
            for (var i = 0; i < c1S.Length - 1; i++)
            {
                double c1 = c1S[i], m = ms[i], invDx = 1 / dxs[i], common = c1 + c1S[i + 1] - m - m;
                c2S[i] = (m - c1 - common) * invDx;
                c3S[i] = common * invDx * invDx;
            }
            // Return interpolant function
            Func<double, double> interpolant = x =>
            {
                // The rightmost point in the dataset should give an exact result
                var i = newXs.Length - 1;
                if (Math.Abs(x - newXs[i]) < Tolerance)
                {
                    return newYs[i];
                }
                // Search for the interval x is in, returning the corresponding y if x is one of the original newXs
                var low = 0;
                var high = c3S.Length - 1;
                while (low <= high)
                {
                    var mid = low + ((high - low) / 2);
                    var xHere = newXs[mid];
                    if (xHere < x)
                    {
                        low = mid + 1;
                    }
                    else if (xHere > x)
                    {
                        high = mid - 1;
                    }
                    else
                    {
                        return newYs[mid];
                    }
                }
                i = Math.Max(0, high);
                // Interpolate
                double diff = x - newXs[i], diffSq = diff * diff;
                return newYs[i] + c1S[i] * diff + c2S[i] * diffSq + c3S[i] * diff * diffSq;
            };
            double xMin = newXs.Min(),
                xMax = newXs.Max(),
                yMin = newYs.Min(),
                yMax = newYs.Max();
            return x =>
            {
                if (x >= xMin && x <= xMax)
                {
                    return interpolant(x);
                }
                switch (extrapolationType)
                {
                    case ExtrapolationType.None:
                        return interpolant(x);
                    case ExtrapolationType.Linear:
                        return yMin + (x - xMin) / (xMax - xMin) * yMax;
                    case ExtrapolationType.Constant:
                        return (x < xMin) ? yMin : yMax;
                    default:
                        return double.NaN;
                }
            };
        }

        /// <summary>
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static double Clamp(double val, double min, double max)
        {
            return (val < min) ? min : ((val > max) ? max : val);
        }

        /// <summary>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rMin"></param>
        /// <param name="rMax"></param>
        /// <param name="iMin"></param>
        /// <param name="iMax"></param>
        /// <returns></returns>
        public static Tuple<double, double> Scale(int width, int height, double rMin, double rMax, double iMin, double iMax)
        {
            var rScale = (Math.Abs(rMin) + Math.Abs(rMax)) / width; // Amount to move each pixel in the real numbers
            var iScale = (Math.Abs(iMin) + Math.Abs(iMax)) / height; // Amount to move each pixel in the imaginary numbers
            return new Tuple<double, double>(rScale, iScale);
        }

        /// <summary>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="rScale"></param>
        /// <param name="iScale"></param>
        /// <param name="rMin"></param>
        /// <param name="iMin"></param>
        /// <returns></returns>
        public static Complex PixelToArgandCoordinates(int x, int y, double rScale, double iScale, double rMin, double iMin) =>
            new Complex(x * rScale + rMin, y * iScale + iMin);

        /// <summary>
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static Tuple<double, double, double, double> UnpackRegion(Program.Region region)
        {
            Complex max = region.Max, min = region.Min;
            double rMin = min.Real, rMax = max.Real, iMax = max.Imaginary, iMin = min.Imaginary;
            return new Tuple<double, double, double, double>(rMin, rMax, iMin, iMax);
        }

        /// <summary>
        /// </summary>
        /// <param name="rMin"></param>
        /// <param name="rMax"></param>
        /// <param name="iMin"></param>
        /// <param name="iMax"></param>
        /// <returns></returns>
        public static Program.Region PackBounds(double rMin, double rMax, double iMin, double iMax) =>
            new Program.Region(new Complex(rMin, iMin), new Complex(rMax, iMax));
    }

    /// <summary>
    /// </summary>
    public static class Palette
    {
        /// <summary>
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="numColors"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="red" />, <paramref name="green" />, or <paramref name="blue" /> is less than 0 or greater than 255.</exception>
        /// <exception cref="OverflowException">The number of elements in <paramref name="source" /> is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is less than 0.-or-<paramref name="start" /> + <paramref name="count" /> -1 is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
        public static Color[] GenerateColorPalette(List<Tuple<double, Color>> controls, int numColors)
        {
            var palette = new Color[numColors];

            double[] red = controls.Select(x => x.Item2.R / 255.0).ToArray(),
                green = controls.Select(x => x.Item2.G / 255.0).ToArray(),
                blue = controls.Select(x => x.Item2.B / 255.0).ToArray(),
                xs = controls.Select(x => x.Item1).ToArray();
            var channelSplines = new[]
            {
                MathUtilities.CreateInterpolant(xs, red, MathUtilities.ExtrapolationType.None),
                MathUtilities.CreateInterpolant(xs, green, MathUtilities.ExtrapolationType.None),
                MathUtilities.CreateInterpolant(xs, blue, MathUtilities.ExtrapolationType.None)
            };
            Enumerable.Range(0, palette.Length).ToList().ForEach(i => palette[i] = (Color.FromArgb(
                (int)MathUtilities.Clamp(Math.Abs((channelSplines[0]((double)i / palette.Count()) * 255.0)), 0.0, 255.0),
                (int)MathUtilities.Clamp(Math.Abs((channelSplines[1]((double)i / palette.Count()) * 255.0)), 0.0, 255.0),
                (int)MathUtilities.Clamp(Math.Abs((channelSplines[2]((double)i / palette.Count()) * 255.0)), 0.0, 255.0)
                )));
            return palette;
        }

        /// <summary>
        /// </summary>
        /// <param name="fromColor"></param>
        /// <param name="toColor"></param>
        /// <param name="bias"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"><paramref name="red" />, <paramref name="green" />, or <paramref name="blue" /> is less than 0 or greater than 255.</exception>
        public static Color Lerp(Color fromColor, Color toColor, double bias)
        {
            bias = (double.IsNaN(bias)) ? 0 : (double.IsInfinity(bias) ? 1 : bias);
            return Color.FromArgb(
                (int)(fromColor.R * (1.0f - bias) + toColor.R * (bias)),
                (int)(fromColor.G * (1.0f - bias) + toColor.G * (bias)),
                (int)(fromColor.B * (1.0f - bias) + toColor.B * (bias))
                );
        }

        /// <summary>
        /// </summary>
        private const int Depth = 3;

        /// <summary>
        /// </summary>
        /// <param name="pixels"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scan"></param>
        /// <param name="color"></param>
        public static void SetPixel(byte[] pixels, int x, int y, int scan, Color color)
        {
            var offset = Depth * ((y * scan) + x);//for 24-bit RGB
            pixels[offset + 0] = color.B;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.R;
        }

        /// <summary>
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="palette"></param>
        /// <returns></returns>
        /// <exception cref="Exception">The operation failed.</exception>
        private static Bitmap PaletteDebug(int height, int width, List<Color> palette)
        {
            var paletteLength = palette.Count();
            width = Math.Min(width, paletteLength);
            var img = new Bitmap(width, height);
            var paletteStep = paletteLength / width;
            for (var y = 0; y < img.Height; y++)
            {
                var paletteIndex = 0;
                for (var x = 0; x < img.Width; x++)
                {
                    paletteIndex += paletteStep;
                    if (paletteIndex >= paletteLength)
                    {
                        break;
                    }
                    img.SetPixel(x, y, palette[paletteIndex]);
                }
            }
            return img;
        }
    }

    /// <summary>
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// </summary>
        private static readonly double OneOverLog2 = 1 / Math.Log(2);

        /// <summary>
        /// </summary>
        /// <param name="threads"></param>
        /// <param name="size"></param>
        /// <param name="region"></param>
        /// <param name="regionMain"></param>
        /// <param name="maxIteration"></param>
        /// <param name="palette"></param>
        /// <param name="img"></param>
        /// <param name="scan"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The <paramref name="action" /> argument is null.</exception>
        /// <exception cref="AggregateException">At least one of the <see cref="T:System.Threading.Tasks.Task" /> instances was canceled. If a task was canceled, the <see cref="T:System.AggregateException" /> exception contains an <see cref="T:System.OperationCanceledException" /> exception in its <see cref="P:System.AggregateException.InnerExceptions" /> collection.-or-An exception was thrown during the execution of at least one of the <see cref="T:System.Threading.Tasks.Task" /> instances. </exception>
        /// <exception cref="ObjectDisposedException">One or more of the <see cref="T:System.Threading.Tasks.Task" /> objects in <paramref name="tasks" /> has been disposed.</exception>
        /// <exception cref="OverflowException">The array is multidimensional and contains more than <see cref="F:System.Int32.MaxValue" /> elements.</exception>
        /// <exception cref="IndexOutOfRangeException"><paramref name="dimension" /> is less than zero.-or-<paramref name="dimension" /> is equal to or greater than <see cref="P:System.Array.Rank" />.</exception>
        public static byte[] DrawMandelbrotParallel(Size threads, Size size, Region region, Region regionMain, int maxIteration, Color[] palette,
            byte[] img, int scan,
            Gradient gradient, double bailout)
        {
            var area = MathUtilities.ConvertRegionToPixelCoordinates(size.Width, size.Height, region);
            return DrawMandelbrotParallel(threads, new Size(Math.Abs(area.Item2 - area.Item1), Math.Abs(area.Item4 - area.Item3)),
                new Size(area.Item1, area.Item3), regionMain, maxIteration, palette, img, scan, gradient, bailout);
        }

        /// <summary>
        /// </summary>
        /// <param name="threads"></param>
        /// <param name="size"></param>
        /// <param name="region"></param>
        /// <param name="maxIteration"></param>
        /// <param name="palette"></param>
        /// <param name="img"></param>
        /// <param name="scan"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The <paramref name="action" /> argument is null.</exception>
        /// <exception cref="AggregateException">At least one of the <see cref="T:System.Threading.Tasks.Task" /> instances was canceled. If a task was canceled, the <see cref="T:System.AggregateException" /> exception contains an <see cref="T:System.OperationCanceledException" /> exception in its <see cref="P:System.AggregateException.InnerExceptions" /> collection.-or-An exception was thrown during the execution of at least one of the <see cref="T:System.Threading.Tasks.Task" /> instances. </exception>
        /// <exception cref="ObjectDisposedException">One or more of the <see cref="T:System.Threading.Tasks.Task" /> objects in <paramref name="tasks" /> has been disposed.</exception>
        /// <exception cref="OverflowException">The array is multidimensional and contains more than <see cref="F:System.Int32.MaxValue" /> elements.</exception>
        /// <exception cref="IndexOutOfRangeException"><paramref name="dimension" /> is less than zero.-or-<paramref name="dimension" /> is equal to or greater than <see cref="P:System.Array.Rank" />.</exception>
        public static byte[] DrawMandelbrotParallel(Size threads, Size size, Region region, int maxIteration, Color[] palette, byte[] img, int scan,
            Gradient gradient, double bailout)
        => DrawMandelbrotParallel(threads, size, new Size(0, 0), region, maxIteration, palette, img, scan, gradient, bailout);

        /// <summary>
        /// </summary>
        /// <param name="threads"></param>
        /// <param name="size"></param>
        /// <param name="start"></param>
        /// <param name="region"></param>
        /// <param name="maxIteration"></param>
        /// <param name="palette"></param>
        /// <param name="img"></param>
        /// <param name="scan"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The <paramref name="action" /> argument is null.</exception>
        /// <exception cref="AggregateException">At least one of the <see cref="T:System.Threading.Tasks.Task" /> instances was canceled. If a task was canceled, the <see cref="T:System.AggregateException" /> exception contains an <see cref="T:System.OperationCanceledException" /> exception in its <see cref="P:System.AggregateException.InnerExceptions" /> collection.-or-An exception was thrown during the execution of at least one of the <see cref="T:System.Threading.Tasks.Task" /> instances. </exception>
        /// <exception cref="ObjectDisposedException">One or more of the <see cref="T:System.Threading.Tasks.Task" /> objects in <paramref name="tasks" /> has been disposed.</exception>
        /// <exception cref="OverflowException">The array is multidimensional and contains more than <see cref="F:System.Int32.MaxValue" /> elements.</exception>
        /// <exception cref="IndexOutOfRangeException"><paramref name="dimension" /> is less than zero.-or-<paramref name="dimension" /> is equal to or greater than <see cref="P:System.Array.Rank" />.</exception>
        [SuppressMessage("ReSharper", "AccessToModifiedClosure")]
        public static byte[] DrawMandelbrotParallel(Size threads, Size size, Size start, Region region, int maxIteration, Color[] palette, byte[] img, int scan,
            Gradient gradient, double bailout)
        {
            int xThreads = threads.Width, yThreads = threads.Height;
            var tasks = new Task[xThreads, yThreads];
            for (var ix = 0; ix < xThreads; ix++)
            {
                for (var iy = 0; iy < yThreads; iy++)
                {
                    tasks[ix, iy] = Task.Factory.StartNew(() =>
                    {
                        var area = MathUtilities.StartEndCoordinates(start.Width, size.Width, start.Height, size.Height, xThreads, ix, yThreads, iy);
                        int startX = area.Item1, startY = area.Item3, endX = area.Item2, endY = area.Item4;
                        int width = endX - startX, height = endY - startY;

                        var scale = MathUtilities.Scale(size.Width, size.Height, region.Min.Real, region.Max.Real,
                            region.Min.Imaginary, region.Max.Imaginary);
                        var min = MathUtilities.PixelToArgandCoordinates(startX, startY, scale.Item1, scale.Item2, region.Min.Real, region.Min.Imaginary);
                        var max = MathUtilities.PixelToArgandCoordinates(endX, endY, scale.Item1, scale.Item2, region.Min.Real, region.Min.Imaginary);

                        double rMin = min.Real, rMax = max.Real, iMin = min.Imaginary, iMax = max.Imaginary;
                        DrawMandelbrot(width, height, startX, startY, rMin, rMax, iMin, iMax, maxIteration, palette, img, scan, gradient, bailout);
                    }
                        );
                }
            }
            var tasksLinear = MathUtilities.Linearize(tasks);
            Task.WaitAll(tasksLinear);
            return img;
        }

        /// <summary>
        /// </summary>
        /// <param name="size"></param>
        /// <param name="region"></param>
        /// <param name="maxIteration"></param>
        /// <param name="palette"></param>
        /// <param name="img"></param>
        /// <param name="scan"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        public static byte[] DrawMandelbrot(Size size, Region region, int maxIteration, Color[] palette, byte[] img, int scan,
            Gradient gradient, double bailout)
        => DrawMandelbrot(size, new Size(0, 0), region, maxIteration, palette, img, scan, gradient, bailout);


        /// <summary>
        /// </summary>
        /// <param name="size"></param>
        /// <param name="start"></param>
        /// <param name="region"></param>
        /// <param name="maxIteration"></param>
        /// <param name="palette"></param>
        /// <param name="img"></param>
        /// <param name="scan"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        public static byte[] DrawMandelbrot(Size size, Size start, Region region, int maxIteration, Color[] palette, byte[] img, int scan,
            Gradient gradient, double bailout)
        {
            region = region.NormalizeRegion();
            var bounds = MathUtilities.UnpackRegion(region);
            return DrawMandelbrot(size.Width, size.Height, start.Width, start.Height, bounds.Item1, bounds.Item2, bounds.Item3, bounds.Item4, maxIteration, palette,
                img, scan, gradient, bailout);
        }

        /// <summary>
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="startY"></param>
        /// <param name="rMin"></param>
        /// <param name="rMax"></param>
        /// <param name="iMin"></param>
        /// <param name="iMax"></param>
        /// <param name="maxIteration"></param>
        /// <param name="palette"></param>
        /// <param name="img"></param>
        /// <param name="scan"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <param name="startX"></param>
        /// <returns></returns>
        private static byte[] DrawMandelbrot(int width, int height, int startX, int startY, double rMin, double rMax, double iMin, double iMax,
            int maxIteration,
            IReadOnlyList<Color> palette, byte[] img, int scan, Gradient gradient, double bailout)
        {
            var logBailout = Math.Log(bailout);
            var bailoutSquared = bailout * bailout;
            var scale = MathUtilities.Scale(width, height, rMin, rMax, iMin, iMax);
            double rScale = scale.Item1, iScale = scale.Item2;
            for (var px = startX; px < width; px++)
            {
                for (var py = startY; py < height; py++)
                {
                    double x = 0.0, xp = 0.0; // x;
                    double y = 0.0, yp = 0.0; // y;
                    var mod = 0.0; // x * x + y * y;
                    var iteration = 0;
                    while (mod < bailoutSquared && iteration < maxIteration)
                    {
                        var z = MathUtilities.PixelToArgandCoordinates(px, py, rScale, iScale, rMin, iMin);
                        double x0 = z.Real, y0 = z.Imaginary;
                        var xtemp = x * x - y * y + x0;
                        var ytemp = 2 * x * y + y0;
                        if ((Math.Abs(xtemp - x) < MathUtilities.Tolerance && Math.Abs(ytemp - y) < MathUtilities.Tolerance) ||
                            (Math.Abs(xtemp - xp) < MathUtilities.Tolerance && Math.Abs(ytemp - yp) < MathUtilities.Tolerance))
                        {
                            iteration = maxIteration;
                            break;
                        }
                        xp = x;
                        yp = y;
                        x = xtemp;
                        y = ytemp;
                        mod = x * x + y * y;
                        ++iteration;
                    }
                    var size = Math.Sqrt(mod);
                    var smoothed = Math.Log(Math.Log(size) * logBailout) * OneOverLog2;
                    var bias = smoothed - (long)smoothed;
                    var idx = (Math.Sqrt(iteration + 1 - smoothed) * gradient.Scale + gradient.Shift) % palette.Count;
                    idx = NormalizeIdx(idx, palette.Count);
                    if (gradient.LogIndex)
                    {
                        idx = ((Math.Log(idx) / Math.Log(palette.Count)) * gradient.Scale) % palette.Count;
                        idx = NormalizeIdx(idx, palette.Count);
                    }
                    var color = Palette.Lerp(palette[(int)idx], palette[((int)idx + 1) % palette.Count], bias);
                    Palette.SetPixel(img, px, py, scan, color);
                }
            }
            return img;
        }

        /// <summary>
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static double NormalizeIdx(double idx, double max)
        {
            return (idx < 0)
                ? Math.Abs((max + idx)) % max
                : (double.IsNaN(idx) ? 0 : (double.IsInfinity(idx) ? max : idx));
        }

        /// <summary>
        /// </summary>
        public struct Region

        {
            /// <summary>
            /// </summary>
            /// <returns></returns>
            public Region NormalizeRegion()
            {
                return OriginAndWidth ? new Region(OriginAndWidthToRegion()) : this;
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            private Tuple<Complex, Complex> OriginAndWidthToRegion()
            {
                var origin = Min;
                var halfX = Max.Real / 2;
                var halfY = Max.Imaginary / 2;
                return new Tuple<Complex, Complex>(
                    new Complex(origin.Real - halfX, origin.Imaginary - halfY),
                    new Complex(origin.Real + halfX, origin.Imaginary + halfY));
            }

            /// <summary>
            /// </summary>
            /// <param name="val"></param>
            private Region(Tuple<Complex, Complex> val) : this(val.Item1, val.Item2)
            {
            }

            /// <summary>
            /// </summary>
            /// <param name="min"></param>
            /// <param name="max"></param>
            /// <param name="originAndWidth"></param>
            public Region(Complex min, Complex max, bool originAndWidth = false)
            {
                Max = max;
                Min = min;
                OriginAndWidth = originAndWidth;
            }

            /// <summary>
            /// </summary>
            public Complex Min { get; }

            /// <summary>
            /// </summary>
            public Complex Max { get; }

            /// <summary>
            /// </summary>
            private bool OriginAndWidth { get; }


            /// <summary>
            /// </summary>
            /// <param name="load"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentNullException"><paramref name="s" /> is null. </exception>
            /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format. </exception>
            /// <exception cref="OverflowException"><paramref name="s" /> represents a number that is less than <see cref="F:System.Double.MinValue" /> or greater than <see cref="F:System.Double.MaxValue" />. </exception>
            public static Region FromString(string load)
            {
                var data = load.Split(' ');
                var min = data[0].Split(',');
                var max = data[1].Split(',');
                return new Region(new Complex(double.Parse(min[0]), double.Parse(min[1])),
                    new Complex(double.Parse(max[0]), double.Parse(max[1])), bool.Parse(data[2]));
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public override string ToString() => $"Min: {Min}, Max: {Max}, OriginAndWidth: {OriginAndWidth}";
        }

        /// <summary>
        /// </summary>
        public struct Gradient : IEquatable<Gradient>
        {
            /// <summary>
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Equals(Gradient other)
            {
                return Scale.Equals(other.Scale) && Shift.Equals(other.Shift) && LogIndex == other.LogIndex;
            }

            /// <summary>
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return (!ReferenceEquals(null, obj)) && obj is Gradient && Equals((Gradient)obj);
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                var hashCode = Scale.GetHashCode();
                hashCode = (hashCode * 397) ^ Shift.GetHashCode();
                hashCode = (hashCode * 397) ^ LogIndex.GetHashCode();
                return hashCode;
            }

            /// <summary>
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <returns></returns>
            public static bool operator ==(Gradient left, Gradient right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// </summary>
            /// <param name="left"></param>
            /// <param name="right"></param>
            /// <returns></returns>
            public static bool operator !=(Gradient left, Gradient right)
            {
                return !left.Equals(right);
            }

            /// <summary>
            /// </summary>
            /// <param name="load"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentNullException"><paramref name="s" /> is null. </exception>
            /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format. </exception>
            /// <exception cref="OverflowException"><paramref name="s" /> represents a number that is less than <see cref="F:System.Double.MinValue" /> or greater than <see cref="F:System.Double.MaxValue" />. </exception>
            public static Gradient FromString(string load)
            {
                var data = load.Split(' ');
                return new Gradient(double.Parse(data[0]), double.Parse(data[1]), bool.Parse(data[2]));
            }

            /// <summary>
            /// </summary>
            /// <returns></returns>
            public override string ToString() => $"Scale: {Scale}, Logindex: {LogIndex}, Shift: {Shift}";

            /// <summary>
            /// </summary>
            /// <param name="scale"></param>
            /// <param name="shift"></param>
            /// <param name="logIndex"></param>
            public Gradient(double scale, double shift, bool logIndex = false)
            {
                Scale = scale;
                Shift = shift;
                LogIndex = logIndex;
            }

            /// <summary>
            /// </summary>
            public double Scale { get; }

            /// <summary>
            /// </summary>
            public double Shift { get; }

            /// <summary>
            /// </summary>
            public bool LogIndex { get; }
        }
    }
}