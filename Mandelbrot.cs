using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Mandelbrot
{
    public static class Mandelbrot
    {

        /// <summary>
        /// </summary>
        private static readonly double OneOverLog2 = 1 / Math.Log(2);

        /// <summary>
        /// </summary>
        private const double Epsilon = MathUtilities.Tolerance * MathUtilities.Tolerance;

        /// <summary>
        /// </summary>
        private const int BitDepthFor24BppRgb = 3;

        /// <summary>
        /// </summary>
        /// <param name="threads"></param>
        /// <param name="size"></param>
        /// <param name="region"></param>
        /// <param name="maxIteration"></param>
        /// <param name="palette"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The <paramref name="action" /> argument is null.</exception>
        /// <exception cref="AggregateException">At least one of the <see cref="T:System.Threading.Tasks.Task" /> instances was canceled. If a task was canceled, the <see cref="T:System.AggregateException" /> exception contains an <see cref="T:System.OperationCanceledException" /> exception in its <see cref="P:System.AggregateException.InnerExceptions" /> collection.-or-An exception was thrown during the execution of at least one of the <see cref="T:System.Threading.Tasks.Task" /> instances. </exception>
        /// <exception cref="ObjectDisposedException">One or more of the <see cref="T:System.Threading.Tasks.Task" /> objects in <paramref name="tasks" /> has been disposed.</exception>
        /// <exception cref="OverflowException">The array is multidimensional and contains more than <see cref="F:System.Int32.MaxValue" /> elements.</exception>
        /// <exception cref="IndexOutOfRangeException"><paramref name="dimension" /> is less than zero.-or-<paramref name="dimension" /> is equal to or greater than <see cref="P:System.Array.Rank" />.</exception>
        public static byte[] DrawMandelbrot(Size threads, Size size, Region region, int maxIteration, Color[] palette,
            Gradient gradient, double bailout)
            => DrawMandelbrot(threads, new Size(0, 0), size, region, maxIteration, palette, gradient, bailout);

        /// <summary>
        /// </summary>
        /// <param name="threads"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="region"></param>
        /// <param name="maxIterations"></param>
        /// <param name="palette"></param>
        /// <param name="gradient"></param>
        /// <param name="bailout"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">The <paramref name="action" /> argument is null.</exception>
        /// <exception cref="AggregateException">At least one of the <see cref="T:System.Threading.Tasks.Task" /> instances was canceled. If a task was canceled, the <see cref="T:System.AggregateException" /> exception contains an <see cref="T:System.OperationCanceledException" /> exception in its <see cref="P:System.AggregateException.InnerExceptions" /> collection.-or-An exception was thrown during the execution of at least one of the <see cref="T:System.Threading.Tasks.Task" /> instances. </exception>
        /// <exception cref="ObjectDisposedException">One or more of the <see cref="T:System.Threading.Tasks.Task" /> objects in <paramref name="tasks" /> has been disposed.</exception>
        /// <exception cref="OverflowException">The array is multidimensional and contains more than <see cref="F:System.Int32.MaxValue" /> elements.</exception>
        /// <exception cref="IndexOutOfRangeException"><paramref name="dimension" /> is less than zero.-or-<paramref name="dimension" /> is equal to or greater than <see cref="P:System.Array.Rank" />.</exception>
        public static byte[] DrawMandelbrot(Size threads, Size start, Size end, Region region, int maxIterations, Color[] palette,
            Gradient gradient, double bailout)
        {
            region = region.NormalizeRegion();
            int startXAll = Math.Min(start.Width, end.Width), endXAll = Math.Max(start.Width, end.Width);
            int startYAll = Math.Min(start.Height, end.Height), endYAll = Math.Max(start.Height, end.Height);
            int widthAll = endXAll - startXAll, heightAll = endYAll - startYAll;
            var image = new byte[heightAll * widthAll * BitDepthFor24BppRgb];
            int xThreads = threads.Width, yThreads = threads.Height;

            #region GeneralConfigurationForAllThreads
            if (maxIterations < 1) throw new ArgumentException($"Max iterations must be >= 1, is {maxIterations}");
            var colors = palette.Length;
            var root = gradient.Exponent;                                             
            double indexScale = gradient.IndexScale, indexWeight = gradient.Weight;
            double scaledMinIterations = indexScale * gradient.MinIterations,
                scaledMaxIterations = indexScale * maxIterations;
            var rootMinIterations = gradient.RootIndex ? Math.Pow(scaledMinIterations, root) : 0.0;
            var logBase = gradient.LogIndex ? Math.Log(scaledMaxIterations / scaledMinIterations) : 0.0;
            var logMinIterations = gradient.LogIndex ? Math.Log(gradient.MinIterations, logBase) : 0.0;
            var logBailout = Math.Log(bailout);
            var halfOverLogBailout = gradient.UseAlternateSmoothingConstant ? 0.5 * logBailout : 0.5 / logBailout;
            var bailoutSquared = bailout * bailout;
            var useSqrt = gradient.RootIndex && Math.Abs(gradient.Root - 2) < Epsilon;
            #endregion

            Parallel.For(0, xThreads, ix =>
              Parallel.For(0, yThreads, iy =>
                {
                    var area = MathUtilities.StartEndCoordinates(startXAll, endXAll, startYAll, endYAll,
                        xThreads, ix, yThreads, iy);
                    int startX = area.Item1, startY = area.Item3, endX = area.Item2, endY = area.Item4;
                    int width = endX - startX, height = endY - startY;
                    var scale = MathUtilities.Scale(widthAll, heightAll, region.Min.Real, region.Max.Real,
                        region.Min.Imaginary, region.Max.Imaginary);
                    var min = MathUtilities.PixelToArgandCoordinates(startX, startY, scale.Item1, scale.Item2,
                        region.Min.Real, region.Min.Imaginary);

                    double rMin = min.Real, iMin = min.Imaginary;
                    DrawMandelbrot(
                        startX, startY, width, height,
                        rMin, iMin, maxIterations,
                        scale.Item1, scale.Item2, widthAll,
                        palette, gradient, image,
                        colors, bailoutSquared, halfOverLogBailout,
                        logBase, logMinIterations, root, rootMinIterations,
                        indexScale, indexWeight, useSqrt);                             
                }));
            return image;
        }

        /// <summary>
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="rMin"></param>
        /// <param name="iMin"></param>
        /// <param name="maxIterations"></param>
        /// <param name="rScale"></param>
        /// <param name="iScale"></param>
        /// <param name="scan"></param>
        /// <param name="palette"></param>
        /// <param name="gradient"></param>
        /// <param name="image"></param>
        /// <param name="colors"></param>
        /// <param name="bailoutSquared"></param>
        /// <param name="halfOverLogBailout"></param>
        /// <param name="logBase"></param>
        /// <param name="logMinIterations"></param>
        /// <param name="root"></param>
        /// <param name="rootMinIterations"></param>
        /// <param name="indexScale"></param>
        /// <param name="indexWeight"></param>
        /// <param name="useSqrt"></param>
        private static void DrawMandelbrot(int startX, int startY, int width, int height,
            double rMin,double iMin,
            int maxIterations, double rScale, double iScale, int scan,
            Color[] palette, Gradient gradient, byte[] image,
            int colors, double bailoutSquared, double halfOverLogBailout, 
            double logBase, double logMinIterations,
            double root, double rootMinIterations,
            double indexScale, double indexWeight,
            bool useSqrt)
        {
            for (var py = 0; py < height; ++py)
            {
                for (var px = 0; px < width; ++px)
                {
                    double x = 0.0, xp = 0.0; // x;
                    double y = 0.0, yp = 0.0; // y;
                    var mod = 0.0; // x * x + y * y;
                    var x0 = px * rScale + rMin;
                    var y0 = py * iScale + iMin;
                    var iteration = 0;
                    while (mod < bailoutSquared && iteration < maxIterations)
                    {
                        var xtemp = x * x - y * y + x0;
                        var ytemp = 2 * x * y + y0;
                        double dx = xtemp - x,
                            dy = ytemp - y,
                            dxp = xtemp - xp,
                            dyp = ytemp - yp;
                        if ((dx*dx < Epsilon && dy*dy < Epsilon)
                            ||(dxp*dxp < Epsilon && dyp*dyp < Epsilon))
                        {
                            iteration = maxIterations;
                            break;
                        }
                        xp = x;
                        yp = y;
                        x = xtemp;
                        y = ytemp;
                        mod = x * x + y * y;
                        ++iteration;
                    }
                    var smoothed = Math.Log(Math.Log(mod) * halfOverLogBailout) * OneOverLog2;
                    var index = indexScale * (iteration + 1 - indexWeight * smoothed);
                    if (useSqrt)
                    {
                        index = Math.Sqrt(index) - rootMinIterations;
                    }
                    else if (gradient.RootIndex)
                    {
                        index = Math.Pow(index, root) - rootMinIterations;
                    }
                    if (gradient.LogIndex)
                    {
                        index = Math.Log(index, logBase) - logMinIterations;

                    }
                    var actualIndex = MathUtilities.NormalizeIdx(index, colors, gradient);
                    Palette.Lerp(
                        palette[actualIndex], 
                        palette[(actualIndex + 1) % colors], 
                        smoothed - (long)smoothed,
                        out byte red, out byte green, out byte blue);
                    var offset = BitDepthFor24BppRgb * ((startY + py) * scan + startX + px);
                    image[offset] = blue;
                    image[offset + 1] = green;
                    image[offset + 2] = red;
                }
            }
        }

    }
}