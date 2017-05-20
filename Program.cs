using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
            stopWatch.Start();
            if (args == null) throw new ArgumentNullException(nameof(args));

            var initialPalette = new[]
            {
                new Tuple<double, Color>(0.0, Color.FromArgb(255, 0, 7, 100)),
                new Tuple<double, Color>(0.16, Color.FromArgb(255, 32, 107, 203)),
                new Tuple<double, Color>(0.42, Color.FromArgb(255, 237, 255, 255)),
                new Tuple<double, Color>(0.6425, Color.FromArgb(255, 255, 170, 0)),
                new Tuple<double, Color>(0.8575, Color.FromArgb(255, 0, 2, 0)),
                new Tuple<double, Color>(1.0, Color.FromArgb(255, 0, 7, 100))
            };
            int width = 1840, height = 1000, numColors = 768;
            var scaleDownFactor = 3.0;
            var root = 2.0;
            if (args.Length > 0)
            {
                int.TryParse(args[0], out width);
                int.TryParse(args[1], out height);
                if (args.Length > 2)
                {
                    int.TryParse(args[2], out numColors);
                    scaleDownFactor = numColors / 256.0;
                }
                if (args.Length > 3)
                {
                    double.TryParse(args[3], out root);
                }
            }
            var palette = Palette.GenerateColorPalette(initialPalette, numColors);

            // Note: For Logarithmic mapping, `gradient.PaletteScale` of palette.Length - 1 works well,
            // while for root or linear mapping, `gradient.PaletteScale` of approx. palette,Length / 3 works well.
            var gradient = new Gradient(
                Palette.RecommendedGradientScale(numColors, false, scaleDownFactor), 0,
                logIndex: false, rootIndex: true,
                root: root, minIterations: 0,
                indexScale: 10, weight: 1.0);
            var display = new Display
            {
                _bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb)
            };
            var bmp = (Bitmap)display._bmp;
            var img = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, display._bmp.PixelFormat);
            var depth = Image.GetPixelFormatSize(img.PixelFormat) / 8; //bytes per pixel
            var region = new Region(new Complex(0.16125, 0.637), new Complex(0.001, 0.001), originAndWidth: true);
            //var region = MathUtilities.PackBounds(-2.5, 1, -1, 1);
            var buffer =
                Mandelbrot.DrawMandelbrot(new Size(2, Environment.ProcessorCount),
                new Size(width, height),
                region,
                1000, palette, gradient, 1E10);
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