using OpenTK.Mathematics;

namespace GLGraphs.CartesianGraph {
    /// A point on the cartesian graph.
    public readonly struct GraphPt<T> {
        
        /// The data point value
        public T Value { get; }

        /// The x position
        public float X { get; }
        /// The y position
        public float Y { get; }

        public GraphPt(GraphSeries<T> series, T value, float x, float y) {
            Value = value;
            X = x;
            Y = y;
            Series = series;
        }

        /// The X/Y position of the point
        public Vector2 Position => new Vector2(X, Y);

        /// The series this point belongs to.
        public GraphSeries<T> Series { get; }
    }
}