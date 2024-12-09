using UnityEngine;

namespace CharControl2D {
    internal static class Extensions {
        internal static Rigidbody2D Configure(this Rigidbody2D rigidbody2D) {
            rigidbody2D.mass = 1f;
#if UNITY_6000
            rigidbody2D.linearDamping = 0f;
            rigidbody2D.angularDamping = 0f;  
#else
            rigidbody2D.drag = 0f;
            rigidbody2D.angularDrag = 0f;  
#endif
            rigidbody2D.gravityScale = 0f;
            rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rigidbody2D.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
            return rigidbody2D;
        }
    }
}