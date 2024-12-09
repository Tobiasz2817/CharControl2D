using UnityEngine;

namespace CharControl2D {
    //TODO: Make example
    [RequireComponent(typeof(BoxCollider2D))]
    public class ExampleCollisionForge : MonoBehaviour {
        CollisionForge _col2D;

        void Awake() {
            _col2D = new CollisionForge(GetComponent<BoxCollider2D>(), 20);

            _col2D.OnCollisionEntry += CollisionEntry;
            _col2D.OnCollisionExit += CollisionExit;
        }

        void CollisionEntry(RaycastHit2D raycastHit, bool isGrounded, bool isCelling, int wallId) {
            if (!col)
                return;
            
            col = false;
        }

        
        void CollisionExit(RaycastHit2D raycastHit, bool isGrounded, bool isCelling, int wallId) {
            
        }

        bool col = false;
        void Update() {
            _col2D.TickCollision();

            if (Input.GetKeyDown(KeyCode.Space)) {
                col = true;
            }
        }

        void OnDrawGizmos() {
            return;
            _col2D.OnDrawGizmos();
        }
    }
}