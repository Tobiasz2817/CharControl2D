using System;
using System.Linq;
using CoreUtility;
using CoreUtility.Extensions;
using UnityEngine;

namespace CharControl2D {
    public class CollisionForge {
        internal const int CollisionCount = 10;
        
        internal readonly RaycastHit2D[] Hits;
        internal RaycastHit2D[] Last;
        
        public Action<RaycastHit2D, bool, bool, int> OnCollisionEntry;
        public Action<RaycastHit2D, bool, bool, int> OnCollisionStay;
        public Action<RaycastHit2D, bool, bool, int> OnCollisionExit;
        
        public bool IsColliding => IsGrounded || IsWalling || IsCelling;
        public bool IsGrounded;
        public bool IsWalling => WallDir != 0;
        public bool IsCelling;
        public int WallDir;
        public int HitCount;
        
        BoxCollider2D _col2D;
        float _sizeOffset;

        public CollisionForge(BoxCollider2D collider2D, int hitsMaxCount, float sizeOffset = 0.1f) {
            _col2D = collider2D;
            _sizeOffset = sizeOffset;
            
            Hits = new RaycastHit2D[hitsMaxCount];
            Last = new RaycastHit2D[hitsMaxCount];
        }
        
        public void TickCollision() {
            FetchCollisions();
            
            ProcessOnExit();
            RemoveSelfCollision();
            ProcessOnEnter();

            FillLastArray();
        }

        void ProcessOnEnter() {
            var states = (false, 0, false);
            for (var i = 0; i < HitCount; i++) {
                var hit = Hits[i];

                var prevState = (IsGrounded, WallDir, IsCelling);
                var contacts = Utility.GetSurfaceContacts(hit.normal);
                // Can upgrade state when new state is true and before was false
                states.Item1 = states.Item1 || contacts.Item1;
                states.Item2 = states.Item2 != 0 ? states.Item2 : contacts.Item2;
                states.Item3 = states.Item3 || contacts.Item3;
                
                IsGrounded = contacts.Item1 || IsGrounded;
                WallDir = contacts.Item2 != 0 ? contacts.Item2 : WallDir;
                IsCelling = contacts.Item3 || IsCelling;
                
                // Notify entry/exit
                OnCollisionStay?.Invoke(hit, contacts.Item1, contacts.Item3, contacts.Item2);
                if (IsGrounded && !prevState.IsGrounded || 
                    IsCelling && !prevState.IsCelling || 
                    WallDir != 0 && prevState.WallDir == 0)
                    
                    OnCollisionEntry?.Invoke(hit, contacts.Item1, contacts.Item3, contacts.Item2);
            }

            IsGrounded = states.Item1;
            WallDir = states.Item2;
            IsCelling = states.Item3;
        }
        
        void ProcessOnExit() {
            foreach (var lastHit in Last) {
                if (lastHit == default)
                    break;
                
                if (ContainsHit(lastHit))
                    continue;
                
                var contacts = Utility.GetSurfaceContacts(lastHit.normal);
                OnCollisionExit?.Invoke(lastHit, contacts.Item1, contacts.Item3, contacts.Item2); 
            }
        }
        
        void RemoveSelfCollision() {
            int validHitCount = 0;
            for (int i = 0; i < HitCount; i++) {
                if (Hits[i].transform == _col2D.transform) 
                    continue;
                
                Hits[validHitCount] = Hits[i];
                validHitCount++;
            }

            Hits[validHitCount] = default;
            
            for (var i = HitCount; i < Hits.Length; i++) 
                Hits[i] = default;
        }

        bool ContainsHit(RaycastHit2D t2) =>
            Hits.TakeWhile(t1 => t1 != default).Any(t1 => t1.collider == t2.collider && t1.normal == t2.normal);
        
        void FetchCollisions() =>
            HitCount = Physics2D.BoxCastNonAlloc(_col2D.bounds.center, _col2D.bounds.size.AddAll(_sizeOffset),
                _col2D.transform.eulerAngles.z, Vector2.zero, Hits);

        void FillLastArray() =>
            Array.Copy(Hits, Last, HitCount);
        
        public void OnDrawGizmos() {
            if (_col2D == null) 
                return;
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(_col2D.bounds.center, _col2D.bounds.size);

            for (var i = 0; i < HitCount; i++) {
                if (Hits[i].collider == null || Hits[i].transform.Equals(_col2D.transform)) 
                    continue;
                
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(Hits[i].point, 0.1f);
            }
        }
        public override string ToString() =>
            "G: " + IsGrounded + " W: " + IsWalling + " " + WallDir + " C: " + IsCelling;
    }
}
