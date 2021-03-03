using System;
using System.Collections.Generic;
using GLGraphs.ObjectTKExtensions;
using GLGraphs.Utils;
using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace GLGraphs.CartesianGraph {

    /// Contains the grid-line spacing for one axis.
    public sealed class GridLineSpacing : CleanObject {

        /// The distance between major lines on this axis.
        /// Don't forget to set <see cref="Automatic"/> to true for this to work.
        public float Major { get; set; } = 0.1f;        

        /// The distance between major lines on this axis.
        /// Don't forget to set <see cref="Automatic"/> to true for this to work.
        public float Minor { get; set; } = 0.02f;

        /// If the major/minor distances are controlled automatically for this axis.
        /// If this is set to true, then Major/Minor are automatically set each update.
        public bool Automatic { get; set; } = true;

        internal GridLineSpacing() { }
    }


    public sealed class CartesianGraphState<T> : CleanObject, IGraphState<T> {
        private readonly CartesianGraphSettings _cfg;

        public const float DefaultCameraZoom = 1.0f;
        
        private readonly List<GraphSeries<T>> _series = new List<GraphSeries<T>>();
        private readonly List<GraphRegion> _regions = new List<GraphRegion>();

        internal CartesianGraphState(CartesianGraphSettings cfg) {
            _cfg = cfg;
            Camera = new DampenedCamera2D {
                VerticalSizeDampeningFactor = 0.5f,
                PositionDampeningFactor = 0.1f
            };
            Camera.Target.VerticalSize = DefaultCameraZoom;
            Camera.Current.VerticalSize = DefaultCameraZoom;
            Camera.Snap();
        }

        /// The camera used to display this scatter graph.
        /// This should be manipulated via zoom and other functions.
        [NotNull]
        public DampenedCamera2D Camera { get; }

        /// The viewport width in pixels.
        public float ViewportWidth { get; set; } = -1;

        /// The viewport height in pixels.
        public float ViewportHeight { get; set; } = -1;

        // public GridLineSpacing GridLineSpacing { get; } = new GridLineSpacing();
        
        /// The number of major grid based on the range of the data displayed.
        public int AutoMajorGridDivisions { get; set; } = 10;
        /// The number of minor grid divisions (per major division).
        public int AutoMinorGridDivisions { get; set; } = 5;

        /// If non-uniform scaling is available on the Y axis, then this is the current Y-Scale value.
        /// TODO: animate this value.
        public float YScale { get; private set; } = 1.0f;

        /// The bounds of all of the visible data in this graph.
        public Box2 Bounds { get; private set; }
        
        /// Manual overrides for the X axis grid line spacing.
        /// Don't forget to set <see cref="GridLineSpacing.Automatic"/> to false if you wish to configure these manually.
        public GridLineSpacing XGridSpacing { get; private set; } = new GridLineSpacing();
        
        /// Manual overrides for the Y axis grid line spacing.
        /// Don't forget to set <see cref="GridLineSpacing.Automatic"/> to false if you wish to configure these manually. 
        public GridLineSpacing YGridSpacing { get; private set; } = new GridLineSpacing();

        /// The mouse position in view space [-1;1]
        public Vector2 MousePosition { get; set; } = Vector2.Zero;

        GraphPt<T>? IGraphState<T>.MouseoverTarget {
            get => MouseoverTarget;
            set => MouseoverTarget = value;
        }

        /// if the mouse is currently over the graph (and the tooltip should be displayed)
        public GraphPt<T>? MouseoverTarget { get; set; }
        
        /// The drag rectangle in view space [-1;1]
        public Box2 DragRectangle { get; set; }

        /// Event fired when a drag selection is finished.
        public event Action<IReadOnlyList<T>> DragSelected;
        
        /// Event fired when an item is clicked.
        public event Action<T> ItemSelected;
        
        /// Collection of all of the data series in this graph.
        public IReadOnlyList<GraphSeries<T>> Series => _series;
        
        /// Collection of all of the data series in this graph.
        public IReadOnlyList<GraphRegion> Regions => _regions;

        /// If the camera's zoom and position is automatically controlled.
        public bool IsCameraAutoControlled { get; set; } = true;

        /// Adds a new series with the specified name to the Scatter graph.
        [NotNull]
        public GraphSeries<T> AddSeries(SeriesType seriesType, string name) {
            var col = _cfg.SeriesColors[_series.Count % _cfg.SeriesColors.Length];
            var s = new GraphSeries<T>(seriesType, name, col);
            _series.Add(s);
            return s;
        }
        
        /// Adds a new region to the scatter graph.
        [NotNull]
        public GraphRegion AddRegion(Box2 bounds, Color4 col) {
            var s = new GraphRegion {Bounds = bounds, Color = col};
            _regions.Add(s);
            return s;
        }

        /// Removes all graph regions from the scatter graph.
        public void ClearRegions() {
            _regions.Clear();
        }
        
        /// Advances the scatter graph simulation by the specified time.
        /// This does not require any access to OpenGL resources and may be run in a separate thread, assuming that proper synchronization is observed.
        public void Update(float t) {
            UpdateGridSpacing();
            if (IsCameraAutoControlled) {
                var ySize = 1.2f*MathF.Max(MathF.Abs(Bounds.Max.Y), MathF.Abs(Bounds.Min.Y)) * YScale;
                var xSize = 1.2f * MathF.Max(MathF.Abs(Bounds.Max.X), MathF.Abs(Bounds.Min.X));
                Camera.Target.VerticalSize = MathF.Max(ySize, xSize);
                var target = -Bounds.Center;
                target.Y *= YScale;
                Camera.Target.Position = target;
            }

            Camera.Update(t);
        }
        
        // figure out and update the grid spacing if required.
        private void UpdateGridSpacing() {
            static double RoundToNearest10(double d) {
                var pow = (int) Math.Ceiling(Math.Log10(d));
                return Math.Pow(10, pow);
            }
            static double FindNearest10ForGridLine(double d) {
                return RoundToNearest10(d * 100) / 100;
            }
            
            
            var minX = 0.0;
            var maxX = 0.0;

            var minY = 0.0;
            var maxY = 0.0;

            foreach (var s in Series) {
                if (!s.IsVisible || s.Points.Count == 0 || double.IsNaN(s.XStats.Minimum) || double.IsNaN(s.YStats.Minimum)) {
                    continue;
                }

                minX = Math.Min(s.XStats.Minimum, minX);
                maxX = Math.Max(s.XStats.Maximum, maxX);

                minY = Math.Min(s.YStats.Minimum, minY);
                maxY = Math.Max(s.YStats.Maximum, maxY);
            }

            if (minX == maxX) {
                minX -= 1;
                maxX += 1;
            }
            
            if (minY == maxY) {
                minY -= 1;
                maxY += 1;
            }

            
            Bounds = new Box2(new Vector2((float) minX, (float) minY),new Vector2((float) maxX, (float) maxY));
            
            var yRange = FindNearest10ForGridLine(maxY - minY);
            var xRange = FindNearest10ForGridLine(maxX - minX);
            
            if (_cfg.ForceSquareGrid) {
                xRange = Math.Max(xRange, yRange);
                yRange = xRange;
            }

            var majorSpacingY = (float) yRange / AutoMajorGridDivisions;
            var minorSpacingY = majorSpacingY / AutoMinorGridDivisions;


            var majorSpacingX = (float) xRange / AutoMajorGridDivisions;
            var minorSpacingX = majorSpacingX / AutoMinorGridDivisions;

            if (XGridSpacing.Automatic) {
                XGridSpacing.Major = majorSpacingX;
                XGridSpacing.Minor = minorSpacingX;
            }
            
            if (YGridSpacing.Automatic) {
                YGridSpacing.Major = majorSpacingY;
                YGridSpacing.Minor = minorSpacingY;
            }

            if (minorSpacingY != 0.0f) {
                YScale = minorSpacingX / minorSpacingY;
            }
        }



        [Pure]
        public bool TryGetMouseover(Vector2 mousePosViewSpace, out GraphPt<T> mouseOver) {
            // rescale into view co-ordinates
            var (x, y) = mousePosViewSpace * 2.0f - Vector2.One;
            // project mouse into world
            
            var vpMat = Camera.Current.ViewProjection;
            vpMat = Matrix4.CreateScale(1.0f, YScale, 1.0f) * vpMat;
            vpMat.Invert();

            var worldPos = (new Vector4(x, -y, 0, 1) * vpMat).Xy;

            var best = -1;
            var bestPt = default(GraphPt<T>);
            var bestDist = 9999.9f;
            
            var scale = Camera.Current.VerticalSize / ViewportHeight * _cfg.PointSize;

            // iterate in drawing (reverse) order to resolve the problem of points being overlaid.
            for (var i = Series.Count - 1; i >= 0; i--) {
                var series = Series[i];
                if (!series.IsVisible) {
                    continue;
                }

                for (int j = series.Points.Count - 1; j >= 0; j--) {
                    var pt = series.Points[j];
                    var delta = pt.Position - worldPos;
                    delta.Y *= YScale;
                    var dist = delta.Length;
                    if (dist < scale && dist < bestDist) {
                        best = j;
                        bestPt = pt;
                        bestDist = dist;
                    }
                }
            }

            mouseOver = bestPt;
            return best != -1;
        }

        public void FinishDrag() {
            
            // rescale into view co-ordinates
            var inRange = new List<T>();

            var min = DragRectangle.Min * 2 - Vector2.One;
            var max = DragRectangle.Max * 2 - Vector2.One;
            
            var vpMat = Camera.Current.ViewProjection;
            vpMat = Matrix4.CreateScale(1.0f, YScale, 1.0f) * vpMat;
            vpMat.Invert();
            
            var minWorld = (new Vector4(min.X, -min.Y, 0, 1) * vpMat).Xy;
            var maxWorld = (new Vector4(max.X, -max.Y, 0, 1) * vpMat).Xy;
            
            var targetRect = new Box2(minWorld, maxWorld);
            
            
            // iterate in drawing (reverse) order to resolve the problem of points being overlaid.
            for (var i = Series.Count - 1; i >= 0; i--) {
                var series = Series[i];
                if (!series.IsVisible) {
                    continue;
                }

                for (int j = series.Points.Count - 1; j >= 0; j--) {
                    var pt = series.Points[j];

                    if (targetRect.Contains(pt.Position, true)) {
                        inRange.Add(pt.Value);
                    }
                }
            }
            DragSelected?.Invoke(inRange);
            // null-out the drag rectangle.
            DragRectangle = default;
        }


        public void Click() {
            if (MouseoverTarget != null) {
                ItemSelected?.Invoke(MouseoverTarget.Value.Value);
            }
        }

    }
}
