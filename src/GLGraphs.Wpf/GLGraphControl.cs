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
    public sealed class GLGraphControl<T>: UserControl {

        private GLWpfControl _control;
        private CartesianGraph<T> _graph;
        private CartesianGraphState<T> _state;
        
        private Vector2 _lastMouse;
        private Vector2 _mouseDownStartPt;

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
            _graph = new CartesianGraph<T>(graphSettings);
            _state = _graph.State;
            _control.Render += OnRender;
        }


        public void ResetView() {
            if (_graph == null) {
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
            if (_graph == null) {
                return;
            }
            var size = _control.RenderSize;
            _graph.State.ViewportHeight = (float) size.Height;
            _graph.State.ViewportHeight = (float) size.Width;
            var delta = (float) deltaTime.TotalSeconds;
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Viewport(0, 0, (int) size.Width, (int) size.Height);
            if (_graph != null) {
                var aspect = (float) (size.Width / size.Height);
                _graph.State.Camera.Target.AspectRatio = aspect;
                _graph.State.Camera.Current.AspectRatio = aspect;
                _graph.State.Update(delta);
                _graph.Render();
            }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs mm) {
            base.OnMouseWheel(mm);
            if (_graph == null) {
                return;
            }
            // for some reason WPF gives mouse wheel values * 120, so we just rescale it back
            var delta = mm.Delta / 120.0f;
            _graph.State.Camera.Target.ZoomIn(delta * 10.0f);
            _graph.State.IsCameraAutoControlled = false;
        }

        protected override void OnMouseMove(MouseEventArgs mm) {
            base.OnMouseMove(mm);
            var renSize = _control.RenderSize;
            var size = new Vector2((float) renSize.Width, (float) renSize.Height);
            var clientPosPt = mm.GetPosition(_control);
            var screenPt = ClientToView(clientPosPt);
            _graph.State.MousePosition = screenPt;
            
            var clientPos = new Vector2((float) clientPosPt.X, (float) clientPosPt.Y);

            if (mm.LeftButton == MouseButtonState.Pressed) {
                var p = screenPt;
                var r = new Box2(p, p);
                r.Inflate(_mouseDownStartPt);
                _graph.State.DragRectangle = r;
            }

            if (mm.RightButton == MouseButtonState.Pressed) {
                _graph.State.IsCameraAutoControlled = false;
                var delta = clientPos - _lastMouse;
                delta.Y = -delta.Y;
                var moveDelta = (delta / (float) size.Y) * _graph.State.Camera.Current.VerticalSize;
                
                _graph.State.Camera.Target.Position += moveDelta;
            }


            if (_graph.State.TryGetMouseover(screenPt, out var targetPt)) {
                _graph.State.MouseoverTarget = targetPt;
            }
            else {
                _graph.State.MouseoverTarget = null;
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
                _graph.State.FinishDrag();
            }
            else {
                _graph.State.Click();
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