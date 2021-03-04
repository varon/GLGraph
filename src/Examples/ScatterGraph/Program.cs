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
                _graph = ScatterGraphGenerator.GenerateScatterGraph();
            }

            var aspect = (float) _window.ClientSize.X / _window.ClientSize.Y;
            _graph.State.Camera.Target.AspectRatio = aspect;
            _graph.State.Camera.Current.AspectRatio = aspect;
            
            _control = new GraphGlfwWindowControl<string>(_window, _graph.State);
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
            var (x, y) = ScatterGraphGenerator.GenNormalDistPt(r);
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
