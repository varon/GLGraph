using System.Collections.Generic;

namespace GLGraphs.NetGraph {
    /// Data class containing the underlying structure of the network graph.
    /// This is never modified at runtime and does not contain any mutable state. 
    public sealed class NetworkGraphData<T> {
        
        /// The nodes in this network graph
        public T[] Nodes { get; }

        /// normalized[0;1] weights (by node)
        public float[] Weights { get; }

        /// matrix of normalized[0;1] connection strengths between nodes
        public float[,] ConnectionStrength { get; }

        // Category is a discrete value that supports N categories.
        // This is used to group nodes.
        // Each node is colored by category, according to the category colors.
        public int[] Categories { get; }

        public IReadOnlyDictionary<T, int> NodeToIndex { get; }

        /// Indexed link pairs showing which nodes have non-zero connections from one to another.
        public (int, int)[] Links { get; }

        /// Sum of all of the connection weights from each node.
        public float[] SumConnectionStrengths { get; }


        public NetworkGraphData(T[] nodes, float[] weights, float[,] connections, int[] categories) {
            Nodes = nodes;
            Weights = weights;
            ConnectionStrength = connections;
            Categories = categories;
            var nodeToIndex = new Dictionary<T, int>(nodes.Length * 2);
            for (var i = 0; i < Nodes.Length; i++) {
                var node = Nodes[i];
                nodeToIndex[node] = i;
            }

            // sum connection strengths and record links:
            var sums = new float[nodes.Length];
            var links = new List<(int, int)>();
            for (var i = 0; i < nodes.Length; i++) {
                var sum = 0.0f;
                for (var j = 0; j < nodes.Length; j++) {
                    if (connections[i, j] > 0 && i < j) {
                        links.Add((i, j));
                    }

                    sum += connections[i, j];
                }

                sums[i] = sum;
            }

            SumConnectionStrengths = sums;
            Links = links.ToArray();
            NodeToIndex = nodeToIndex;
        }

        public int Count => Nodes.Length;
    }
}
