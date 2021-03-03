using JetBrains.Annotations;

namespace GLGraphs.Utils {
    internal static class IntExtensions {
        /// Retrieves the next power of 2 for the given integer.
        [Pure]
        public static int NextPowerOf2(this int x) {
            var power = 1;
            while (power < x) {
                power *= 2;
            }

            return power;
        }
    }
}