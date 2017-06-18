using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Mandelbrot
{
    public static class Palette
    {
        public static Tuple<int, Tuple<double, Color>[]> LoadPaletteConfiguration(string fileName)
        {
            using (var reader = new StreamReader(fileName))
            {
                if (reader.Peek() > 0 && int.TryParse(reader.ReadLine(), out var numColors))
                {
                    if (numColors <= 0)
                    {
                        throw new ArgumentException($"Cannot have a palette with number of colors less than 0 - got {numColors}");
                    }
                    var initialColors = new List<Tuple<double, Color>>(numColors);
                    for (uint i = 0; i < numColors; ++i)
                    {
                        var lineParts = reader.ReadLine()?.Split(new []{":", ": "}, StringSplitOptions.RemoveEmptyEntries);
                        if (lineParts == null)
                        {
                            break;
                        }
                        if (lineParts.Length >= 2)
                        {
                            if (double.TryParse(lineParts[0], out double position))
                            {
                                var colorComponents = lineParts[1].Split(new[] { ",", ", " }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                                if (colorComponents.Length < 3)
                                {
                                    throw new ArgumentException(
                                        $"Color one line {i + 2} has less than three components");
                                }
                                var color = Color.FromArgb(colorComponents[0], colorComponents[1], colorComponents[2]);
                                initialColors.Add(Tuple.Create(position, color));
                            }
                            else
                            {
                                throw new ArgumentException($"Could not read position of color on line {i + 2}");
                            }
                        }
                        else
                        {
                            throw new ArgumentException($"Color specification is invalid on line {i + 2}");
                        }
                    }
                    return Tuple.Create(numColors, initialColors.ToArray());
                }
                throw new ArgumentException("File is corrupt or number of colors not specified");
            }
        }
        public static void SavePaletteAsMspal(string filename, Color[] colors)
        {
            // Calculate file length
            var length = 4 + 4 + 4 + 4 + 2 + 2 + colors.Length * 4;

            var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None);
            using (var bw = new BinaryWriter(stream, Encoding.ASCII))
            {
                // RIFF header
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(length - 4);
                bw.Write(Encoding.ASCII.GetBytes("PAL "));

                // Data chunk
                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(colors.Length * 4 + 4);
                bw.Write((ushort)0x0300); // PAL version
                bw.Write((ushort)colors.Length);

                // Colors
                foreach (var color in colors)
                {
                    bw.Write((byte)color.R);
                    bw.Write((byte)color.G);
                    bw.Write((byte)color.B);
                    bw.Write((byte)0x00); // Flags are always 0x00
                }
            }
        }

        public static string PaletteToString(Color[] palette)
        {
            var accumulator = new StringBuilder(palette.Length * 12); // 3 digits for 3 color components, separated by ',', terminated by '\n'
            foreach (var color in palette)
            {
                accumulator.Append(color.R).Append(',').Append(color.G).Append(',').Append(color.B).Append('\n');
            }
            return accumulator.ToString();
        }
        /// <summary>
        /// </summary>
        /// <param name="numColors"></param>
        /// <param name="logIndex"></param>
        /// <param name="scaleDownFactor"></param>
        /// <returns></returns>
        public static double RecommendedGradientScale(int numColors, bool logIndex, double scaleDownFactor) =>
            logIndex ? numColors - 1 : numColors * MathUtilities.Clamp(scaleDownFactor);

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

        #region Routines for ensuring the highest iteration counts map to a specific color
        private static double EuclideanDistance(Color a, Color b)
        {
            double dR = a.R - b.R, dG = a.G - b.G, dB = a.B - b.B;
            return Math.Sqrt(dR * dR + dG * dG + dB * dB);
        }
        public static double FindPaletteColorLocation(Color[] palette, Color color)
        {
            var locationOfPaletteColorClosestToBlack = 0.0;
            var distanceOfPaletteColorClosestToBlack = EuclideanDistance(palette[0], color);
            for (var i = 1; i < palette.Length; ++i)
            {
                var distanceOfCurrentColorFromBlack = EuclideanDistance(palette[i], color);
                if (Math.Abs(distanceOfCurrentColorFromBlack) < MathUtilities.Tolerance)
                {
                    break;
                }
                if (distanceOfCurrentColorFromBlack < distanceOfPaletteColorClosestToBlack)
                {
                    locationOfPaletteColorClosestToBlack = i;
                    distanceOfPaletteColorClosestToBlack = distanceOfCurrentColorFromBlack;
                }
            }
            return locationOfPaletteColorClosestToBlack / palette.Length;
        }
        public static double CalculateScaleDownFactorForLinearMapping(double paletteMaxIterationColorLocation)
        {
            return 1 - paletteMaxIterationColorLocation + MathUtilities.Tolerance;
        }
        #endregion
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
            byte fromRed = from.R, fromGreen = from.G, fromBlue = from.B;
            red = (byte)(fromRed + (to.R - fromRed) * bias);
            green = (byte)(fromGreen + (to.G - fromGreen) * bias);
            blue = (byte)(fromBlue + (to.B - fromBlue) * bias);
        }
    }
}