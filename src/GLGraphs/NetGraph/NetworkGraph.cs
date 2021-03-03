using System;
using GLGraphs.CartesianGraph;
using GLGraphs.ObjectTKExtensions;
using JetBrains.Annotations;

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
