using System;
using GLGraphs.Utils;
using JetBrains.Annotations;

namespace GLGraphs.CartesianGraph {
    /// A complete cartesian graph. This is supplied with the data and exposes render/update methods.
    public sealed class CartesianGraph<T> : CleanObject, IDisposable {
        private readonly CartesianGraphSettings _cfg;
        private readonly CartesianGraphRenderer<T> _renderer;
        private bool _isDisposed;
        
        /// The data associated with this graph. Access and edit this to modify the displayed data.
        [NotNull]
        public CartesianGraphState<T> State { get; }

        public CartesianGraph([CanBeNull] CartesianGraphSettings cfg = null) {
            cfg ??= CartesianGraphSettings.Default;
            cfg = cfg.Copy();
            _cfg = cfg;
            State = new CartesianGraphState<T>(cfg);
            _renderer = new CartesianGraphRenderer<T>(cfg);
        }
        

        /// Renders this cartesian graph.
        public void Render() {
            _renderer.Render(State);
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
