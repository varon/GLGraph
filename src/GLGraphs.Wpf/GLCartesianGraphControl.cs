using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GLGraphs.CartesianGraph;
using JetBrains.Annotations;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Wpf;

namespace GLGraphs.Wpf {
    /// Generic cartesian graph control.
    /// Extend this class with the type you need, if you wish to define this in XAML directly.
    /// i.e. `public sealed class GLCartesianGraphControlString :  GLCartesianGraphControl&lt;string&gt;
    public class GLCartesianGraphControl<T>: UserControl {

        private GLWpfControl _control;
        private CartesianGraphState<T> _state;
        
        private Vector2 _lastMouse;
        private Vector2 _mouseDownStartPt;

        /// The actual graph this control wraps.
        public CartesianGraph<T> Graph { get; set; }

        /// Event fired before the graph is updated & rendered.
        public event Action<TimeSpan> Render;

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
            var settings = new GLWpfControlSettings();
            _control = new GLWpfControl();
            _control.Ready += OnReady;
            Content = _control;
            _control.Start(settings);
        }

        private void OnReady() {
            var graphSettings = CartesianGraphSettings.Default;
            Graph = new CartesianGraph<T>(graphSettings);
            _state = Graph.State;
            _control.Render += OnRender;
        }


        public void ResetView() {
            if (Graph == null) {
                return;
            }
            _state.Camera.Target.Position = Vector2.Zero;
            _state.Camera.Target.VerticalSize = CartesianGraphState<T>.DefaultCameraZoom;
            _state.IsCameraAutoControlled = true;
        }

        [Pure]
        private Vector2 ClientToView(Point pt) {
            var v = new Vector2((float) pt.X, (float) pt.Y);
            v.X /= (float) _control.RenderSize.Width;
            v.Y /= (float) _control.RenderSize.Height;
            return v;
        }

        private void OnRender(TimeSpan deltaTime) {
            if (Graph == null) {
                return;
            }
            Render?.Invoke(deltaTime);
            var size = _control.RenderSize;
            Graph.State.ViewportHeight = (float) size.Height;
            Graph.State.ViewportHeight = (float) size.Width;
            var delta = (float) deltaTime.TotalSeconds;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0, 0, (int) size.Width, (int) size.Height);
            if (Graph != null) {
                var aspect = (float) (size.Width / size.Height);
                Graph.State.Camera.Target.AspectRatio = aspect;
                Graph.State.Camera.Current.AspectRatio = aspect;
                Graph.State.Update(delta);
                Graph.Render();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs mm) {
            base.OnMouseWheel(mm);
            if (Graph == null) {
                return;
            }
            // for some reason WPF gives mouse wheel values * 120, so we just rescale it back
            var delta = mm.Delta / 120.0f;
            Graph.State.Camera.Target.ZoomIn(delta * 10.0f);
            Graph.State.IsCameraAutoControlled = false;
        }

        protected override void OnMouseMove(MouseEventArgs mm) {
            base.OnMouseMove(mm);
            var renSize = _control.RenderSize;
            var size = new Vector2((float) renSize.Width, (float) renSize.Height);
            var clientPosPt = mm.GetPosition(_control);
            var screenPt = ClientToView(clientPosPt);
            Graph.State.MousePosition = screenPt;
            
            var clientPos = new Vector2((float) clientPosPt.X, (float) clientPosPt.Y);

            if (mm.LeftButton == MouseButtonState.Pressed) {
                var p = screenPt;
                var r = new Box2(p, p);
                r.Inflate(_mouseDownStartPt);
                Graph.State.DragRectangle = r;
            }

            if (mm.RightButton == MouseButtonState.Pressed) {
                Graph.State.IsCameraAutoControlled = false;
                var delta = clientPos - _lastMouse;
                delta.Y = -delta.Y;
                var moveDelta = (delta / (float) size.Y) * Graph.State.Camera.Current.VerticalSize;
                
                Graph.State.Camera.Target.Position += moveDelta;
            }


            if (Graph.State.TryGetMouseover(screenPt, out var targetPt)) {
                Graph.State.MouseoverTarget = targetPt;
            }
            else {
                Graph.State.MouseoverTarget = null;
            }

            _lastMouse = clientPos;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            _mouseDownStartPt = ClientToView(e.GetPosition(_control));
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonUp(e);
            var screenPt = ClientToView(e.GetPosition(_control));
            // we moved more than 2 pixels
            if (Vector2.Distance(screenPt, _mouseDownStartPt) > 2.0f / _control.FrameBufferHeight) {
                Graph.State.FinishDrag();
            }
            else {
                Graph.State.Click();
            }

            _mouseDownStartPt = Vector2.Zero;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e) {
            base.OnMouseUp(e);
            if (e.ChangedButton == MouseButton.Middle) {
                ResetView();
            }
        }
    }
}