using System;
using System.Collections;
using CoreUtility;
using CoreUtility.Extensions;
using Signals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CharControl2D {
    [DefaultExecutionOrder(-500)]
    public class CharController : MonoBehaviour {
        [Header("Collision")]
        [SerializeField] LayerMask _collisionMask;
        [SerializeField] float _collisionThreshold = 0.1f;
        
        [Header("Controller Settings")]
        [SerializeField] CharControllerData Data;
        
        [Header("Controller Input")]
        [SerializeField] InputActionAsset InputAsset;
        [SerializeField] InputSheet InputSheet = InputSheet.Default();
        [SerializeField] InputDevice InputDevice = InputDevice.Keyboard | InputDevice.XboxController;

        Rigidbody2D _rb2D;
        CollisionForge _col2D;
        InputData _input;
        
        public CollisionForge Col2D => _col2D;
        public InputData Input => _input;
        
        #region Conditions

        public bool CanMove { private set; get; }
        public bool CanJump => (_jumpCoyoteTime > 0f && _currentJumpCount == 0) || (_currentJumpCount < Data.JumpCount && _currentJumpCount != 0);
        public bool CanDash => IsDashReady && IsDashReset && !IsDashing;
        public bool IsDashReset { protected set; get; }
        public bool IsDashReady { protected set; get; }
        public bool IsJumpCut { protected set; get; }
        public bool IsJumping => _rb2D.velocity.y > 0;     
        public bool IsDashing { protected set; get; }
        public bool IsCrouch { private set; get; }
        public bool IsFalling => _rb2D.velocity.y < 0f && !_col2D.IsGrounded;
        public bool IsStanding => Mathf.Approximately(_rb2D.velocity.y, 0.05f);

        #endregion

        #region Unity Callbacks

        void Awake() {
            var boxCollider2D = gameObject.GetOrAdd<BoxCollider2D>().WithLayer(_collisionMask);
            _rb2D = gameObject.GetOrAdd<Rigidbody2D>().Configure();
            _col2D = new CollisionForge(boxCollider2D, CollisionForge.CollisionCount);
            _request = new RequestSystem();
            
#if ENABLE_INPUT_SYSTEM
            CharInput.RegistryControllerListeners(this, InputAsset, InputSheet);
#endif
        }
        
        void OnValidate() {
#if ENABLE_INPUT_SYSTEM
            if (!InputAsset)
                return;
            
            CharInput.ConfigureInputAsset(InputAsset, InputSheet, InputDevice);
#endif
        }

        void Start() {
            InitConditions();
            RegistryCallbacks();
        }
            

        void Update() {
            TickConditions();
            Flip();
            
            _request.Tick();
            
            // Set the gravity when is not the same
            _rb2D.SetGravityScale(GetGravity());
            // Not falling when gravity is equal 0
            _rb2D.SetVelocity(y: Mathf.Max(_rb2D.velocity.y, -Data.GravityMaxFall), condition: _rb2D.gravityScale != 0);
        }
        
        void FixedUpdate() {
            _request.TickFixed();
            _col2D.TickCollision();
            
            if (CanMove) {
                Move();
            }
            
            // Wall
            
            // Slide
        }

#if UNITY_EDITOR
        void OnDrawGizmos() =>
            _col2D?.OnDrawGizmos();
#endif

        #endregion

        #region Initialize

        void RegistryCallbacks() {
            _col2D.OnCollisionEntry -= CollisionEntry;
            _col2D.OnCollisionExit -= CollisionExit;
            _col2D.OnCollisionEntry += CollisionEntry;
            _col2D.OnCollisionExit += CollisionExit;
        }
        
        void InitConditions() {
            CanMove = true;
            IsDashReset = true;
            IsDashReady = true;
            IsJumpCut = false;
            _rb2D.gravityScale = Data.GravityScale;
            _jumpCoyoteTime = Data.JumpCoyoteTime;
        }
        
        void TickConditions() {
            if(!_col2D.IsColliding && _jumpCoyoteTime > 0)
                _jumpCoyoteTime = Mathf.Clamp(_jumpCoyoteTime - Time.deltaTime, 0f, float.MaxValue);
        }

        #endregion
        
        #region Collision
        
        void CollisionEntry(RaycastHit2D raycastHit, bool isGrounded, bool isCelling, int wallDir) {
            if (isGrounded || wallDir != 0) {
                IsJumpCut = false;  
                _currentJumpCount = _col2D.IsGrounded ? 0 : 1;
                _jumpCoyoteTime = Data.JumpCoyoteTime;
            }

            IsDashReset = true;
        }

        
        void CollisionExit(RaycastHit2D raycastHit, bool isGrounded, bool isCelling, int wallDir) { }

        #endregion
        #region Signals

        // Processed by Signals
        void OnMovementCondition(bool condition) {
            CanMove = condition;
        }

        #endregion
        #region Buffer Request

        RequestSystem _request;
        
        public void RequestCutJump() {
            if (!IsJumping)
                return;
            
            _request.Force(new RequestData{
                Action = () => IsJumpCut = true,
                Condition = () => CutJumpCondition,
                BufferTime = Data.JumpTimeToApex,
            });
        }

        public void RequestJump() {
            _request.Force(new RequestData{
                Action = Jump,
                Condition = () => CanJump,
                BufferTime = Data.JumpInputBuffer,
            });
        }

        public void RequestDash() {
            _request.Force(new RequestData{
                Action = () => {
                    ProcessDashCooldown();
                    PrepareDash();
                },
                Condition = () => CanDash,
                BufferTime = Data.DashInputBuffer,
                Event = new RequestCallbacks{
                    CallbacksTime = Data.DashDuration,
                    End = StopDash,
                    TickFixed = Dash
                }
            });
        }
        
        #endregion
        #region Gravity

        float GetGravity() {
            // Dash fall condition
            if (IsDashing)
                return Data.GravityDashFall;
                
            // Cut jump fall condition
            if (IsJumping && IsJumpCut && !_col2D.IsGrounded) 
                return Data.GravityJumpCutFall;
            
            // Fall condition
            if (!_col2D.IsGrounded && _rb2D.velocity.y <= Data.VelocityFallThreshold && !_rb2D.IsGravityEqual(Data.GravityJumpCutFall)) 
                return Data.GravityJumpFall;
            
            // Flying/default condition
            if (_col2D.IsGrounded || IsJumping) 
                return Data.GravityScale;
            
            return _rb2D.gravityScale;
        }

        #endregion
        #region Input

        public void UpdateInput(Vector3? direction = null, bool? isJumpHolding = null) {
            _input.Direction = direction ?? _input.Direction;
            _input.IsJumpHolding = isJumpHolding ?? _input.IsJumpHolding;
        }

        #endregion
        
        // States
        #region Jump

        float _jumpProgressTime;
        float _jumpCoyoteTime;
        int _currentJumpCount;
        
        void Jump() {
            IsJumpCut = false;
            _currentJumpCount++;
            
            StaticTimer.RunCountdown(Data.JumpTimeToApex,
                onTick: progress => { _jumpProgressTime = 1 - progress; },
                onComplete: () => { _jumpProgressTime = 0f; },
                condition: () => IsJumpCut);
            
            _rb2D.velocity = _rb2D.velocity.With(y: 0f);
            _rb2D.AddForce(Data.JumpForce * Vector2.up, ForceMode2D.Impulse);
            BusSignals.CharacterJump(_currentJumpCount);
        }

        bool CutJumpCondition => _jumpProgressTime >= Data.MinJumpCutPercent &&
                                 _jumpProgressTime <= Data.MaxJumpCutPercent && !_input.IsJumpHolding &&
                                 !(_col2D.IsGrounded || IsFalling);

        #endregion

        #region Movement


        public void BlockMovement() {
            CanMove = false;
        }

        public void UnlockMovement() {
            CanMove = true;
        }
        
        void Move() {
            var targetSpeed = _input.Direction.x * Data.MoveMaxSpeed;
            targetSpeed = Mathf.Lerp(_rb2D.velocity.x, targetSpeed, IsStanding && IsCrouch ? 0.5f : 1f);

            var accelRate = Mathf.Abs(targetSpeed) > 0.01f ? Data.MoveAccel : Data.MoveDecel;
            accelRate *= IsFalling || IsJumping ? Mathf.Abs(targetSpeed) > 0.01f ? Data.MoveAirAcceleration : Data.MoveAirDeceleration : 1f;
            
            var speedDif = targetSpeed - _rb2D.velocity.x;
            var moveSpeed = speedDif * accelRate;
            
            _rb2D.AddForce(moveSpeed * Vector2.right, ForceMode2D.Force);
        }

        void Flip() {
            var dir = _input.Direction.x;
            var flipValue = dir > 0 ? 1 : -1;
            if (dir == 0 || Mathf.Approximately(_rb2D.transform.localScale.x, dir)) return;

            var localScale = _rb2D.transform.localScale;
            _rb2D.transform.localScale = localScale.With(x: flipValue);
        }

        #endregion

        #region Dash

        Coroutine _dashCooldown;

        void Dash(float progress) {
            var velocity = Mathf.Clamp(
                Mathf.Clamp01(Data.DashCurve.Evaluate(progress / Data.DashDuration)) * Data.DashSpeed * transform.localScale.x,
                -Data.DashSpeed, 
                Data.DashSpeed);
                
            _rb2D.velocity = new Vector2(velocity, 0);
        }

        IEnumerator DashCooldown() {
            IsDashReady = false;
            yield return new WaitForSeconds(Data.DashCooldown);
            IsDashReady = true;

            if (Col2D.IsGrounded)
                IsDashReset = true;
        }

        void ProcessDashCooldown() {
            if(_dashCooldown != null)
                StaticCoroutine.AbortCoroutine(_dashCooldown);

            _dashCooldown = StaticCoroutine.RunCoroutine(DashCooldown());
        }

        void PrepareDash() {
            IsDashing = true;
            IsDashReset = false;
            CanMove = false;
            _rb2D.velocity = _rb2D.velocity.With(y: 0f);
        }

        void StopDash() {
            CanMove = true;
            IsDashing = false;
        }

        #endregion
    }

    
    [Serializable]
    public struct CharRules {
        public bool CanDashInterruptJump;
        public bool CanJumpInterruptDash;

        public void Initialize() {
            CanDashInterruptJump  = false;
            CanJumpInterruptDash = false;
        }
    }
}