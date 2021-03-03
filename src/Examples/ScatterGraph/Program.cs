using System;
using System.Diagnostics;
using GLGraphs;
using GLGraphs.CartesianGraph;
using GLGraphs.Utils;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Examples.ScatterGraph {

    public sealed class GraphGlfwWindowControl<T> : CleanObject {
        private readonly GameWindow _window;
        private readonly CartesianGraph<T> _graph;
        private bool _hasBoundToEvents;
        
        private float _lastMousewheel;
        private Vector2 _mouseDownStartPt;


        public GraphGlfwWindowControl(GameWindow window, CartesianGraph<T> graph) {
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
                _graph.State.FinishDrag();
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
                var c = _graph.State.Camera;
                c.Target.Position = Vector2.Zero;
                c.Target.VerticalSize = CartesianGraphState<T>.DefaultCameraZoom;
                c.Target.Rotation = 0.0f;
                _graph.State.IsCameraAutoControlled = true;
            }
        }

        private void OnMouseMove(MouseMoveEventArgs ev) {
            if (_window.IsMouseButtonDown(MouseButton.Right)) {
                _graph.State.IsCameraAutoControlled = false;
                var delta = ev.Delta;
                delta.Y = -delta.Y;
                _graph.State.Camera.Target.Position += (delta / _window.ClientSize.Y) * _graph.State.Camera.Target.VerticalSize;
            }

            if (_window.IsMouseButtonDown(MouseButton.Left)) {
                var p = ClientToView(ev.Position);
                var r = new Box2(p, p);
                r.Inflate(_mouseDownStartPt);
                _graph.State.DragRectangle = r;
            }

            _graph.State.ViewportWidth = _window.ClientSize.X;
            _graph.State.ViewportHeight = _window.ClientSize.Y;
            _graph.State.MousePosition = ClientToView(ev.Position);

            if (_graph.State.TryGetMouseover(ClientToView(ev.Position), out var targetPt)) {
                _graph.State.MouseoverTarget = targetPt;
            }
            else {
                _graph.State.MouseoverTarget = null;
            }
        }
        
        private void OnMouseWheel(MouseWheelEventArgs obj) {
            var delta = obj.OffsetY - _lastMousewheel;
            _lastMousewheel = obj.OffsetY;
            _graph.State.Camera.Target.ZoomIn(delta * 10);
            _graph.State.IsCameraAutoControlled = false;
        }
    }
    
    public static class Program {
        private static readonly Stopwatch _timer = Stopwatch.StartNew();
        private static TimeSpan _lastFrame = TimeSpan.Zero;
        private static GameWindow _window;
        private static CartesianGraph<string> _graph;
        private static GraphGlfwWindowControl<string> _control;
        
        private static readonly NativeWindowSettings _nativeWindowSettings = new NativeWindowSettings {
            Flags = ContextFlags.Debug,
            Profile = ContextProfile.Core,
            Title = "Scatter Graph Test",
            NumberOfSamples = 1,
            Size = new Vector2i(1920,1080),
            APIVersion = new Version(3, 3),
        };
        

        public static void Main() {
            _window = new GameWindow(GameWindowSettings.Default, _nativeWindowSettings);
            var isNetworkGraph = false;

            if (isNetworkGraph) {
                // var graphData = TestGraphGenerator.GenerateNetworkGraph();
                // _graph = new NetworkGraph<string>(graphData);
            }
            else {
                _graph = TestGraphGenerator.GenerateScatterGraph();
            }

            var aspect = (float) _window.ClientSize.X / _window.ClientSize.Y;
            _graph.State.Camera.Target.AspectRatio = aspect;
            _graph.State.Camera.Current.AspectRatio = aspect;
            
            _control = new GraphGlfwWindowControl<string>(_window, _graph);
            _control.BindToEvents();
            
            GLDebugLog.Message += OnMessage;
            _window.RenderFrame += OnRenderFrame;
            _window.UpdateFrame += OnUpdate;
            _window.Run();
        }


        private static void OnUpdate(FrameEventArgs obj) {
            var r = new Random();
            var seriesIdx = r.Next(_graph.State.Series.Count);
            var series = _graph.State.Series[seriesIdx];
            var (x, y) = TestGraphGenerator.GenNormalDistPt(r);
            var pt = DateTime.UtcNow.Ticks;
            var str = pt.ToString();
            var offset = series.Points.Count;
            series.Add(str, x, y);
            _graph.State.Update((float) obj.Time);
        }

        private static void OnMessage(object sender, DebugMessageEventArgs e) {
            Console.Error.WriteLine($"[{e.ID}]{e.Severity}|{e.Type}/{e.Source}: {e.Message}");
        }

        private static void OnRenderFrame(FrameEventArgs frameEventArgs) {
            var cur = _timer.Elapsed;
            var delta = cur - _lastFrame;
            _lastFrame = cur;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0,0,_window.ClientSize.X, _window.ClientSize.Y);
            var aspect = _window.ClientSize.X / (float) _window.ClientSize.Y;
            _graph.State.Camera.Current.AspectRatio = aspect;
            _graph.State.Camera.Target.AspectRatio = aspect;
            _graph.Render();

            _window.Context.SwapBuffers();
        }
        
        
    }
}
