using System.Collections.Generic;
using GLGraphs.Utils;
using MathNet.Numerics.Statistics;
using OpenTK.Mathematics;

namespace GLGraphs.CartesianGraph {
    
    public enum SeriesType {
        Line,
        Point
    }
    
    /// The available scatter symbol types.
    public enum SeriesPointShape {
        Circle = 0,
        CircleOutline = 1,
        Square = 2,
        SquareOutline = 3,
        Diamond = 4,
        DiamondOutline = 5,
        Triangle = 6,
        TriangleOutline = 7,
        InvertedTriangle = 8,
        InvertedTriangleOutline = 9,
    }
    
    /// A data series in the cartesian graph.
    public sealed class GraphSeries<T> : CleanObject {
        private readonly List<GraphPt<T>> _ptList = new List<GraphPt<T>>();
        
        internal bool InvalidateRenderCache { get; set; }

        /// The name of this series.
        public string Name { get; }
        
        /// Data points in the series.
        public IReadOnlyList<GraphPt<T>> Points => _ptList;

        public SeriesType SeriesType { get; }
        
        
        /// The shape used to draw the points.
        /// This is only used if <see cref="SeriesType"/> is set to <see cref="CartesianGraph.SeriesType.Point"/>.
        public SeriesPointShape PointShape { get; set; } = SeriesPointShape.Circle;

        /// Statistics for the X Axis. This is updated internally. Do not modify this.
        public RunningStatistics XStats { get; private set; } = new RunningStatistics();
        /// Statistics for the Y axis. This is updated internally. Do not modify this.
        public RunningStatistics YStats { get; private set; } = new RunningStatistics();

        /// If this series is displayed (or not).
        public bool IsVisible { get; set; } = true;

        public Color4 Color { get; set; }

        /// Adds a point to this series.
        public void Add(T value, float x, float y) {
            var pt = new GraphPt<T>(this, value, x, y);
            _ptList.Add(pt);
            if (!float.IsNaN(x) && !float.IsInfinity(x) && !float.IsNaN(y) && !float.IsInfinity(y)) {
                XStats.Push(x);
                YStats.Push(y);
            }
        }

        /// Clears all data from this series and resets statistics.
        public void Clear() {
            InvalidateRenderCache = true;
            _ptList.Clear();
            XStats = new RunningStatistics();
            YStats = new RunningStatistics();
        }
        
        internal GraphSeries(SeriesType seriesType, string name, Color4 color) {
            SeriesType = seriesType;
            Name = name;
            Color = color;
        }
    }
}
