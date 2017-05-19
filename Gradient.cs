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
            return Math.Abs(Scale - other.Scale) < MathUtilities.Tolerance &&
                   Math.Abs(Shift - other.Shift) < MathUtilities.Tolerance &&
                   Math.Abs(Root - other.Root) < MathUtilities.Tolerance &&
                   Math.Abs(FractionScale - other.FractionScale) < MathUtilities.Tolerance &&
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
            var hashCode = Scale.GetHashCode();
            hashCode = (hashCode * 397) ^ Shift.GetHashCode();
            hashCode = (hashCode * 397) ^ Root.GetHashCode();
            hashCode = (hashCode * 397) ^ FractionScale.GetHashCode();
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
            $"Scale: {Scale}, Shift: {Shift}," +
            $" Log_Index: {LogIndex}, Root_Index: {RootIndex}," +
            $" Use_Alternate_Smoothing_Constant: {UseAlternateSmoothingConstant}," +
            $" Root: {Root}, MinIterations = {MinIterations},"+
            $" Fraction_Scale: {FractionScale}, Weight = {Weight}";

        /// <summary>
        /// </summary>
        /// <param name="scale"></param>
        /// <param name="shift"></param>
        /// <param name="logIndex"></param>
        /// <param name="rootIndex"></param>
        /// <param name="useAlternateSmoothingConstant"></param>
        /// <param name="root"></param>
        /// <param name="minIterations"></param>
        /// <param name="fractionScale"></param>
        /// <param name="weight"></param>
        public Gradient(double scale, double shift,
            bool logIndex = false, bool rootIndex = true, bool useAlternateSmoothingConstant = false,
            double root = 2, int minIterations = 1,
            double fractionScale = 1.0, double weight = 1.0)
        {
            Scale = scale;
            Shift = shift;
            LogIndex = logIndex;
            RootIndex = rootIndex;
            UseAlternateSmoothingConstant = useAlternateSmoothingConstant;
            Root = root;
            Exponent = 1 / root;
            if (minIterations < 0 && !logIndex) throw new ArgumentException($"Min. iterations cutoff must be >= 0, is {minIterations}");
            if (minIterations < 1 && logIndex) throw new ArgumentException($"Min. iterations cutoff must be >= 1, is {minIterations}");
            MinIterations = minIterations;
            FractionScale = fractionScale;
            Weight = weight;
        }

        #region GradientEqualityComparer

        private sealed class GradientEqualityComparer : IEqualityComparer<Gradient>
        {
            public bool Equals(Gradient x, Gradient y)
            {
                return x.Scale.Equals(y.Scale) && x.Shift.Equals(y.Shift) && x.LogIndex == y.LogIndex && x.RootIndex == y.RootIndex && x.UseAlternateSmoothingConstant == y.UseAlternateSmoothingConstant && x.Root.Equals(y.Root) && x.MinIterations == y.MinIterations && x.FractionScale.Equals(y.FractionScale) && x.Weight.Equals(y.Weight);
            }

            public int GetHashCode(Gradient obj)
            {
                unchecked
                {
                    var hashCode = obj.Scale.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Shift.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.LogIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.RootIndex.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.UseAlternateSmoothingConstant.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.Root.GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.MinIterations;
                    hashCode = (hashCode * 397) ^ obj.FractionScale.GetHashCode();
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
        public double Scale { get; }

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
        public double FractionScale { get; }

        /// <summary>
        /// </summary>
        public double Weight { get; }
    }
}