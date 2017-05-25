using System;
using System.Drawing;
using System.Globalization;

namespace Mandelbrot
{
    public struct Gradient : IEquatable<Gradient>
    {
        /// <summary>
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Gradient other)
        {
            return PaletteBailout.Equals(other.PaletteBailout) && 
                MaxIterationColor == other.MaxIterationColor && 
                PaletteScale.Equals(other.PaletteScale) && 
                Shift.Equals(other.Shift) && 
                LogIndex == other.LogIndex &&
                RootIndex == other.RootIndex && 
                Root.Equals(other.Root) &&
                MinIterations == other.MinIterations &&
                IndexScale.Equals(other.IndexScale) &&
                Weight.Equals(other.Weight);
        }

        /// <summary>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Gradient && Equals((Gradient) obj);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PaletteBailout.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxIterationColor.GetHashCode();
                hashCode = (hashCode * 397) ^ PaletteScale.GetHashCode();
                hashCode = (hashCode * 397) ^ Shift.GetHashCode();
                hashCode = (hashCode * 397) ^ LogIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ RootIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ Root.GetHashCode();
                hashCode = (hashCode * 397) ^ MinIterations;
                hashCode = (hashCode * 397) ^ IndexScale.GetHashCode();
                hashCode = (hashCode * 397) ^ Weight.GetHashCode();
                return hashCode;
            }
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
            return new Gradient(
                Color.FromArgb(int.Parse(data[10], NumberStyles.HexNumber)),
                double.Parse(data[0]), double.Parse(data[1]), double.Parse(data[2]),
                bool.Parse(data[3]), bool.Parse(data[4]), bool.Parse(data[5]),
                double.Parse(data[6]), int.Parse(data[7]),
                double.Parse(data[8]), double.Parse(data[9]));
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            $"PaletteScale: {PaletteScale}, Shift: {Shift}, Palette_Bailout = {PaletteBailout}" +
            $" Log_Index: {LogIndex}, Root_Index: {RootIndex}," +
            $" Root: {Root}, MinIterations = {MinIterations},"+
            $" Fraction_Scale: {IndexScale}, Weight = {Weight}";

        /// <summary>
        /// </summary>
        /// <param name="maxIterationColor"></param>
        /// <param name="paletteScale"></param>
        /// <param name="shift"></param>
        /// <param name="paletteBailout"></param>
        /// <param name="logIndex"></param>
        /// <param name="rootIndex"></param>
        /// <param name="useAlternateSmoothingConstant"></param>
        /// <param name="root"></param>
        /// <param name="minIterations"></param>
        /// <param name="indexScale"></param>
        /// <param name="weight"></param>
        public Gradient(
            Color maxIterationColor,
            double paletteScale, double shift, double paletteBailout, 
            bool logIndex = false, bool rootIndex = true, bool useAlternateSmoothingConstant = false,
            double root = 2, int minIterations = 1,
            double indexScale = 1.0, double weight = 1.0
           )
        {
            MaxIterationColor = maxIterationColor;
            PaletteScale = paletteScale;
            Shift = shift;
            PaletteBailout = paletteBailout;
            LogIndex = logIndex;
            RootIndex = !(Math.Abs(root - 1.0) < MathUtilities.Tolerance) && rootIndex;
            Root = root;
            Exponent = 1 / root;
            if (minIterations < 0 && !logIndex) throw new ArgumentException($"Min. iterations cutoff must be >= 0, is {minIterations}");
            if (minIterations < 1 && logIndex) throw new ArgumentException($"Min. iterations cutoff must be >= 1, is {minIterations}");
            MinIterations = minIterations;
            IndexScale = indexScale;
            Weight = weight;
        }

        #region Implementation of IEquatable<Gradient>

        /// <inheritdoc />
        bool IEquatable<Gradient>.Equals(Gradient other)
        {
            return Equals(other);
        }

        #endregion

        /// <summary>
        /// </summary>
        public double Exponent { get; }

        /// <summary>
        /// </summary>
        public double PaletteBailout { get; }

        public Color MaxIterationColor { get; }

        /// <summary>
        /// </summary>
        public double PaletteScale { get; }

        /// <summary>
        /// </summary>
        public double Shift { get; }

        /// <summary>
        /// </summary>
        public bool LogIndex { get; }

        /// <summary>
        /// </summary>
        public bool RootIndex { get; }

        /// <summary>
        /// </summary>
        public double Root { get; }

        /// <summary>
        /// </summary>
        public int MinIterations { get; }

        /// <summary>
        /// </summary>
        public double IndexScale { get; }

        /// <summary>
        /// </summary>
        public double Weight { get; }
    }
}