using GLGraphs.CartesianGraph;
using GLGraphs.Utils;
using JetBrains.Annotations;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace GLGraphs {
    
    public sealed class GraphGlfwWindowControl<T> : CleanObject {
        private readonly GameWindow _window;
        private readonly IGraphState<T> _graph;
        private bool _hasBoundToEvents;
        
        private float _lastMousewheel;
        private Vector2 _mouseDownStartPt;


        public GraphGlfwWindowControl(GameWindow window, IGraphState<T> graph) {
            _window = window;
            _graph = graph;
        }

        public void BindToEvents() {
            if (_hasBoundToEvents) {
                return;
            }
            _hasBoundToEvents = true;
            
            _window.MouseWheel += OnMouseWheel;
            _window.MouseMove += OnMouseMove;
            _window.MouseDown += OnMouseDown;
            _window.MouseUp += OnMouseUp;
        }

        private void OnMouseUp(MouseButtonEventArgs obj) {
            if (_window.IsMouseButtonReleased(MouseButton.Left)) {
                _graph.FinishDrag();
                _mouseDownStartPt = Vector2.Zero;
            }
        }

        [Pure]
        private Vector2 ClientToView(Vector2 pt) {
            pt.X /= _window.ClientSize.X;
            pt.Y /= _window.ClientSize.Y;
            // pt.Y = -pt.Y;
            return pt;
        }

        private void OnMouseDown(MouseButtonEventArgs obj) {
            if (_window.IsMouseButtonDown(MouseButton.Left)) {
                var viewPt = ClientToView(_window.MouseState.Position);
                _mouseDownStartPt = viewPt;
            }
            else if (_window.IsMouseButtonPressed(MouseButton.Middle)) {
                var c = _graph.Camera;
                c.Target.Position = Vector2.Zero;
                c.Target.VerticalSize = CartesianGraphState<T>.DefaultCameraZoom;
                c.Target.Rotation = 0.0f;
                _graph.IsCameraAutoControlled = true;
            }
        }

        private void OnMouseMove(MouseMoveEventArgs ev) {
            if (_window.IsMouseButtonDown(MouseButton.Right)) {
                _graph.IsCameraAutoControlled = false;
                var delta = ev.Delta;
                delta.Y = -delta.Y;
                _graph.Camera.Target.Position += (delta / _window.ClientSize.Y) * _graph.Camera.Target.VerticalSize;
            }

            if (_window.IsMouseButtonDown(MouseButton.Left)) {
                var p = ClientToView(ev.Position);
                var r = new Box2(p, p);
                r.Inflate(_mouseDownStartPt);
                _graph.DragRectangle = r;
            }

            _graph.ViewportWidth = _window.ClientSize.X;
            _graph.ViewportHeight = _window.ClientSize.Y;
            _graph.MousePosition = ClientToView(ev.Position);

            if (_graph.TryGetMouseover(ClientToView(ev.Position), out var targetPt)) {
                _graph.MouseoverTarget = targetPt;
            }
            else {
                _graph.MouseoverTarget = default;
            }
        }
        
        private void OnMouseWheel(MouseWheelEventArgs obj) {
            var delta = obj.OffsetY - _lastMousewheel;
            _lastMousewheel = obj.OffsetY;
            _graph.Camera.Target.ZoomIn(delta * 10);
            _graph.IsCameraAutoControlled = false;
        }
    }
}