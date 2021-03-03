using System;
using System.Collections.Generic;
using System.Drawing;
using GLGraphs.ObjectTKExtensions;
using GLGraphs.ObjectTKExtensions.Text;
using GLGraphs.Utils;
using JetBrains.Annotations;
using ObjectTK;
using ObjectTK.GLObjects;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace GLGraphs.CartesianGraph {

    internal static class ShapeHelper {
        [Pure]
        private static string ShapeToName(SeriesPointShape shape) {
            return shape switch {
                SeriesPointShape.Circle => "circle.png",
                SeriesPointShape.CircleOutline => "circle-outline.png",
                SeriesPointShape.Square => "square.png",
                SeriesPointShape.SquareOutline => "square-outline.png",
                SeriesPointShape.Diamond => "diamond.png",
                SeriesPointShape.DiamondOutline => "diamond-outline.png",
                SeriesPointShape.Triangle => "triangle.png",
                SeriesPointShape.TriangleOutline => "triangle-outline.png",
                SeriesPointShape.InvertedTriangle => "inverted-triangle.png",
                SeriesPointShape.InvertedTriangleOutline => "inverted-triangle-outline.png",
                _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null)
            };
        }


        private static readonly Dictionary<SeriesPointShape, Texture2D> _texCache = new Dictionary<SeriesPointShape, Texture2D>();

        [NotNull]
        [MustUseReturnValue]
        public static Texture2D TextureFor(SeriesPointShape shape) {
            if (_texCache.TryGetValue(shape, out var cached)) {
                return cached;
            }
            
            var name = ShapeToName(shape);
            var tex = GLFactory.Texture.FromEmbeddedImage(TextureConfig.Default, name);
            _texCache[shape] = tex;
            return tex;
        }
        
        
    }

    /// Responsible for performing the rendering of the Scatter Graph.
    internal sealed class CartesianGraphRenderer<T> {
        private readonly ShaderProgram _solidShader = GLFactory.Shader.EmbeddedResVertFrag("Solid", "Simple.vert", "SolidColor.frag");
        private readonly ShaderProgram _pointShader = GLFactory.Shader.EmbeddedResVertFrag("Point", "Point.vert", "ColorTexture.frag");
        private readonly ShaderProgram _gridShader = GLFactory.Shader.EmbeddedResVertFrag("Grid", "Grid.vert", "Grid.frag");
        private readonly ShaderProgram _textShader = GLFactory.Shader.EmbeddedResVertFrag("Text", "Simple.vert", "Text.frag");
        private readonly CartesianGraphSettings _cfg;
        private readonly List<VertexArray> _seriesVaos = new List<VertexArray>();

        private static Vector2[] _uvPositions = {
            // first triangle
            new Vector2(-1, -1),
            new Vector2(1, 1),
            new Vector2(-1, 1),
            // second triangle
            new Vector2(1, 1),
            new Vector2(-1, -1),
            new Vector2(1, -1),
        };
        
        private static readonly int _stride = _uvPositions.Length;
        
        private bool _hasSetupGraphics;

        public float DisplayedPointSize { get; private set; }
        
        public float DisplayedLineWidth { get; private set; }

        public CartesianGraphRenderer(CartesianGraphSettings cfg) {
            _cfg = cfg;
        }

        internal void Render(CartesianGraphState<T> state) {
            if (!_hasSetupGraphics) {
                _hasSetupGraphics = true;
                SetupGraphics();
            }

            UpdateVaos(state);
            DrawRegions(state);
            DrawGrid(state);
            DrawLineSeries(state);
            DrawPointSeries(state);
            DrawDragBox(state);
            DrawAxisLabels(state);
            DrawTooltip(state);
        }


        private void DrawRegions(CartesianGraphState<T> state) {
            GL.BindVertexArray(Primitives.Quad.Handle);
            GL.UseProgram(_solidShader.Handle);
            var vpMat = state.Camera.Current.ViewProjection;
            GL.UniformMatrix4(_solidShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);
            foreach (var sec in state.Regions) {
                var box = sec.Bounds;
                var col = sec.Color;

                GL.Uniform2(_solidShader.Uniforms["Position"].Location, box.Center);
                GL.Uniform2(_solidShader.Uniforms["Scale"].Location, box.HalfSize);
                GL.Uniform4(_solidShader.Uniforms["Color"].Location, col);

                GL.DrawElements(PrimitiveType.TriangleStrip, Primitives.Quad.ElementCount, DrawElementsType.UnsignedInt, 0);

            }
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        [MustUseReturnValue]
        private static VertexArray CreatePointSeriesVao(GraphSeries<T> series) {
            // hacky, buffer-based approach. Should be doing diffs/deltas here.
            var count = series.Points.Count;
            var capacity = (count * _stride).NextPowerOf2();
            var posData = new Vector2[capacity];
            var uvData = new Vector2[capacity];
            for (var i = 0; i < count; i++) {
                var p = series.Points[i].Position;
                for (int j = 0; j < _uvPositions.Length; j++) {
                    posData[i * _stride + j] = p;
                    uvData[i * _stride + j] = _uvPositions[j];
                }
            }
            
            var positions = GLFactory.Buffer.ArrayBuffer("Positions", posData);
            var uvs = GLFactory.Buffer.ArrayBuffer("Uvs", uvData);
            positions.ElementCount = count * _stride;
            var vao = GLFactory.VertexArray.FromBuffers($"Series '{series.Name}'", positions, uvs);
            return vao;
        }
        
        private static unsafe void UpdatePointSeriesVao(VertexArray vao, GraphSeries<T> series) {
            var count = series.Points.Count;
            var capacity = vao.ElementCount.NextPowerOf2();
            
            var pointsBuffer = vao.Buffers[0];
            var uvBuffer = vao.Buffers[1];
            var offsetBytes = new IntPtr(vao.ElementCount * sizeof(Vector2));
            var missingElemCount = count - (pointsBuffer.ElementCount / _stride);
            if (series.InvalidateRenderCache || (count * _stride) > capacity) {
                // reallocate the buffer:
                capacity = (count * _stride).NextPowerOf2();
                var posData = new Vector2[capacity];
                var uvData = new Vector2[capacity];
                for (var i = 0; i < count; i++) {
                    var p = series.Points[i].Position;
                    for (int j = 0; j < _uvPositions.Length; j++) {
                        posData[i * _stride + j] = p;
                        uvData[i * _stride + j] = _uvPositions[j];
                    }
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBuffer.Handle);
                GL.BufferData(BufferTarget.ArrayBuffer,capacity * sizeof(Vector2), posData, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, uvBuffer.Handle);
                GL.BufferData(BufferTarget.ArrayBuffer,capacity * sizeof(Vector2), uvData, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
                pointsBuffer.ElementCount = count * _stride;
            }
            else {
                var sizeBytes = missingElemCount * sizeof(Vector2) * _stride;
                var posData = new Vector2[missingElemCount * _stride];
                var uvData = new Vector2[missingElemCount * _stride];
                for (var i = 0; i < missingElemCount; i++) {
                    var srcIdx = (pointsBuffer.ElementCount / _stride) + i;
                    var p = series.Points[srcIdx].Position;
                    
                    for (int j = 0; j < _uvPositions.Length; j++) {
                        posData[i * _stride + j] = p;
                        uvData[i * _stride + j] = _uvPositions[j];
                    }
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBuffer.Handle);
                GL.BufferSubData(BufferTarget.ArrayBuffer, offsetBytes, sizeBytes, posData);
                GL.BindBuffer(BufferTarget.ArrayBuffer, uvBuffer.Handle);
                GL.BufferSubData(BufferTarget.ArrayBuffer, offsetBytes, sizeBytes, uvData);
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
            pointsBuffer.ElementCount = (count * _stride);
            series.InvalidateRenderCache = false;
        }
        
        
        [MustUseReturnValue]
        private static VertexArray CreateLineSeriesVao(GraphSeries<T> series) {
            // hacky, buffer-based approach. Should be doing diffs/deltas here.
            var count = series.Points.Count;
            var capacity = count.NextPowerOf2();
            var arr = new Vector2[capacity];
            for (var i = 0; i < count; i++) {
                arr[i] = series.Points[i].Position;
            }
            
            var positions = GLFactory.Buffer.ArrayBuffer("Positions", arr);
            positions.ElementCount = count;
            var vao = GLFactory.VertexArray.FromBuffers($"Series '{series.Name}'", positions);
            return vao;
        }
        
        private static unsafe void UpdateLineSeriesVao(VertexArray vao, GraphSeries<T> series) {
            var count = series.Points.Count;
            var capacity = vao.ElementCount.NextPowerOf2();
            
            var pointsBuffer = vao.Buffers[0];
            GL.BindBuffer(BufferTarget.ArrayBuffer, pointsBuffer.Handle);
            var offsetBytes = new IntPtr(vao.ElementCount * sizeof(Vector2));
            var missingElemCount = count - pointsBuffer.ElementCount;
            if (series.InvalidateRenderCache || count > capacity) {
                // reallocate the buffer:
                capacity = MathHelper.NextPowerOfTwo(count);
                var allData = new Vector2[capacity];
                for (var i = 0; i < Math.Min(capacity, count); i++) {
                    allData[i] = series.Points[i].Position;
                }
                GL.BufferData(BufferTarget.ArrayBuffer,capacity * sizeof(Vector2), allData, BufferUsageHint.StaticDraw);
                pointsBuffer.ElementCount = count;
            }
            else {
                var sizeBytes = missingElemCount * sizeof(Vector2);
                var newData = new Vector2[missingElemCount];
                for (var i = 0; i < missingElemCount; i++) {
                    newData[i] = series.Points[pointsBuffer.ElementCount + i].Position;
                }
                GL.BufferSubData(BufferTarget.ArrayBuffer, offsetBytes, sizeBytes, newData);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            pointsBuffer.ElementCount = count;
            series.InvalidateRenderCache = false;
        }
        
        private void UpdateVaos(CartesianGraphState<T> state) {
            for (var i = 0; i < state.Series.Count; i++) {
                var series = state.Series[i];
                if (series.SeriesType == SeriesType.Point) {
                    if (i >= _seriesVaos.Count) {
                        var newVao = CreatePointSeriesVao(series);
                        _seriesVaos.Add(newVao);
                    }

                    var vao = _seriesVaos[i];
                    if (series.InvalidateRenderCache || series.Points.Count * _stride > vao.ElementCount) {
                        // append the latest data:
                        UpdatePointSeriesVao(vao, series);
                    }
                }
                else {
                    if (i >= _seriesVaos.Count) {
                        var newVao = CreateLineSeriesVao(series);
                        _seriesVaos.Add(newVao);
                    }
            
                    var vao = _seriesVaos[i];
                    if (series.InvalidateRenderCache || series.Points.Count > vao.ElementCount) {
                        // append the latest data:
                        UpdateLineSeriesVao(vao, series);
                    }
                }
            }
        }
        
        
        private void SetupGraphics() {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.ClearColor(_cfg.BackgroundColor);
            
            // smoother/larger points
            GL.Enable(EnableCap.ProgramPointSize);
            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);

            // retrieve the max (point) size and clamp to that.
            var pointSize = new float[2];
            GL.GetFloat(GetPName.PointSizeRange,pointSize);
            var targetSize = MathF.Min(_cfg.PointSize, pointSize[1]);
            DisplayedPointSize = targetSize;
            GL.PointSize(targetSize);


            GL.Enable(EnableCap.LineSmooth);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            
            // retrieve the max line width size and clamp to that.
            var lineWidth = new float[2];
            GL.GetFloat(GetPName.LineWidthRange,lineWidth);
            var targetLineWidth = MathF.Min(_cfg.LineSize, lineWidth[1]);
            DisplayedLineWidth = targetLineWidth;
            GL.LineWidth(DisplayedLineWidth);
        }
        
        private void DrawPointSeries(CartesianGraphState<T> state) {
            GL.UseProgram(_pointShader.Handle);
            var vpMat = state.Camera.Current.ViewProjection;
            vpMat = Matrix4.CreateScale(1.0f, state.YScale, 1.0f) * vpMat;
            GL.UniformMatrix4(_pointShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);
            GL.Uniform2(_pointShader.Uniforms["Scale"].Location, Vector2.One);
            GL.Uniform2(_pointShader.Uniforms["Position"].Location, Vector2.Zero);
            GL.Uniform1(_pointShader.Uniforms["Aspect"].Location, state.Camera.Current.AspectRatio);

            for (var i = 0; i < state.Series.Count; i++) {
                var series = state.Series[i];
                if (series.SeriesType != SeriesType.Point || !series.IsVisible) {
                    continue;
                }

                var vao = _seriesVaos[i];

                if (series.SeriesType == SeriesType.Point) {
                    var tex = ShapeHelper.TextureFor(series.PointShape);
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
                    GL.Uniform1(_pointShader.Uniforms["tex"].Location, 0);
                }

                GL.Uniform1(_pointShader.Uniforms["PointSize"].Location, 2 * _cfg.PointSize / state.ViewportHeight);
                GL.Uniform4(_pointShader.Uniforms["Color"].Location, series.Color);
                
                GL.BindVertexArray(vao.Handle);
                var primitiveType = series.SeriesType == SeriesType.Point ? PrimitiveType.Triangles : PrimitiveType.LineStrip;
                
                GL.DrawArrays(primitiveType, 0, series.Points.Count * _stride);
                GL.BindVertexArray(0);
            }
            GL.UseProgram(0);
        }
        
        
        private void DrawLineSeries(CartesianGraphState<T> state) {
            GL.UseProgram(_solidShader.Handle);
            var vpMat = state.Camera.Current.ViewProjection;
            vpMat = Matrix4.CreateScale(1.0f, state.YScale, 1.0f) * vpMat;
            GL.UniformMatrix4(_solidShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);
            GL.Uniform2(_solidShader.Uniforms["Scale"].Location, Vector2.One);
            GL.Uniform2(_solidShader.Uniforms["Position"].Location, Vector2.Zero);

            for (var i = 0; i < state.Series.Count; i++) {
                var series = state.Series[i];
                if (series.SeriesType != SeriesType.Line || !series.IsVisible) {
                    continue;
                }

                var vao = _seriesVaos[i];

                GL.Uniform4(_solidShader.Uniforms["Color"].Location, series.Color);
                
                GL.BindVertexArray(vao.Handle);
                var primitiveType = series.SeriesType == SeriesType.Point ? PrimitiveType.Points : PrimitiveType.LineStrip;
                
                GL.DrawArrays(primitiveType, 0, series.Points.Count);
                GL.BindVertexArray(0);
            }
            GL.UseProgram(0);
        }


        private void DrawGrid(CartesianGraphState<T> state) {
            GL.UseProgram(_gridShader.Handle);
            GL.Uniform4(_gridShader.Uniforms["Color"].Location, Color4.Gray);
            GL.Uniform1(_gridShader.Uniforms["majorXGridSpacing"].Location, state.XGridSpacing.Major);
            GL.Uniform1(_gridShader.Uniforms["minorXGridSpacing"].Location, state.XGridSpacing.Minor);
            
            GL.Uniform1(_gridShader.Uniforms["majorYGridSpacing"].Location, state.YGridSpacing.Major);
            GL.Uniform1(_gridShader.Uniforms["minorYGridSpacing"].Location, state.YGridSpacing.Minor);
            
            GL.Uniform1(_gridShader.Uniforms["majorXAlpha"].Location, _cfg.XAxis.MajorVisibile ? 1.0f : 0.0f);
            GL.Uniform1(_gridShader.Uniforms["minorXAlpha"].Location, _cfg.XAxis.MinorVisible ? 1.0f : 0.0f);
            GL.Uniform1(_gridShader.Uniforms["majorYAlpha"].Location, _cfg.YAxis.MajorVisibile ? 1.0f : 0.0f);
            GL.Uniform1(_gridShader.Uniforms["minorYAlpha"].Location, _cfg.YAxis.MinorVisible ? 1.0f : 0.0f);
            
            GL.Uniform1(_gridShader.Uniforms["originXAlpha"].Location, _cfg.XAxis.OriginVisible ? 1.0f : 0.0f);
            GL.Uniform1(_gridShader.Uniforms["originYAlpha"].Location, _cfg.XAxis.OriginVisible ? 1.0f : 0.0f);
            
            var vpMat = state.Camera.Current.ViewProjection;
            vpMat = Matrix4.CreateScale(1.0f, state.YScale, 1.0f) * vpMat;
            vpMat.Invert();
            GL.UniformMatrix4(_gridShader.Uniforms["InvViewProjectionMatrix"].Location, false, ref vpMat);
            // GL.Uniform2(_solidShader.Uniforms["Scale"].Location, Vector2.One);
            GL.BindVertexArray(Primitives.Quad.Handle);
            GL.DrawElements(PrimitiveType.TriangleStrip, Primitives.Quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }
        
        private void DrawAxisLabels(CartesianGraphState<T> state) {
            
            GL.UseProgram(_textShader.Handle);
            
            // vp matrix
            var yScale = state.YScale;
            var vpMat = state.Camera.Current.ViewProjection;
            // vpMat = Matrix4.CreateScale(1.0f, yScale, 1.0f) * vpMat;
            GL.UniformMatrix4(_textShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);
            // Color
            var col = Color4.White;
            GL.Uniform4(_textShader.Uniforms["Color"].Location, col);
            // bind
            var quad = Primitives.Quad;
            GL.BindVertexArray(quad.Handle);

            var size = _cfg.TextScale * 0.025f * state.Camera.Current.VerticalSize;
            var scale = Vector2.One * size;
            
            var leftPos = - (state.Camera.Current.HorizontalSize * 0.5f - size) - state.Camera.Current.Position.X;
            var bottomPos = - (state.Camera.Current.VerticalSize * 0.5f - size) - state.Camera.Current.Position.Y;

            var minorSpacingY = state.YGridSpacing.Minor;
            
            for (int i = 0; i < 100; i++) {
                var yPos = i * minorSpacingY;
                var pos = new Vector2(leftPos, yPos * yScale);
                var label = yPos.ToString("0.###"); 
                
                var tex = TextRendering.GetTextTexture(label, StringAlignment.Near);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
                GL.Uniform1(_textShader.Uniforms["tex"].Location, 0);
                
                GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
                
                
                var finalScale = new Vector2(scale.X * tex.AspectRatio, scale.Y);
                GL.Uniform2(_textShader.Uniforms["Scale"].Location, finalScale);
                
                GL.DrawElements(PrimitiveType.TriangleStrip, quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            }
            
            
            for (int i = 1; i < 100; i++) {
                var yPos = -i * minorSpacingY;
                var pos = new Vector2(leftPos, yPos * yScale);
                var label = yPos.ToString("0.###"); 
                GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
                var tex = TextRendering.GetTextTexture(label, StringAlignment.Near);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
                GL.Uniform1(_textShader.Uniforms["tex"].Location, 0);
                
                GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
                
                var finalScale = new Vector2(scale.X * tex.AspectRatio, scale.Y);
                GL.Uniform2(_textShader.Uniforms["Scale"].Location, finalScale);
                
                GL.DrawElements(PrimitiveType.TriangleStrip, quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            }


            var minorSpacingX = state.XGridSpacing.Minor;
            
            for (int i = 0; i < 100; i++) {
                var xPos = i * minorSpacingX;
                var pos = new Vector2(xPos, bottomPos);
                var label = xPos.ToString("0.###"); 
                GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
                var tex = TextRendering.GetTextTexture(label, StringAlignment.Near);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
                GL.Uniform1(_textShader.Uniforms["tex"].Location, 0);
                
                GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
                
                
                var finalScale = new Vector2(scale.X * tex.AspectRatio, scale.Y);
                GL.Uniform2(_textShader.Uniforms["Scale"].Location, finalScale);
                
                GL.DrawElements(PrimitiveType.TriangleStrip, quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            }
            
            for (int i = 1; i < 100; i++) {
                var xPos = -i * minorSpacingX;
                var pos = new Vector2(xPos, bottomPos);
                var label = xPos.ToString("0.###"); 
                GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
                var tex = TextRendering.GetTextTexture(label, StringAlignment.Near);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
                GL.Uniform1(_textShader.Uniforms["tex"].Location, 0);
                
                GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
                
                var finalScale = new Vector2(scale.X * tex.AspectRatio, scale.Y);
                GL.Uniform2(_textShader.Uniforms["Scale"].Location, finalScale);
                
                GL.DrawElements(PrimitiveType.TriangleStrip, quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            }

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }

        private void DrawTooltip(CartesianGraphState<T> state) {
            if (!state.MouseoverTarget.HasValue) {
                return;
            }
            var targetPt = state.MouseoverTarget.Value;

            // rescale into view co-ordinates
            var (x, y) = state.MousePosition * 2.0f - Vector2.One;
            // project mouse into world
            var inverseVp = state.Camera.Current.ViewProjection.Inverted();

            var worldPos = (new Vector4(x, -y, 0, 1) * inverseVp).Xy;

            // screenPos.X *= Camera.Current.AspectRatio;

            GL.BindVertexArray(Primitives.Quad.Handle);
            var tooltip = $"{targetPt.X:0.000}, {targetPt.Y:0.000}";
            var tex = TextRendering.GetTextTexture(tooltip);
            var tooltip2 = $"{targetPt.Series.Name}";// - {targetPt.Value}";
            var tex2 = TextRendering.GetTextTexture(tooltip2);
            
            var scale = 0.1f * new Vector2(tex.AspectRatio, 1.0f) * state.Camera.Current.VerticalSize * 0.15f;
            var pos = worldPos + new Vector2(scale.X + scale.Y, -scale.Y * 2);
            
            GL.UseProgram(_solidShader.Handle);
            
            var vpMat = state.Camera.Current.ViewProjection;
            GL.UniformMatrix4(_solidShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);
            
            // render the background box
            var bgCol = new Color4(0.25f, 0.25f, 0.25f, 0.5f);
            GL.Uniform4(_solidShader.Uniforms["Color"].Location, bgCol);

            var scaleBg = 0.1f * new Vector2(MathF.Max(tex.AspectRatio, tex2.AspectRatio), 1.0f) * state.Camera.Current.VerticalSize * 0.15f;
            var bgPos = new Vector2(pos.X, pos.Y - scaleBg.Y);
            GL.Uniform2(_solidShader.Uniforms["Position"].Location, bgPos);
            scaleBg.Y *= 2;
            GL.Uniform2(_solidShader.Uniforms["Scale"].Location, scaleBg);
            GL.DrawElements(PrimitiveType.TriangleStrip, Primitives.Quad.ElementCount, DrawElementsType.UnsignedInt, 0);

            GL.UseProgram(0);
            
            GL.UseProgram(_textShader.Handle);
            
            var col = Color4.White;
            GL.Uniform4(_textShader.Uniforms["Color"].Location, col);
            
            GL.UniformMatrix4(_textShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);
            
            
            GL.Uniform2(_textShader.Uniforms["Position"].Location, pos);
            
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex.Handle);
            GL.Uniform1(_textShader.Uniforms["tex"].Location, 0);
            GL.Uniform2(_solidShader.Uniforms["Scale"].Location, scale);
            GL.DrawElements(PrimitiveType.TriangleStrip, Primitives.Quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            
            GL.BindTexture(TextureTarget.Texture2D, tex2.Handle);
            GL.Uniform1(_textShader.Uniforms["tex"].Location, 0);

            var scale2 = 0.1f * new Vector2(tex2.AspectRatio, 1.0f) * state.Camera.Current.VerticalSize * 0.15f;
            GL.Uniform2(_solidShader.Uniforms["Scale"].Location, scale2);
            
            var pos2 = pos + new Vector2(0, -scale.Y*2f);
            GL.Uniform2(_textShader.Uniforms["Position"].Location, pos2);


            var series = targetPt.Series;
            var index = state.Series.IndexOf(series);
            GL.Uniform4(_textShader.Uniforms["Color"].Location, series.Color);
                
            GL.DrawElements(PrimitiveType.TriangleStrip, Primitives.Quad.ElementCount, DrawElementsType.UnsignedInt, 0);
            
            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }
        
        
        private void DrawDragBox(CartesianGraphState<T> state) {
            var box = state.DragRectangle;
            if (box == default) {
                return;
            }
            var col = new Color4(1,1,1, 0.15f);
            // rescale into view co-ordinates
            var pt1 = box.Min * 2.0f - Vector2.One;
            var pt2 = box.Max * 2.0f - Vector2.One;
            // project mouse into world
            var inverseVp = state.Camera.Current.ViewProjection.Inverted();

            var minpt = (new Vector4(pt1.X, -pt2.Y, 0, 1) * inverseVp).Xy;
            var maxPt = (new Vector4(pt2.X, -pt1.Y, 0, 1) * inverseVp).Xy;


            var center = Vector2.Lerp(minpt, maxPt, 0.5f);
            var scale = maxPt - minpt;

            // render the background box
            GL.BindVertexArray(Primitives.Quad.Handle);
            GL.UseProgram(_solidShader.Handle);

            var vpMat = state.Camera.Current.ViewProjection;
            GL.UniformMatrix4(_solidShader.Uniforms["ViewProjectionMatrix"].Location, false, ref vpMat);

            GL.Uniform2(_solidShader.Uniforms["Position"].Location, center);
            GL.Uniform2(_solidShader.Uniforms["Scale"].Location, scale * 0.5f);

            GL.Uniform4(_solidShader.Uniforms["Color"].Location, col);

            GL.DrawElements(PrimitiveType.TriangleStrip, Primitives.Quad.ElementCount, DrawElementsType.UnsignedInt, 0);

            GL.BindVertexArray(0);
            GL.UseProgram(0);
        }


        internal void DeleteBuffers() {
        }
    }

}
