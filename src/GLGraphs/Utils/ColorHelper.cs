using System;
using System.Drawing;
using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace GLGraphs.Utils {
    internal static class ColorHelper {
        private static readonly ColorConverter _cc = new ColorConverter();

        /// Parses a hexadecimal color code into a color4. This must be prefixed with '#'.
        [Pure]
        public static Color4 Parse([NotNull]
            string hexCode) {
            var result = _cc.ConvertFromString(hexCode);
            if (result == null) {
                throw new ArgumentException(
                    $"Could not convert color '{hexCode}'. Was not valid input. Maybe you are missing a '#' in front?", nameof(hexCode));
            }

            var sysCol = (Color) result;
            return sysCol;
        }
    }
}
