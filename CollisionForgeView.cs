using UnityEngine;

namespace CharControl2D {
    [RequireComponent(typeof(BoxCollider2D))]
    public class CollisionForgeView : MonoBehaviour {
        CollisionForge _forge;

        public bool IsGrounded;
        public bool IsWalling;
        public bool IsCelling;
        
        void Awake() {
            _forge = new CollisionForge(GetComponent<BoxCollider2D>(), 20);
        }

        void Update() {
            _forge.TickCollision();
            IsGrounded = _forge.IsGrounded;
            IsWalling = _forge.IsWalling;
            IsCelling = _forge.IsCelling;
        }
    }
}