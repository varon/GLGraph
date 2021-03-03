using System;
using System.Collections.Generic;
using System.Diagnostics;
using GLGraphs.CartesianGraph;
using GLGraphs.ObjectTKExtensions;
using OpenTK.Mathematics;

namespace GLGraphs.NetGraph {
    /// Holds the simulation state of the Network graph.
    /// This is separate to allow easy resets of the network state.
    internal sealed class NetworkGraphState<T> : IGraphState<T> {
        /// How strong the repulsion force is between nodes. [0, 1]
        private const float ProximityRepulsionForce = 0.025f; // between 0 and 1.

        /// How strongly the center of the space attracts nodes. [0, 1]
        private const float CenterAttractionForce = 0.001f;
        private const float CenterAttractionForceAfterStabilize = 0.1f;

        /// How strongly edges attract nodes [0; 1]
        private const float LinkAttractionForce = 1.0f;


        /// How strongly some nodes are affected by inertia [0; 1]
        private const float RandomInertiaFactor = 0.5f;

        private readonly NetworkGraphConfig _cfg;
        private readonly NetworkGraphData<T> _data;
        
        private readonly Stopwatch _wallTime = Stopwatch.StartNew();
        
        private float _runtime;

        internal Vector2[] Positions { get; }
        internal float[] Inertia { get; }
        private Vector2[] Displacements { get; }
        
        
        public DampenedCamera2D Camera { get; }
        public float ViewportWidth { get; set; }
        public float ViewportHeight { get; set; }
        public bool IsCameraAutoControlled { get; set; }
        public Box2 DragRectangle { get; set; }
        public Vector2 MousePosition { get; set; }
        public GraphPt<T>? MouseoverTarget { get; set; }


        /// The currently selected node index.
        /// Changing this will update the
        /// <see cref="SelectedItem" />
        /// automatically.
        public int SelectedIndex { get; set; } = -1;


        public NetworkGraphState(NetworkGraphData<T> data, NetworkGraphConfig cfg) {
            _cfg = cfg;
            _data = data;
            Positions = new Vector2[data.Count];
            Displacements = new Vector2[data.Count];
            Inertia = new float[data.Count];

            var r = new Random(cfg.LayoutSeed);
            // randomize the initial positions
            for (var i = 0; i < Positions.Length; i++) {
                var x = r.NextDouble() * 2.0 - 1.0;
                var y = r.NextDouble() * 2.0 - 1.0;
                Positions[i] = new Vector2((float) x * 1, (float) y * 1);
                Positions[i].Normalize();
                Positions[i] *= 5.0f;
                // more falloff for inertia.
                Inertia[i] = (float) r.NextDouble() * (float) r.NextDouble() * RandomInertiaFactor;
            }
            
            Camera = new DampenedCamera2D {
                VerticalSizeDampeningFactor = 0.5f,
                PositionDampeningFactor = 0.1f
            };
            Camera.Target.VerticalSize = 5;
            Camera.Current.VerticalSize = 5;
            Camera.Snap();
        }

        internal float[] Weights => _data.Weights;
        internal (int, int)[] Links => _data.Links;
        internal T[] Nodes => _data.Nodes;
        public int[] Categories => _data.Categories;

        /// The currently selected item. Changing this will update the
        /// <see cref="SelectedIndex" />
        /// automatically.
        public T SelectedItem {
            get
            {
                if (SelectedIndex < 0 || SelectedIndex >= _data.Count) {
                    return default;
                }

                return _data.Nodes[SelectedIndex];
            }
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, default)) {
                    SelectedIndex = -1;
                }
                else if (_data.NodeToIndex.TryGetValue(value, out var idx)) {
                    SelectedIndex = idx;
                }
                else {
                    SelectedIndex = -1;
                }
            }
        }

        internal float[,] ConnectionStrengths => _data.ConnectionStrength;



        public void FinishDrag() {
            
        }

        public void Click() {
            
        }
        public bool TryGetMouseover(Vector2 clientToView, out GraphPt<T> o) {
            o = default;
            return false;
        }

        //Fruchterman-Reingol
        //Kobourov, Stephen G. (2012), Spring Embedders and Force-Directed Graph Drawing Algorithms
        //arXiv:1201.3011 Freely accessible.
        // MODIFIED TO DYNAMIC K values based on number of adjacent nodes
        // modified to include radius-based springs
        public void Update(float timeStep) {
            Camera.Update(timeStep);
            
            // width * height
            var area = 1.0f * 1.0f;
            var baseKFactor = 0.25f * MathF.Sqrt(area / (1.0f * Positions.Length));
            // time step
            var subSamples = 10;
            var startTime = _wallTime.Elapsed;
            var lastTime = TimeSpan.Zero;
            for (var sample = 0; sample < subSamples && (_wallTime.Elapsed - startTime + lastTime) < _cfg.MaxSimulationTimePerFrame; sample++) {
                var stepStartTime = _wallTime.Elapsed;
                var t = timeStep / subSamples;

                // zero the displacements:
                Array.Clear(Displacements, 0, Displacements.Length);

                // adjust displacements based on the proximity to nodes
                for (var i = 0; i < _data.Count; i++) {
                    var leftPos = Positions[i];
                    for (var j = 0; j < _data.Count; j++) {
                        // nodes do not repel themselves.
                        if (j == i) {
                            continue;
                        }

                        // we only do one-direction per loop iteration.
                        var rightPos = Positions[j];
                        var delta = leftPos - rightPos;
                        var leftWeight = _data.Weights[i];
                        var rightWeight = _data.Weights[j];
                        var leftScale = _cfg.WeightToScale(leftWeight);
                        var rightScale = _cfg.WeightToScale(rightWeight);

                        var relativeSize = rightScale / leftScale;

                        // square falloff:
                        var scale = 1 / delta.LengthSquared * baseKFactor;
                        var dir = delta.Normalized();
                        var displacement = dir * scale * ProximityRepulsionForce;
                        Displacements[i] += displacement;
                    }
                }


                var centerAttractionForce =
                    _runtime < _cfg.TimeToStabilize.TotalSeconds
                    ? CenterAttractionForce
                    : CenterAttractionForceAfterStabilize;
                var stabTime = MathF.Min(1.0f, MathF.Floor(_runtime / (float) _cfg.TimeToStabilize.TotalSeconds));

                for (var i = 0; i < _data.Count; i++) {
                    var basePos = Positions[i];
                    //we also add a special displacement based on how far away from the center it is - there is naturally a sphere trying to repulse these inwards!
                    var pos = Vector2.Lerp(basePos * 2.0f, basePos, stabTime);
                    var aspect = Camera.Current.AspectRatio;
                    pos.X /= aspect;
                    
                    Displacements[i] -= centerAttractionForce * pos * baseKFactor;
                }

                // add attraction based on the links:
                for (var i = 0; i < _data.Links.Length; i++) {
                    var (leftIdx, rightIdx) = _data.Links[i];
                    var leftPos = Positions[leftIdx];
                    var rightPos = Positions[rightIdx];
                    var delta = leftPos - rightPos;

                    var linkStrength = 1.0f;
                    if (_cfg.UseWeightsToScaleLinkAttractionForces) {
                        linkStrength = _data.ConnectionStrength[leftIdx, rightIdx];
                    }

                    // square increase in attraction scale
                    var scale = MathF.Pow(delta.Length * linkStrength, 2);
                    var dir = delta.Normalized();
                    
                    

                    var displacement = dir * scale * LinkAttractionForce * baseKFactor;
                    Displacements[leftIdx] -= displacement;
                    Displacements[rightIdx] += displacement;
                }

                //                 
                // now adjust the positions by the displacement...
                for (var i = 0; i < _data.Count; i++) {
                    var disp = Displacements[i];
                    Positions[i] += disp.Normalized() * MathF.Min(disp.Length, t * subSamples);
                }

                _runtime += t;
                var stepEndTime = _wallTime.Elapsed;
                lastTime = stepEndTime - stepStartTime;
            }
        }
    }
}
