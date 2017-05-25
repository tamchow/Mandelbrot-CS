using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Threading;
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
            var cartesianRegion = new Region(
                min: new Complex(region.Min.Real, -region.Min.Imaginary),
                max: new Complex(region.Max.Real, -region.Max.Imaginary));
            int globalStartX = start.Width, globalEndX = end.Width;
            int globalStartY = start.Height, globalEndY = end.Height;

            int globalWidth = globalEndX - globalStartX,
                globalHeight = globalEndY - globalStartY;


            double globalRealStart = region.Min.Real,
                globalImaginaryStart = region.Min.Imaginary;

            var image = new byte[globalHeight * globalWidth * BitDepthFor24BppRgb];

            int xThreads = threads.Width, yThreads = threads.Height;

            var globalScale = MathUtilities.Scale(
                globalWidth, globalHeight,
                region.Min.Real, region.Max.Real,
                region.Min.Imaginary, region.Max.Imaginary);

            #region General Configuration For All Threads
            if (maxIterations < 1) throw new ArgumentException($"Max iterations must be >= 1, is {maxIterations}");
            var colors = palette.Length;
            var root = gradient.Exponent;
            double indexScale = gradient.IndexScale, indexWeight = gradient.Weight;
            double scaledMinIterations = indexScale * gradient.MinIterations,
                scaledMaxIterations = indexScale * maxIterations;
            var rootMinIterations = gradient.RootIndex ? Math.Pow(scaledMinIterations, root) : 0.0;
            var logBase = gradient.LogIndex ? Math.Log(scaledMaxIterations / scaledMinIterations) : 0.0;
            var logMinIterations = gradient.LogIndex ? Math.Log(scaledMinIterations, logBase) : 0.0;
            var logPaletteBailout = Math.Log(gradient.PaletteBailout);
            var halfOverLogPaletteBailout = 0.5 / logPaletteBailout;
            var bailoutSquared = bailout * bailout;
            var useSqrt = gradient.RootIndex && Math.Abs(gradient.Root - 2) < Epsilon;
            var maxIterationColor = gradient.MaxIterationColor;
            #endregion

            var tasks = new Thread[xThreads * yThreads];
            for (var iy = 0; iy < yThreads; ++iy)
            {
                for (var ix = 0; ix < xThreads; ++ix)
                {
                    var localRegion = MathUtilities.StartEndCoordinates(
                        globalStartX, globalEndX, globalStartY, globalEndY,
                        xThreads, ix, yThreads, iy);

                    int localStartX = localRegion.Item1,
                        localStartY = localRegion.Item3,
                        localEndX = localRegion.Item2,
                        localEndY = localRegion.Item4;

                    int localWidth = localEndX - localStartX,
                        localHeight = localEndY - localStartY;

                    var localStart =
                        MathUtilities.PixelToArgandCoordinates(
                            localStartX, localStartY,
                            globalScale.Real, globalScale.Imaginary,
                            globalRealStart, globalImaginaryStart);


                    var taskIndex = iy * xThreads + ix;
                    tasks[taskIndex] =
                        new Thread(() =>
                    DrawMandelbrot(
                        localStartX, localStartY,
                        localWidth, localHeight,
                        localStart.Real, localStart.Imaginary, maxIterations,
                        globalScale.Real, globalScale.Imaginary, globalWidth,
                        palette, gradient, image,
                        colors, bailoutSquared, halfOverLogPaletteBailout,
                        logBase, logMinIterations, root, rootMinIterations,
                        indexScale, indexWeight, useSqrt, maxIterationColor));
                    tasks[taskIndex].Start();
                }
            }
            // Wait for completion
            foreach (var task in tasks)
            {

                task.Join();
            }

            return image;
        }

        /// <summary>
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="realStart"></param>
        /// <param name="imaginaryStart"></param>
        /// <param name="maxIterations"></param>
        /// <param name="realScale"></param>
        /// <param name="imaginaryScale"></param>
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
        /// <param name="maxIterationColor"></param>
        private static void DrawMandelbrot(
            int startX, int startY,
            int width, int height,
            double realStart, double imaginaryStart,
            int maxIterations,
            double realScale, double imaginaryScale,
            int scan, Color[] palette, Gradient gradient, byte[] image, int colors,
            double bailoutSquared, double halfOverLogBailout,
            double logBase, double logMinIterations,
            double root, double rootMinIterations,
            double indexScale, double indexWeight,
            bool useSqrt, Color maxIterationColor)
        {
            for (var py = 0; py < height; ++py)
            {
                for (var px = 0; px < width; ++px)
                {
                    double x = 0.0, xp = 0.0; // x;
                    double y = 0.0, yp = 0.0; // y;
                    var modulusSquared = 0.0; // x * x + y * y;
                    var x0 = px * realScale + realStart;
                    var y0 = py * imaginaryScale + imaginaryStart;
                    var iterations = 0;
                    while (modulusSquared < bailoutSquared && iterations < maxIterations)
                    {
                        var xtemp = x * x - y * y + x0;
                        var ytemp = 2 * x * y + y0;
                        double dx = xtemp - x,
                            dy = ytemp - y,
                            dxp = xtemp - xp,
                            dyp = ytemp - yp;
                        if ((dx * dx < Epsilon && dy * dy < Epsilon) ||
                            (dxp * dxp < Epsilon && dyp * dyp < Epsilon))
                        {
                            iterations = maxIterations;
                            break;
                        }
                        xp = x;
                        yp = y;
                        x = xtemp;
                        y = ytemp;
                        modulusSquared = x * x + y * y;
                        ++iterations;
                    }
                    var smoothed = Math.Log(Math.Log(modulusSquared) * halfOverLogBailout) * OneOverLog2;
                    var index = indexScale * (iterations + 1 - indexWeight * smoothed);
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
                    index = MathUtilities.NormalizeIndex(index, colors);
                    var actualIndex = MathUtilities.PreparePaletteIndex(index, colors, gradient);

                    byte red, green, blue;
                    if (iterations >= maxIterations)
                    {
                        red = maxIterationColor.R;
                        green = maxIterationColor.G;
                        blue = maxIterationColor.B;
                    }
                    else
                    {
                        Palette.Lerp(
                            palette[actualIndex],
                            palette[(actualIndex + 1) % colors],
                            index - (long)index,
                            out red, out green, out blue);
                    }
                    var offset = BitDepthFor24BppRgb * ((startY + py) * scan + (startX + px));
                    image[offset] = blue;
                    image[offset + 1] = green;
                    image[offset + 2] = red;
                }
            }
        }
    }
}
