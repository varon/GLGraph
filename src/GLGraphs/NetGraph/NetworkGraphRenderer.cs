using System;
using GLGraphs.ObjectTKExtensions;
using GLGraphs.ObjectTKExtensions.Text;
using ObjectTK;
using ObjectTK.GLObjects;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GLGraphs.NetGraph {
    /// Responsible for performing the rendering of the Network Graph.
    internal sealed class NetworkGraphRenderer<T> {
        private readonly ShaderProgram _circleShader = GLFactory.Shader.EmbeddedResVertFrag("Circle", "Simple.vert", "Circle.frag");
        private readonly int _lineVao = GL.GenVertexArray();
        private readonly int _lineVertexBuffer = GL.GenBuffer();
        private readonly ShaderProgram _solidShader = GLFactory.Shader.EmbeddedResVertFrag("Solid", "Simple.vert", "SolidColor.frag");
        private readonly ShaderProgram _textShader = GLFactory.Shader.EmbeddedResVertFrag("Text", "Simple.vert", "Text.frag");

        private readonly NetworkGraphConfig _cfg;
        private bool _hasSetupGraphics;


        public NetworkGraphRenderer(NetworkGraphConfig cfg) {
            _cfg = cfg;
        }

        internal void Render(NetworkGraphState<T> state) {
            if (!_hasSetupGraphics) {
                _hasSetupGraphics = true;
                SetupGraphics();
            }

            DrawNodes(state);
            DrawLinks(state);
            if (_cfg.LabelDisplayMode != LabelDisplayMode.Never) {
                DrawLabels(state);
            }

            var suppressedMessages = new[] {131185};
            GLDebugLog.Deactivate(DebugSourceControl.DebugSourceApi,DebugTypeControl.DebugTypeOther, suppressedMessages);
        }


        private void SetupGraphics() {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ClearColor(_cfg.BackgroundColor);
            GL.Enable(EnableCap.LineSmooth);
            GL.LineWidth(2.0f);

            GL.BindVertexArray(_lineVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _lineVertexBuffer);
            GL.EnableVertexAttribArray(0);
            int elemSize;
            unsafe {
                elemSize = sizeof(Vector2);
            }

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, elemSize, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            
        }

        private void DrawNodes(NetworkGraphState<T> state) {
            GL.UseProgram(_circleShader.Handle);
            var vpMat = state.Camera.Current.ViewProjection;
            GL.UniformMatrix4(_circleShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);

            var quad = Primitives.Quad;
            GL.BindVertexArray(quad.Handle);
            // draw the nodes:
            for (var i = 0; i < state.Positions.Length; i++) {
                GL.Uniform2(_circleShader.Uniforms["Position"].Location, state.Positions[i]);
                var size = _cfg.WeightToScale(state.Weights[i]);
                GL.Uniform2(_circleShader.Uniforms["Scale"].Location, Vector2.One * size);
                var category = state.Categories[i];
                var catCol = _cfg.CategoryColors[category % _cfg.CategoryColors.Length];
                var col = i == state.SelectedIndex ? _cfg.SelectedCol : catCol;
                GL.Uniform4(_circleShader.Uniforms["Color"].Location, col);
                GL.DrawElements(PrimitiveType.TriangleStrip, quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            }

            GL.UseProgram(0);
            GL.BindVertexArray(0);
        }

        private void DrawLinks(NetworkGraphState<T> state) {
            // draw the links:
            GL.UseProgram(_solidShader.Handle);
            var vpMat = state.Camera.Current.ViewProjection;
            GL.UniformMatrix4(_solidShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);
            GL.Uniform2(_solidShader.Uniforms["Scale"].Location, Vector2.One);
            GL.Uniform4(_solidShader.Uniforms["Color"].Location, Color4.White);
            // hacky buffer-based approach:
            var bufferData = new Vector2[state.Links.Length * 2];
            for (var i = 0; i < state.Links.Length; i++) {
                var (left, right) = state.Links[i];
                bufferData[i * 2] = state.Positions[left];
                bufferData[i * 2 + 1] = state.Positions[right];
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _lineVertexBuffer);
            int elemSize;
            unsafe {
                elemSize = sizeof(Vector2);
            }

            GL.BufferData(BufferTarget.ArrayBuffer, elemSize * bufferData.Length, bufferData, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // draw the vertex array for the lines.
            GL.BindVertexArray(_lineVao);
            GL.DrawArrays(PrimitiveType.Lines, 0, bufferData.Length);
            
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        private static bool IsAdjacentSelected(NetworkGraphState<T> state, int idx) {
            var selectedIdx = state.SelectedIndex;
            if (selectedIdx == -1 || idx == -1) {
                return false;
            }

            if (selectedIdx == idx) {
                return true;
            }
            var con1 = state.ConnectionStrengths[selectedIdx, idx];
            var con2 = state.ConnectionStrengths[idx, selectedIdx];

            return con1 > 0 || con2 > 0;
        }
        
        
        private void DrawLabels(NetworkGraphState<T> state) {
            GL.UseProgram(_textShader.Handle);
            var vpMat = state.Camera.Current.ViewProjection;
            GL.UniformMatrix4(_textShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);

            var quad = Primitives.Quad;
            GL.BindVertexArray(quad.Handle);
            var fixedScale = 0.025f * state.Camera.Current.VerticalSize;

            // draw the nodes:
            for (var i = 0; i < state.Nodes.Length; i++) {
                float opacity;
                switch (_cfg.LabelDisplayMode) {
                    case LabelDisplayMode.Never:
                        opacity = 0;
                        break;
                    case LabelDisplayMode.Always:
                        opacity = 1;
                        break;
                    case LabelDisplayMode.Selected:
                        opacity = i == state.SelectedIndex ? 1 : 0;
                        break;
                    case LabelDisplayMode.SelectedAndAdjacent:
                        opacity = IsAdjacentSelected(state, i) ? 1 : 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                
                var label = state.Nodes[i].ToString();
                var tex = TextRendering.GetTextTexture(label);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
                GL.Uniform1(_textShader.Uniforms["tex"].Location, 0);

                var aspect = (float) tex.Width / tex.Height;
                var size = _cfg.LabelScaleMode == LabelScaleMode.Fixed ? fixedScale: 0.2f;
                var scale = new Vector2(aspect, 1.0f) * size;
                var renderScale = scale * _cfg.TextScale;
                GL.Uniform2(_textShader.Uniforms["Scale"].Location, renderScale);

                var pos = state.Positions[i];
                // pos.X += 0.66f*size * aspect;
                // pos.Y -= size;
                GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
                var unselectedCol = _cfg.LabelColor;
                var col = i == state.SelectedIndex ? _cfg.SelectedLabelCol : unselectedCol;
                col.A = opacity;
                GL.Uniform4(_textShader.Uniforms["Color"].Location, col);
                GL.DrawElements(PrimitiveType.TriangleStrip, quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            }
            
            GL.UseProgram(0);
            GL.BindVertexArray(0);
        }

        internal void DeleteBuffers() {
            GL.DeleteVertexArray(_lineVao);
            GL.DeleteBuffer(_lineVertexBuffer);
        }
    }
}
