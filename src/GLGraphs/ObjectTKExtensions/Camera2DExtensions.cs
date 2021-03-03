using JetBrains.Annotations;
using ObjectTK._2D;

namespace GLGraphs.ObjectTKExtensions {
    public static class Camera2DExtensions {
        
        /// 'Zooms in' the camera by a percentage by manipulating the VerticalSize relative to its current value.
        /// Zoom delta is expressed in percent:
        /// 1.0 = 1%.
        /// 100.0 = 100% Zoom.
        public static void ZoomIn([NotNull] this Camera2D cam, float zoomDelta) {            
            cam.VerticalSize += zoomDelta * cam.VerticalSize / 100.0f;
        }
        
    }
}
