using System.Collections.Generic;
using JetBrains.Annotations;

namespace GLGraphs.Utils {
    internal static class ReadOnlyListExtensions {
        
        /// Finds the index of the item in the readonly list.
        [Pure]
        public static int IndexOf<T>(this IReadOnlyList<T> self, T elementToFind) {
            for (var i = 0; i < self.Count; i++) {
                var element = self[i];
                if (Equals(element, elementToFind)) {
                    return i;
                }
            }
            return -1;
        }
    }
}
