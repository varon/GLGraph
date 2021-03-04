using System;
using System.Windows;
using Examples.NetworkGraph;
using Examples.ScatterGraph;
using GLGraphs.CartesianGraph;
using GLGraphs.NetGraph;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;

namespace WpfExample {
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        
        public MainWindow() {
            InitializeComponent();
            NetGraph.Graph = NetGraphGenerator.GenerateNetworkGraph();
            CartGraph.Graph = ScatterGraphGenerator.GenerateScatterGraph();
            CartGraph.Render += AddPoint;
        }

        private void AddPoint(TimeSpan deltaTime) {
            var r = new Random();
            var seriesIdx = r.Next(CartGraph.Graph.State.Series.Count);
            var series = CartGraph.Graph.State.Series[seriesIdx];
            var (x, y) = ScatterGraphGenerator.GenNormalDistPt(r);
            var pt = DateTime.UtcNow.Ticks;
            var str = pt.ToString();
            var offset = series.Points.Count;
            series.Add(str, x, y);
            CartGraph.Graph.State.Update((float) deltaTime.TotalSeconds);
        }

        //
        // private void RenderLeftControl(TimeSpan deltaTime) {
        //     GL.ClearColor(Color4.Black);
        //     GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        //     
        //     _cartesianGraph.State.Update((float) deltaTime.TotalSeconds);
        //     
        //     
        //     GL.Viewport(0,0,LeftGLControl.FrameBufferWidth, LeftGLControl.FrameBufferHeight);
        //     var aspect = (1.0f * LeftGLControl.FrameBufferWidth) / LeftGLControl.FrameBufferHeight;
        //     _cartesianGraph.State.Camera.Current.AspectRatio = aspect;
        //     _cartesianGraph.State.Camera.Target.AspectRatio = aspect;
        //
        //     _cartesianGraph.Render();
        //     // _netGraph.Render();
        // }

        // private void RenderRightControl(TimeSpan deltaTime) {
        //     GL.ClearColor(Color4.Black);
        //     GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        //     
        //     _netGraph.State.Update((float) deltaTime.TotalSeconds);
        //     
        //     GL.Viewport(0,0,RightGLControl.FrameBufferWidth, RightGLControl.FrameBufferHeight);
        //     var aspect = (1.0f * RightGLControl.FrameBufferWidth) / RightGLControl.FrameBufferHeight;
        //     _netGraph.Camera.Current.AspectRatio = aspect;
        //     _netGraph.Camera.Target.AspectRatio = aspect;
        //     
        //     _netGraph.Render();
        // }
    }
}