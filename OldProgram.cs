using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MandleCS
{
    class Program
    {
        static void Main(string[] args)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var bmp = DrawMandelbrot((int)(300 * (1 + 2.5)), 600, -2.5, 1, -1, 1);

            PictureBox P = new PictureBox();
            P.Size = bmp.Size;

            Form form = new Form
            {
                Name = "Screenshot Displayer",

                //Location = new System.Drawing.Point(140, 170),
                Visible = false
                ,
                AutoSize = true
            };
            form.Size = bmp.Size;

            P.Image = bmp;
            form.Controls.Add(P);
            Console.WriteLine(stopWatch.Elapsed);
            form.ShowDialog();
        }

        public class FastBitmap
        {
            public FastBitmap(int width, int height)
            {
                this.Bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            }

            public unsafe void SetPixel(int x, int y, Color color)
            {
                BitmapData data = this.Bitmap.LockBits(new Rectangle(0, 0, this.Bitmap.Width, this.Bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                IntPtr scan0 = data.Scan0;

                byte* imagePointer = (byte*)scan0.ToPointer(); // Pointer to first pixel of image
                int offset = (y * data.Stride) + (3 * x); // 3x because we have 24bits/px = 3bytes/px
                byte* px = (imagePointer + offset); // pointer to the pixel we want
                px[0] = color.B; // Red component
                px[1] = color.G; // Green component
                px[2] = color.R; // Blue component

                this.Bitmap.UnlockBits(data); // Set the data again
            }

            public Bitmap Bitmap
            {
                get;
                set;
            }
        }



        //
        //
        //
        //
        public static List<Color> GenerateColorPalette()
        {

            var tt = new List<Tuple<double, Color>>();
            tt.Add(new Tuple<double, Color>(0.0, Color.FromArgb(255, 0, 7, 100)));
            tt.Add(new Tuple<double, Color>(0.16, Color.FromArgb(255, 32, 107, 203)));
            tt.Add(new Tuple<double, Color>(0.42, Color.FromArgb(255, 237, 255, 255)));
            tt.Add(new Tuple<double, Color>(0.6425, Color.FromArgb(255, 255, 170, 0)));
            tt.Add(new Tuple<double, Color>(0.8575, Color.FromArgb(255, 0, 2, 0)));

            List<Color> retVal = new List<Color>();
            for (int i = 0; i <= 2048; i++)
            {
                retVal.Add(Color.FromArgb(255, i, i, 255));
            }
            return retVal;
        }

        public static List<Color> GenerateColorPaletteOrigl()
        {
            List<Color> retVal = new List<Color>();
            for (int i = 0; i <= 255; i++)
            {
                retVal.Add(Color.FromArgb(255, i, i, 255));
            }
            return retVal;
        }


        public static Bitmap DrawMandelbrot(int width, int height, double rMin, double rMax, double iMin, double iMax)
        {
            List<Color> Palette = GenerateColorPaletteOrigl();
            FastBitmap img = new FastBitmap(width, height); // Bitmap to contain the set

            double rScale = (Math.Abs(rMin) + Math.Abs(rMax)) / width; // Amount to move each pixel in the real numbers
            double iScale = (Math.Abs(iMin) + Math.Abs(iMax)) / height; // Amount to move each pixel in the imaginary numbers

            for (int Px = 0; Px < width; Px++)
            {
                for (int Py = 0; Py < height; Py++)
                {
                    var x0 = Px * rScale + rMin;
                    var y0 = Py * iScale + iMin;
                    var x = 0.0;
                    var y = 0.0;

                    int iteration = 0;
                    int max_iteration = 1000;
                    while ((x * x + y * y) < 2 * 2 && iteration < max_iteration)
                    {
                        var xtemp = x * x - y * y + x0;
                        y = 2 * x * y + y0;
                        x = xtemp;
                        iteration = iteration + 1;
                    }
                    //  color = palette[iteration]
                    //Console.WriteLine(iteration);
                    img.SetPixel(Px, Py, Palette[iteration  *  Palette.Count() / (max_iteration + 1)]);


                    //for (int i = 0; i < Palette.Count; i++) // 255 iterations with the method we already wrote
                    //{
                    //    if (z.Magnitude >= 2.0)
                    //    {
                    //        img.SetPixel(x, y, Palette[i]); // Set the pixel if the magnitude is greater than two
                    //        break; // We're done with this loop
                    //    }
                    //    else
                    //    {
                    //        z = c + Complex.Pow(z, 2); // Z = Zlast^2 + C
                    //    }
                    //}
                }
            }

            return img.Bitmap;
        }
    }
}