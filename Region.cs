using System.Numerics;

namespace Mandelbrot
{
    public struct Region

    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public Region NormalizeRegion()
        {
            return OriginAndWidth ? OriginAndWidthToRegion() : this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Region OriginAndWidthToRegion()
        {
            var origin = Min;
            var halfX = Max.Real / 2;
            var halfY = Max.Imaginary / 2;
            return new Region(
                min: new Complex(origin.Real - halfX, origin.Imaginary - halfY),
                max: new Complex(origin.Real + halfX, origin.Imaginary + halfY),
                originAndWidth: false);
        }

        /// <summary>
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="originAndWidth"></param>
        public Region(Complex min, Complex max, bool originAndWidth = false)
        {
            Max = max;
            Min = min;
            OriginAndWidth = originAndWidth;
        }

        /// <summary>
        /// </summary>
        public Complex Min { get; }

        /// <summary>
        /// </summary>
        public Complex Max { get; }

        /// <summary>
        /// </summary>
        private bool OriginAndWidth { get; }


        /// <summary>
        /// </summary>
        /// <param name="load"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is null. </exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format. </exception>
        /// <exception cref="OverflowException"><paramref name="s" /> represents a number that is less than <see cref="F:System.Double.MinValue" /> or greater than <see cref="F:System.Double.MaxValue" />. </exception>
        public static Region FromString(string load)
        {
            var data = load.Split(' ');
            var min = data[0].Split(',');
            var max = data[1].Split(',');
            return new Region(new Complex(double.Parse(min[0]), double.Parse(min[1])),
                new Complex(double.Parse(max[0]), double.Parse(max[1])), bool.Parse(data[2]));
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Min: {Min}, Max: {Max}, OriginAndWidth: {OriginAndWidth}";
    }
}