using GLGraphs.ObjectTKExtensions;
using OpenTK.Mathematics;

namespace GLGraphs.CartesianGraph {
    public interface IGraphState<T> {
        /// The camera used to display this graph.
        /// This should be manipulated via zoom and other functions.
        DampenedCamera2D Camera { get; }

        /// The viewport width in pixels.
        float ViewportWidth { get; set; }

        /// The viewport height in pixels.
        float ViewportHeight { get; set; }

        bool IsCameraAutoControlled { get; set; }
        
        Box2 DragRectangle { get; set; }
        
        Vector2 MousePosition { get; set; }
        
        GraphPt<T>? MouseoverTarget { get; set; }

        /// Advances the graph simulation by the specified time.
        void Update(float t);

        void FinishDrag();
        
        void Click();
        
        /// Retrieves the current mouseover item.
        /// This requires the mouse position in VIEW space. This is a co-ordinate system where:
        /// Top left is (0,0) and bottom right is (1,1).
        /// To get to this from screen (pixel) co-ordinates, divide x/y by screen width/height respectively, then flip the Y value. 
        bool TryGetMouseover(Vector2 clientToView, out GraphPt<T> o);
    }
}