using ObjectTK._2D;
using OpenTK.Mathematics;

namespace GLGraphs.ObjectTKExtensions {
    
    /// As per camera2D, but with position, rotation and vertical size dampened over time.
    public sealed class DampenedCamera2D {
        /// How much dampening is taking place.
        /// Less dampening = faster motion.
        /// More dampening = smoother, but slower motion.
        public float VerticalSizeDampeningFactor{ get; set; } = 0.25f;
        public float PositionDampeningFactor { get; set; } = 0.25f;
        public float RotationDampeningFactor{ get; set; } = 0.25f;

        /// The current camera values. In general, do not set these unless you need immediate changes.
        public Camera2D Current { get; } = new Camera2D();
        
        /// The target camera values. In general, these are the values that should be set.
        public Camera2D Target { get; } = new Camera2D();

        /// Immediately snaps to all values to the target.
        public void Snap() {
            Current.Position = Target.Position;
            Current.Rotation = Target.Rotation;
            Current.VerticalSize = Target.VerticalSize;
        }

        /// Updates the values, blending the dampened ones towards the target, based on the <see cref="PositionDampeningFactor"/>
        public void Update(float timeDelta) {
            var tPos = MathHelper.Clamp(timeDelta / PositionDampeningFactor, 0, 1);
            Current.Position = Vector2.Lerp(Current.Position, Target.Position, tPos);
            
            var tRot = MathHelper.Clamp(timeDelta / RotationDampeningFactor, 0, 1);
            Current.Rotation = MathHelper.Lerp(Current.Rotation, Target.Rotation, tRot);
            
            var tVSize = MathHelper.Clamp(timeDelta / VerticalSizeDampeningFactor, 0, 1);
            Current.VerticalSize = MathHelper.Lerp(Current.VerticalSize, Target.VerticalSize, tVSize);
        }
    }
}
