using OpenTK.Mathematics;

namespace GLGraphs.CartesianGraph {
    
    /// A square area of the graph colored a particular color.
    /// Used to add highlights to specific regions.
    public sealed class GraphRegion {
        /// The actual bounds of this region.
        public Box2 Bounds { get; set; }

        /// The color of this region.
        public Color4 Color { get; set; } = new Color4(1.0f, 0.0f, 0.0f, 0.25f);

        internal GraphRegion() { }

    }
}
