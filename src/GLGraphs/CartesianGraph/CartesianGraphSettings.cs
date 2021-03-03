using System.Linq;
using GLGraphs.Utils;
using JetBrains.Annotations;
using OpenTK.Mathematics;

namespace GLGraphs.CartesianGraph {

    public sealed class AxisConfig : CleanObject {

        public bool MajorVisibile { get; set; } = true;
        public bool MinorVisible { get; set; } = true;
        public bool OriginVisible { get; set; } = true;

        internal AxisConfig() {
        }

        [Pure]
        [NotNull]
        internal AxisConfig Copy() {
            return new AxisConfig {
                MajorVisibile = MajorVisibile,
                MinorVisible = MinorVisible,
                OriginVisible = OriginVisible
            };
        }
        
    }
    
    /// Configuration type for the scatter graph.
    public sealed class CartesianGraphSettings : CleanObject {
        
        /// The background color of the chart
        public Color4 BackgroundColor { get; set; } = new Color4(0x21, 0x21, 0x21, 0xff);

        /// The category colours.
        /// If there are more categories than colours listed in the array, the categories will use modulus (%) to loop back to the first colors.
        public Color4[] SeriesColors { get; set; } = {
            ColorHelper.Parse("#f44336"),
            ColorHelper.Parse("#9c27b0"),
            ColorHelper.Parse("#3f51b5"),
            ColorHelper.Parse("#03a9f4"),
            ColorHelper.Parse("#009688"),
            ColorHelper.Parse("#8bc34a"),
            ColorHelper.Parse("#ffeb3b"),
            ColorHelper.Parse("#ff9800"),
        };

        /// The colour used to highlight objects on mouseover
        public Color4 SelectedCol { get; set; } = new Color4(0xff, 0x3d, 0x00, 0xff);

        /// If points on the graph can be selected (or not).
        public GraphSelectionMode SelectionMode { get; set; } = GraphSelectionMode.Click;

        /// The colour used to draw the label
        public Color4 LabelColor { get; set; } = Color4.White;

        public bool ForceSquareGrid { get; set; } = false;

        /// The default configuration to use.
        public static CartesianGraphSettings Default => new CartesianGraphSettings();

        /// The scale used to render the text at.
        public float TextScale { get; set; } = 0.5f;

        /// How large points are (in pixels, diameter). This value is clamped to the max supported by the hardware.
        public float PointSize { get; set; } = 7;
        
        /// How wide the lines are (in pixels). This value is clamped to the max supported by the hardware.
        public float LineSize { get; set; } = 3;
        
        public AxisConfig XAxis { get; private set; } = new AxisConfig();
        
        public AxisConfig YAxis { get; private set; } = new AxisConfig();
        
        [Pure]
        [NotNull]
        public CartesianGraphSettings Copy() {
            return new CartesianGraphSettings {
                BackgroundColor = BackgroundColor,
                SeriesColors = SeriesColors.ToArray(),
                LabelColor = LabelColor,
                SelectedCol = SelectedCol,
                TextScale = TextScale,
                PointSize = PointSize,
                LineSize = LineSize,
                SelectionMode = SelectionMode,
                ForceSquareGrid = ForceSquareGrid,
                XAxis = XAxis.Copy(),
                YAxis = YAxis.Copy(),
            };
        }
    }

    public enum GraphSelectionMode {
        None = 0,
        Click = 1 << 0,
        Drag =  1 << 2,
        ClickAndDrag = Click & Drag
    }
}
