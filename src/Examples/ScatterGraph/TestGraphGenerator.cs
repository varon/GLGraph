using System;
using GLGraphs.CartesianGraph;
using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace Examples.ScatterGraph {
    /// Generates test graph data
    internal static class TestGraphGenerator {

        [Pure]
        public static Vector2 GenNormalDistPt(Random r) {
            
            var mag = r.NextDouble();
            while (r.NextDouble() > 0.1f) {
                mag += r.NextDouble();
            }
            var dir = new Vector2((float) r.NextDouble()*2 - 1, (float) r.NextDouble()*2-1);
            dir.Normalize();

            var res = (float) mag * dir * 0.1f;
            if (r.NextDouble() < 0.1f) {
                if (r.NextDouble() > 0.5f) {
                    res.X = float.NaN;
                }
                else {
                    res.Y = float.NaN;
                }
            }
            return res;
        }


        [Pure]
        [NotNull]
        public static CartesianGraph<string> GenerateScatterGraph() {
            const int scatterPoints = 1;
            const int seriesCount = 5;

            var g = new CartesianGraph<string>();
            
            var r = new Random(21);
            
            for (var i = 0; i < seriesCount; i++) {
                var series = g.State.AddSeries(SeriesType.Point, $"Series {i + 1}");
                series.PointShape = (SeriesPointShape) r.Next((int) SeriesPointShape.InvertedTriangleOutline);
                
                for (var j = 0; j < scatterPoints; j++) {
                    var (x, y) = GenNormalDistPt(r);
                    series.Add($"Point {i}", x, y);
                }
            }

            var c = new Color4(r: 1.0f, g:0.0f, b:0.0f, a:0.125f);
            g.State.AddRegion(new Box2(-0.5f, -0.5f, 0.5f, 0.5f), c);
            return g;
        }

    }
}
