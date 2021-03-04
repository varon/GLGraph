using System;
using GLGraphs.NetGraph;
using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace Examples.NetworkGraph {
    /// Generates test graph data
    public static class NetGraphGenerator {
        
        [Pure]
        [NotNull]
        public static NetworkGraph<string> GenerateNetworkGraph(NetworkGraphConfig cfg = null) {
            const int count = 96;
            const double linkDensity = 0.65 / count;
            var r = new Random(21);
            // set the nodes up (as strings for this example).
            var nodes = new string[count];
            for (int i = 0; i < nodes.Length; i++) {
                nodes[i] = $"Node {i}";
            }
            // set up the node weights:
            var weights = new float[count];
            for (int i = 0; i < count; i++) {
                weights[i] = (float) r.NextDouble();
            }
            
            // set the links up;
            var connections = new float[count, count];
            for (int i = 0; i < count; i++) {
                for (int j = 0; j < count; j++) {
                    if (i == j) {
                        // nodes never link to themselves
                        continue;
                    }

                    if (r.NextDouble() < linkDensity) {
                        // we have a link:
                        var weight = (float) r.NextDouble();
                        connections[i, j] = weight;
                        connections[j, i] = weight;
                    }
                }
            }
            // set the categories up (placeholder/blank for now)
            var categories = new int[count];
            var data = new NetworkGraphData<string>(nodes, weights, connections, categories);
            return new NetworkGraph<string>(data, cfg);
        }

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
    }
}
