using System;
using System.Linq;
using GLGraphs.Utils;
using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace GLGraphs.NetGraph {
    
    /// The possible display modes for labels. Used to customize if/when they are visible.
    public enum LabelDisplayMode {
        /// The labels are never shown under any circumstances
        Never,
        /// The labels are always shown
        Always,
        /// Only the label for the selected item is shown
        Selected,
        /// The label is shown for the selected item, and all nodes directly connected to it.
        SelectedAndAdjacent,
    }
    
    /// How the labels are displayed as zooming in/out occurs.
    public enum LabelScaleMode {
        /// The labels are always shown at a fixed size (relative to the screen and node size).
        /// They WILL NOT change size as you zoom in/out.
        Fixed,
        /// The labels are rendered proportional to the circle size.
        /// They WILL change size as you zoom in/out.
        Scaled,
        
    }

    
    /// Configuration type for the network graph.
    /// While the Default should work perfectly, be cautious as to how/when this is modified.
    public sealed class NetworkGraphConfig : CleanObject {
        /// The background color of the chart
        public Color4 BackgroundColor { get; set; } = new Color4(0x21, 0x21, 0x21, 0xff);

        /// The category colours.
        /// If there are more categories than colours listed in the array, the categories will use modulus (%) to loop back to the first colors.
        public Color4[] CategoryColors { get; set; } = {new Color4(0x00, 0xb0, 0xff, 0xff)};

        /// The colour used to highlight objects on mouseover
        public Color4 SelectedCol { get; set; } = new Color4(0xff, 0x3d, 0x00, 0xff);

        /// The color used for the labels of objects on mouseover.
        public Color4 SelectedLabelCol { get; set; } = Color4.White;

        /// The colour used to draw the links
        public Color4 LinkColor { get; set; } = Color4.White;
        
        /// The colour used to draw the labels
        public Color4 LabelColor { get; set; } = Color4.White;

        /// The random seed used to determine initial positions.
        /// Change this if you want a different graph arrangement with the same data.
        public int LayoutSeed { get; set; }

        /// When running the simulation, we simulate in an unstable, non-converging way to help unfold the graph first.
        /// Which we stabilize after this time by moving to a converging algorithm instead.
        /// Increasing this can help unfold the graph better.
        public TimeSpan TimeToStabilize { get; set; } = TimeSpan.FromSeconds(2.0f);

        /// The number of smaller steps each time step is divided into.
        /// This can be adjusted lower to increase performance at the possible expense of accuracy.
        /// Default value should be okay for the vast majority of use cases.
        public int SubsamplesPerTimestep { get; set; } = 10;

        /// The size a node at weight 0 is displayed at.
        public float MinNodeScale { get; set; } = 0.1f;

        /// The size a node at weight 1 is displayed at.
        public float MaxNodeScale { get; set; } = 1.0f;

        /// if the graph weights are used to scale the attraction forces.
        /// This should be left off in almost all circumstances except incredibly connected graphs that otherwise cannot untangle themselves.
        public bool UseWeightsToScaleLinkAttractionForces { get; set; } = false;

        /// The default configuration to use.
        public static NetworkGraphConfig Default => new NetworkGraphConfig();

        /// The scale used to render the text at, relative to the node size.
        public float TextScale { get; set; } = 0.5f;

        /// The way in which the labels of each item are displayed (or not).
        public LabelDisplayMode LabelDisplayMode { get; set; } = LabelDisplayMode.SelectedAndAdjacent;
        
        /// How the labels respond to zooming in/out on the graph.
        public LabelScaleMode LabelScaleMode { get; set; } = LabelScaleMode.Fixed;
        
        /// The maximum amount of simulation time that should not be exceeded per frame.
        public TimeSpan MaxSimulationTimePerFrame { get; set; } = TimeSpan.FromMilliseconds(10);

        [Pure]
        internal float WeightToScale(float w) {
            return 0.2f * MathHelper.Lerp(MinNodeScale, MaxNodeScale, w);
        }

        [Pure]
        [NotNull]
        public NetworkGraphConfig Copy() {
            return new NetworkGraphConfig {
                BackgroundColor = BackgroundColor,
                CategoryColors = CategoryColors.ToArray(),
                LayoutSeed = LayoutSeed,
                LinkColor = LinkColor,
                LabelColor = LabelColor,
                SelectedCol = SelectedCol,
                MaxNodeScale = MaxNodeScale,
                MinNodeScale = MinNodeScale,
                TimeToStabilize = TimeToStabilize,
                SubsamplesPerTimestep = SubsamplesPerTimestep,
                UseWeightsToScaleLinkAttractionForces = UseWeightsToScaleLinkAttractionForces,
                TextScale = TextScale,
                LabelDisplayMode = LabelDisplayMode,
                LabelScaleMode = LabelScaleMode,
                SelectedLabelCol = SelectedLabelCol,
                MaxSimulationTimePerFrame = MaxSimulationTimePerFrame
            };
        }
    }
}
