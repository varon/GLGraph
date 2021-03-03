using GLGraphs.CartesianGraph;
using GLGraphs.NetGraph;
using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace GLGraphs {
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
        /// This may be null if the point does not belong to a fixed series, i.e. in a <see cref="NetworkGraph{T}"/>
        [CanBeNull]
        public GraphSeries<T> Series { get; }
    }
}