using System;
using System.Numerics;

namespace Mandelbrot
{
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
        public static Tuple<int, int, int, int> ConvertRegionToPixelCoordinates(int width, int height, Region region)
        {
            var area = UnpackRegion(region);
            double rMin = area.Item1, rMax = area.Item2, iMin = area.Item3, iMax = area.Item4;
            var scale = Scale(width, height, rMin, rMax, iMin, iMax);
            var startPixelCoordinates = ArgandToPixelCoordinates(region.Min, scale.Real, scale.Imaginary, rMin, iMin);
            var endPixelCoordinates = ArgandToPixelCoordinates(region.Max, scale.Real, scale.Imaginary, rMin, iMin);
            return new Tuple<int, int, int, int>(
                startPixelCoordinates.Item1, endPixelCoordinates.Item1,
                startPixelCoordinates.Item2, endPixelCoordinates.Item2);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="maxIndex"></param>
        /// <returns></returns>
        public static double NormalizeIndex(double index, int maxIndex)
        {
            return index < 0 ? maxIndex + index % maxIndex :
                (double.IsNaN(index) ? 0 : (double.IsInfinity(index) ? maxIndex : index));
        }

        public static double Clamp(double val, double min = 0.0, double max = 1.0)
        {
            return val < min ? min : (val > max ? max : val);
        }

        public static int PreparePaletteIndex(double index, int maxIndex, Gradient gradient)
        {
            return (int)(index * gradient.PaletteScale + gradient.Shift) % maxIndex;
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
            int xDist = (int)Math.Round((endX - startX) / (float)nx), yDist = (int)Math.Round((endY - startY) / (float)ny);
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
            new Tuple<int, int>(
                (int)((argand.Real - rMin) / rScale),
                (int)((argand.Imaginary - iMin) / iScale));

        /// <summary>
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Region ConvertPixelCoordinatesToRegion(Tuple<int, int, int, int> rectangle, Complex scale) =>
            ConvertPixelCoordinatesToRegion(rectangle, scale, Complex.Zero);

        /// <summary>
        /// </summary>
        /// <param name="rectangle"></param>
        /// <param name="scale"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Region ConvertPixelCoordinatesToRegion(Tuple<int, int, int, int> rectangle, Complex scale, Complex offset)
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
        public const double Tolerance = 1E-10;

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
                throw new ArgumentException($"length of xs ({length}) must be equal to length of ys ({ys.Length})");
            }
            switch (length)
            {
                case 0:
                    return x => 0.0;
                case 1:
                    return x => ys[0];
                default:
                    var indices = new int[length];
                    for (var i = 0; i < length; ++i) indices[i] = i;
                    Array.Sort(indices, (a, b) => xs[a] < xs[b] ? -1 : 1);
                    double xMin = 0.0, xMax = 0.0, yMin = 0.0, yMax = 0.0;
                    double[] newXs = new double[xs.Length], newYs = new double[ys.Length];
                    for (var i = 0; i < length; i++)
                    {
                        newXs[i] = xs[indices[i]];
                        xMin = Math.Min(xMin, newXs[i]);
                        xMax = Math.Max(xMax, newXs[i]);
                        newYs[i] = ys[indices[i]];
                        yMin = Math.Min(yMin, newYs[i]);
                        yMax = Math.Max(yMax, newYs[i]);
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
                            c1S[i + 1] = 3 * common / ((common + dxNext) / m + (common + dx) / mNext);
                        }
                    }
                    c1S[c1S.Length - 1] = ms[ms.Length - 1];
                    // Get degree-2 and degree-3 coefficients
                    double[] c2S = new double[c1S.Length - 1], c3S = new double[c1S.Length - 1];
                    for (var i = 0; i < c1S.Length - 1; i++)
                    {
                        double c1 = c1S[i], m = ms[i], invDx = 1 / dxs[i], common = c1 + c1S[i + 1] - m - m;
                        c2S[i] = (m - c1 - common) * invDx;
                        c3S[i] = common * invDx * invDx;
                    }
                    // Return interpolant function
                    double Interpolant(double x)
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
                            var mid = low + (high - low) / 2;
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
                    }

                    double RangeCheckedInterpolant(double x)
                    {
                        if (x >= xMin && x <= xMax)
                        {
                            return Interpolant(x);
                        }
                        switch (extrapolationType)
                        {
                            case ExtrapolationType.None:
                                return Interpolant(x);
                            case ExtrapolationType.Linear:
                                return yMin + (x - xMin) / (xMax - xMin) * yMax;
                            case ExtrapolationType.Constant:
                                return x < xMin ? yMin : yMax;
                            default:
                                return double.NaN;
                        }
                    }

                    return RangeCheckedInterpolant;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Clamp(int val, int min, int max)
        {
            return val < min ? min : (val > max ? max : val);
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
        public static Complex Scale(
            int width, int height,
            double rMin, double rMax,
            double iMin, double iMax)
        {
            var rScale = (Math.Max(rMin, rMax) - Math.Min(rMin, rMax)) / width;  // Amount to move each pixel in the real numbers
            var iScale = (Math.Max(iMin, iMax) - Math.Min(iMin, iMax)) / height; // Amount to move each pixel in the imaginary numbers
            return new Complex(rScale, iScale);
        }

        /// <summary>
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="realScale"></param>
        /// <param name="imaginaryScale"></param>
        /// <param name="realStart"></param>
        /// <param name="imaginaryStart"></param>
        /// <returns></returns>
        public static Complex PixelToArgandCoordinates(
            int x, int y,
            double realScale, double imaginaryScale,
            double realStart, double imaginaryStart) =>
            new Complex(x * realScale + realStart, y * imaginaryScale + imaginaryStart);

        /// <summary>
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        public static Tuple<double, double, double, double> UnpackRegion(Region region)
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
        public static Region PackBounds(double rMin, double rMax, double iMin, double iMax) =>
            new Region(new Complex(rMin, iMin), new Complex(rMax, iMax));
    }
}