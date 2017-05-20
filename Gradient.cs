using System;
using System.Collections.Generic;

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
            return Math.Abs(PaletteScale - other.PaletteScale) < MathUtilities.Tolerance &&
                   Math.Abs(Shift - other.Shift) < MathUtilities.Tolerance &&
                   Math.Abs(Root - other.Root) < MathUtilities.Tolerance &&
                   Math.Abs(IndexScale - other.IndexScale) < MathUtilities.Tolerance &&
                   Math.Abs(Weight - other.Weight) < MathUtilities.Tolerance &&
                   MinIterations == other.MinIterations &&
                   LogIndex == other.LogIndex &&
                   RootIndex == other.RootIndex &&
                   UseAlternateSmoothingConstant == other.UseAlternateSmoothingConstant;
        }

        /// <summary>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return !ReferenceEquals(null, obj) && obj is Gradient && Equals((Gradient)obj);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hashCode = PaletteScale.GetHashCode();
            hashCode = (hashCode * 397) ^ Shift.GetHashCode();
            hashCode = (hashCode * 397) ^ Root.GetHashCode();
            hashCode = (hashCode * 397) ^ IndexScale.GetHashCode();
            hashCode = (hashCode * 397) ^ Weight.GetHashCode();
            hashCode = (hashCode * 397) ^ MinIterations.GetHashCode();
            hashCode = (hashCode * 397) ^ LogIndex.GetHashCode();
            hashCode = (hashCode * 397) ^ RootIndex.GetHashCode();
            hashCode = (hashCode * 397) ^ UseAlternateSmoothingConstant.GetHashCode();
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
            return new Gradient(
                double.Parse(data[0]), double.Parse(data[1]),
                bool.Parse(data[2]), bool.Parse(data[3]), bool.Parse(data[4]),
                double.Parse(data[5]), int.Parse(data[6]),
                double.Parse(data[7]), double.Parse(data[8]));
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() =>
            $"PaletteScale: {PaletteScale}, Shift: {Shift}," +
            $" Log_Index: {LogIndex}, Root_Index: {RootIndex}," +
            $" Use_Alternate_Smoothing_Constant: {UseAlternateSmoothingConstant}," +
            $" Root: {Root}, MinIterations = {MinIterations},"+
            $" Fraction_Scale: {IndexScale}, Weight = {Weight}";

        /// <summary>
        /// </summary>
        /// <param name="paletteScale"></param>
        /// <param name="shift"></param>
        /// <param name="logIndex"></param>
        /// <param name="rootIndex"></param>
        /// <param name="useAlternateSmoothingConstant"></param>
        /// <param name="root"></param>
        /// <param name="minIterations"></param>
        /// <param name="indexScale"></param>
        /// <param name="weight"></param>
        public Gradient(double paletteScale, double shift,
            bool logIndex = false, bool rootIndex = true, bool useAlternateSmoothingConstant = false,
            double root = 2, int minIterations = 1,
            double indexScale = 1.0, double weight = 1.0)
        {
            PaletteScale = paletteScale;
            Shift = shift;
            LogIndex = logIndex;
            RootIndex = !(Math.Abs(root - 1.0) < MathUtilities.Tolerance) && rootIndex;
            UseAlternateSmoothingConstant = useAlternateSmoothingConstant;
            Root = root;
            Exponent = 1 / root;
            if (minIterations < 0 && !logIndex) throw new ArgumentException($"Min. iterations cutoff must be >= 0, is {minIterations}");
            if (minIterations < 1 && logIndex) throw new ArgumentException($"Min. iterations cutoff must be >= 1, is {minIterations}");
            MinIterations = minIterations;
            IndexScale = indexScale;
            Weight = weight;
        }

        #region GradientEqualityComparer

        private sealed class GradientEqualityComparer : IEqualityComparer<Gradient>
        {
            public bool Equals(Gradient x, Gradient y)
            {
                return x.PaletteScale.Equals(y.PaletteScale) && x.Shift.Equals(y.Shift) && x.LogIndex == y.LogIndex && x.RootIndex == y.RootIndex && x.UseAlternateSmoothingConstant == y.UseAlternateSmoothingConstant && x.Root.Equals(y.Root) && x.MinIterations == y.MinIterations && x.IndexScale.Equals(y.IndexScale) && x.Weight.Equals(y.Weight);
            }

            public int GetHashCode(Gradient obj)
            {
                unchecked
                {
                    var hashCode = obj.PaletteScale.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Shift.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.LogIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.RootIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.UseAlternateSmoothingConstant.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Root.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.MinIterations;
                    hashCode = (hashCode * 397) ^ obj.IndexScale.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Weight.GetHashCode();
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<Gradient> GradientComparer { get; } = new GradientEqualityComparer();

        #region Implementation of IEquatable<Gradient>

        bool IEquatable<Gradient>.Equals(Gradient other)
        {
            return Equals(other);
        }

        #endregion

        #endregion

        /// <summary>
        /// </summary>
        public double Exponent { get; }

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
        public bool UseAlternateSmoothingConstant { get; }

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