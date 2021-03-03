using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace GLGraphs.Utils {
    
    /// An object with default methods hidden (but still usable) for a cleaner public API.
    public abstract class CleanObject {

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            // ReSharper disable once BaseObjectEqualsIsObjectEquals
            return base.Equals(obj);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
            return base.GetHashCode();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        // ReSharper disable once AnnotateCanBeNullTypeMember
        public override string ToString()
        {
            return base.ToString();
        }
            
        [EditorBrowsable(EditorBrowsableState.Never)]
        [NotNull]
        // ReSharper disable once UnusedMember.Global
        public new Type GetType() {
            return base.GetType();
        }
        
        [MethodImpl(MethodImplOptions.InternalCall)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected new extern object MemberwiseClone();
    }
}
