using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
            if (args == null) throw new ArgumentNullException(nameof(args));

            /*
             * Notes for making palettes -
             * We generally want the interior of the set to map to black
             */
            var initialPalette = new[]
            {
                new Tuple<double, Color>(0.0, Color.FromArgb(255, 0, 7, 100)),
                new Tuple<double, Color>(0.16, Color.FromArgb(255, 32, 107, 203)),
                new Tuple<double, Color>(0.42, Color.FromArgb(255, 237, 255, 255)),
                new Tuple<double, Color>(0.6425, Color.FromArgb(255, 255, 170, 0)),
                new Tuple<double, Color>(0.8575,  Color.FromArgb(255, 0, 2, 0)),
                new Tuple<double, Color>(1.0, Color.FromArgb(255, 0, 7, 100))
            };
            int width = 3840, height = 2160, numColors = 768, maxIterations = 256;
            var palette = Palette.GenerateColorPalette(initialPalette, numColors);
            var palette2 = new[]{
                Color.FromArgb(66, 30, 15),
                Color.FromArgb(25, 37, 26),
                Color.FromArgb(9, 1, 47),
                Color.FromArgb(4, 4, 73),
                Color.FromArgb(0, 7, 100),
                Color.FromArgb(12, 44, 138),
                Color.FromArgb(24, 82, 177),
                Color.FromArgb(57, 125, 209),
                Color.FromArgb(134, 181, 229),
                Color.FromArgb(211, 236, 248),
                Color.FromArgb(241, 233, 191),
                Color.FromArgb(248, 201, 95),
                Color.FromArgb(255, 170, 0),
                Color.FromArgb(204, 128, 0),
                Color.FromArgb(153, 87, 0),
                Color.FromArgb(106, 52, 3),
                Color.FromArgb(0, 0, 0)
            };
            /*
             * Note that the way scaleDownFactor is calculated will ensure that 
             * `gradient.PaletteScale` is such that the highest ieration counts will map to Black.
             * 
             * To change the frequency of colors in the output,
             * change `gradient.IndexScale` in proportion to the frquency, as necessary.
             */
            var scaleDownFactor = Palette.CalculateScaleDownFactorForLinearMapping(Palette.FindPaletteColorLocation(palette, Color.Black));
            var root = 4.0;
            if (args.Length > 0)
            {
                int.TryParse(args[0], out width);
                int.TryParse(args[1], out height);
                if (args.Length > 2)
                {
                    int.TryParse(args[2], out maxIterations);
                }
                if (args.Length > 3)
                {
                   int.TryParse(args[3], out numColors);
                }
            }

            var maxIterationColor = Color.Black;
            var gradient = new Gradient(
                maxIterationColor,
                Palette.RecommendedGradientScale(palette.Length, true, scaleDownFactor),
                0, 1E10,
                logIndex: true, rootIndex: false,
                root: root, minIterations: 1,
                indexScale: 1,  weight: 1.0);
            using (var paletteOutput = new StreamWriter("./palette.txt"))
            {
                var totalPalette = new Color[palette.Length + 1];
                totalPalette[0] = maxIterationColor;
                palette.CopyTo(totalPalette, 1);
                paletteOutput.WriteLine(numColors);
                paletteOutput.Write(Palette.PaletteToString(totalPalette));
            }
            var display = new Display
            {
                _bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb)
            };
            var bmp = (Bitmap)display._bmp;
            var img = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, display._bmp.PixelFormat);
            var depth = Image.GetPixelFormatSize(img.PixelFormat) / 8; //bytes per pixel
            //var region = new Region(new Complex(-0.1593247826659642, 1.0342115878556377), new Complex(0.0325, 0.0325), originAndWidth: true);
            //var region = new Region(new Complex(0.27969303810093984, 0.00838423653868096), new Complex(3.27681E-12, 3.27681E-12), originAndWidth: true);
            var region = new Region(new Complex(-2.5, -1), new Complex(1, 1), originAndWidth: false);
            stopWatch.Start();
            var buffer =
                Mandelbrot.DrawMandelbrot(
                    new Size(1, 1),
                new Size(width, height),
                region,
                maxIterations, palette, gradient, 1E10);
            stopWatch.Stop();
            CopyArrayToBitmap(width, height, depth, buffer, img);
            bmp.UnlockBits(img);
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
            form.KeyDown += CloseOnEscapePressed;
            form.KeyDown += display.Form_KeyDown;

            void ResizeHandler(object sender, EventArgs e)
            {
                var size = form.Size;
                form.Visible = false;
                form.Close();
                Main(new[] { size.Width.ToString(), size.Height.ToString() });
            }

            void CloseOnEscapePressed(object sender, KeyEventArgs e)
            {
                if (e.KeyCode != Keys.Escape) return;
                form.Close();
                e.Handled = true;
            }

            form.ResizeEnd += ResizeHandler;
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
}