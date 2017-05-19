using System;
using System.Drawing;

namespace Mandelbrot
{
    public static class Palette
    {
        /// <summary>
        /// </summary>
        /// <param name="numColors"></param>
        /// <param name="logIndex"></param>
        /// <param name="scaleDownFactor"></param>
        /// <returns></returns>
        public static double RecommendedGradientScale(int numColors, bool logIndex, double scaleDownFactor) =>
            logIndex ? numColors - 1 : numColors / scaleDownFactor;

        /// <summary>
        /// </summary>
        /// <param name="controls"></param>
        /// <param name="numColors"></param>
        /// <param name="extrapolationType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> or <paramref name="selector" /> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="red" />, <paramref name="green" />, or <paramref name="blue" /> is less than 0 or greater than 255.</exception>
        /// <exception cref="OverflowException">The number of elements in <paramref name="source" /> is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source" /> contains no elements.</exception>
        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count" /> is less than 0.-or-<paramref name="start" /> + <paramref name="count" /> -1 is larger than <see cref="F:System.Int32.MaxValue" />.</exception>
        public static Color[] GenerateColorPalette(Tuple<double, Color>[] controls, int numColors,
            MathUtilities.ExtrapolationType extrapolationType = MathUtilities.ExtrapolationType.None)
        {
            var palette = new Color[numColors];
            var controlCount = controls.Length;
            var oneOverNumColors = 1.0 / numColors;
            double[] red = new double[controlCount],
                green = new double[controlCount],
                blue = new double[controlCount],
                xs = new double[controlCount];
            for (var i = 0; i < controlCount; ++i)
            {
                red[i] = controls[i].Item2.R;
                green[i] = controls[i].Item2.G;
                blue[i] = controls[i].Item2.B;
                xs[i] = controls[i].Item1;
            }
            var channelSplines = new[]
            {
                MathUtilities.CreateInterpolant(xs, red, extrapolationType),
                MathUtilities.CreateInterpolant(xs, green, extrapolationType),
                MathUtilities.CreateInterpolant(xs, blue, extrapolationType)
            };
            for (var i = 0; i < numColors; ++i)
            {
                var arg = i * oneOverNumColors;
                palette[i] = Color.FromArgb(
                    MathUtilities.Clamp((int)channelSplines[0](arg), 0, 255),
                    MathUtilities.Clamp((int)channelSplines[1](arg), 0, 255),
                    MathUtilities.Clamp((int)channelSplines[2](arg), 0, 255));
            }
            return palette;
        }

        /// <summary>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="bias"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <exception cref="ArgumentException"><paramref name="red" />, <paramref name="green" />, or <paramref name="blue" /> is less than 0 or greater than 255.</exception>
        public static void Lerp(Color from, Color to, double bias, out byte red, out byte green, out byte blue)
        {
            bias = double.IsNaN(bias) ? 0 : (double.IsInfinity(bias) ? 1 : bias);
            byte toRed = to.R, toGreen = to.G, toBlue = to.B;
            red = (byte)(toRed + (from.R - toRed) * bias);
            green = (byte)(toGreen + (from.G - toGreen) * bias);
            blue = (byte)(toBlue + (from.B - toBlue) * bias);
        }
    }
}