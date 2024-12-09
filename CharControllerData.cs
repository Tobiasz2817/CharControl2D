using UnityEngine;

namespace CharControl2D
{
    [CreateAssetMenu(menuName = "Content/Data/CharControl")]
    public class CharControllerData : ScriptableObject
    {
        [field: Header("Gravity")]
        [field: SerializeField] public float GravityMaxFall { private set; get; } = 30;
        public float GravityStrength { private set; get; }
        public float GravityScale { private set; get; }

        [field: Header("Movement")]
        
        [field: SerializeField] public float MoveAcceleration { private set; get; } = 11;
        [field: SerializeField] public float MoveDeceleration { private set; get; } = 11;
        [field: SerializeField] public float MoveMaxSpeed { private set; get; } = 11;
        [field: SerializeField] [field: Range(0f, 1)] public float MoveAirAcceleration { private set; get; } = 0.4f;
        [field: SerializeField] [field: Range(0f, 1)] public float MoveAirDeceleration { private set; get; } = 0.4f;
        public float MoveAccel { private set; get; }
        public float MoveDecel { private set; get; }
        
        [field: Header("Jump")]
        [field: Tooltip("Count of the jump")]
        [field: SerializeField] public int JumpCount { private set; get; } = 2;
        [field: Tooltip("Distance from the ground. Jump length")]
        [field: SerializeField] public float JumpHeight { private set; get; } = 3;
        [field: Tooltip("Time to reach the max jump apex")]
        [field: SerializeField] public float JumpTimeToApex { private set; get; } = 0.3f;
        [field: Tooltip("Jump fall gravity")]
        [field: SerializeField] public float JumpFall { private set; get; } = 1;
        [field: Tooltip("Jump cut fall gravity when we release jump button")]
        [field: SerializeField] public float JumpCutFall { private set; get; } = 1.25f;
        [field: Tooltip("The minimal jump cut range when we release button earlier system will wait until the time counted and will process cut jump" +
                        "Its based on JumpTimeToApex np. 1f -> 0.2 = when timer finish will process cut jump on 0.2f")]
        [field: SerializeField] [field: Range(0, 1)] public float MinJumpCutPercent { private set; get; } = 0;
        [field: Tooltip("Similar to the MinJumpCutPercent but its max. If we release button after the percent of the jump wil didn't force the jump cut")]
        [field: SerializeField] [field: Range(0, 1)] public float MaxJumpCutPercent { private set; get; } = 0.8f;
        [field: Tooltip("Its time to press button after we will fall that give us ability to more relative jump")]
        [field: SerializeField] public float JumpCoyoteTime { private set; get; } = 0.05f;
        [field: Tooltip("Its time to press button before we land on the ground if we are in the time when we hit ground will jump" +
                        "0.1f press before the ground will process jump when we land")]
        [field: SerializeField] public float JumpInputBuffer { private set; get; } = 0.2f;
        
        //TODO: maybe to delete
        [field: Tooltip("Velocity fall threshold its value before the velocity take high point. 0f if value is 2f then when velocity will be on -2f goes start falling")]
        [field: SerializeField] public float VelocityFallThreshold { private set; get; } = 0.5f;
        
        public float GravityJumpFall { private set; get; }
        public float GravityJumpCutFall { private set; get; }
        public float JumpForce { private set; get; }
        
        [field: Header("Dash")]
        [field: SerializeField] public float DashCooldown { private set; get; } = 0.2f;
        [field: SerializeField] public float DashSpeed { private set; get; } = 20;
        [field: SerializeField] public float DashDuration { private set; get; } = 0.2f;
        [field: SerializeField] public float DashInputBuffer { private set; get; } = 0.1f;
        [field: SerializeField] public float GravityDashFall { set; get; } = 0f;
        [field: SerializeField] public AnimationCurve DashCurve { set; get; } = AnimationCurve.Constant(0f, 1f, 1f);

        [field: Header("Wall slide")] 
        [field: SerializeField] public float WallFall { private set; get; } = 1f;
        
        void OnValidate() {
            // Movement
            // ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed
            MoveAccel = (50 * MoveAcceleration) / MoveMaxSpeed;
            MoveDecel = (50 * MoveDeceleration) / MoveMaxSpeed;
            MoveAcceleration = Mathf.Clamp(MoveAcceleration, 0.01f, MoveMaxSpeed);
            MoveDeceleration = Mathf.Clamp(MoveDeceleration, 0.01f, MoveMaxSpeed);
            
            // Gravity 
            GravityStrength = -(2 * JumpHeight) / (JumpTimeToApex * JumpTimeToApex);
            GravityScale = GravityStrength / Physics2D.gravity.y;
            GravityJumpFall = GravityScale * JumpFall;
            GravityJumpCutFall = GravityScale * JumpCutFall;
            
            // Jump
            // root 2 * H * g
            JumpForce = Mathf.Sqrt(2 * JumpHeight * Mathf.Abs(GravityStrength));
        }
    }
}