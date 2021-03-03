using System;
using GLGraphs.CartesianGraph;
using GLGraphs.ObjectTKExtensions;
using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace GLGraphs.NetGraph {
    
    /// A complete network graph. This is supplied with the data and exposes render/update methods.
    public sealed class NetworkGraph<T> : IDisposable {
        private readonly NetworkGraphConfig _cfg;
        private readonly NetworkGraphData<T> _data;
        private readonly NetworkGraphState<T> _state;
        private readonly NetworkGraphRenderer<T> _renderer;
        private bool _isDisposed;
        
        public NetworkGraph([NotNull] NetworkGraphData<T> data, [CanBeNull] NetworkGraphConfig cfg = null) {
            cfg ??= NetworkGraphConfig.Default;
            cfg = cfg.Copy();
            _cfg = cfg;
            _data = data;
            _state = new NetworkGraphState<T>(data, cfg);
            _renderer = new NetworkGraphRenderer<T>(cfg);
        }

        /// The camera used to display this network graph.
        /// This should be manipulated via zoom and other functions.
        public DampenedCamera2D Camera => _state.Camera;

        public IGraphState<T> State => _state;

        /// The currently selected item in this network graph.
        /// If this is null, then no item is selected.
        [CanBeNull]
        public T SelectedItem {
            get => _state.SelectedItem;
            set => _state.SelectedItem = value;
        }

        /// Advances the network graph simulation by the specified time.
        /// This does not require any access to OpenGL resources and may be run in a separate thread, assuming that proper synchronization is observed.
        public void Update(float t) {
            _state.Update(t);
        }

        /// Renders this network graph.
        public void Render() {
            _renderer.Render(_state);
        }

        /// Retrieves the current mouseover item.
        /// This requires the mouse position in VIEW space. This is a co-ordinate system where:
        /// Top left is (0,0) and bottom right is (1,1).
        /// To get to this from screen (pixel) co-ordinates, divide x/y by screen width/height respectively, then flip the Y value. 
        [Pure]
        public bool TryGetMouseover(Vector2 mousePosViewSpace, out T mouseOver) {
            // rescale into view co-ordinates
            var (x, y) = mousePosViewSpace * 2.0f - Vector2.One;
            // project mouse into world
            var inverseVp = Camera.Current.ViewProjection.Inverted();

            var worldPos = (new Vector4(x, -y, 0, 1) * inverseVp).Xy;

            var best = -1;
            for (int i = 0; i < _data.Count; i++) {
                var pos = _state.Positions[i];
                var weight = _state.Weights[i];
                var scale = _cfg.WeightToScale(weight);
                var dist = Vector2.Distance(pos, worldPos);
                if (dist < scale) {
                    best = i;
                }
            }

            if (best == -1) {
                mouseOver = default;
            }
            else {
                mouseOver = _data.Nodes[best];
            }
            return best != -1;
        }

        /// When called, this object will destroy any resources.
        public void Dispose() {
            if (_isDisposed) {
                return;
            }
            _isDisposed = true;
            _renderer.DeleteBuffers();
        }
    }
}
