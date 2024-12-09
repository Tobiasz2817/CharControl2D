using UnityEngine;
using UnityEngine.InputSystem;

namespace CharControl2D.Archive {
    [CreateAssetMenu(menuName = "Content/Config/CharControl", fileName = "CharConfig")]
    public class CharConfig : ScriptableObject {
        [Header("Custom")]
        public int CollisionCount = 10;
        
        [Header("Rules")]
        public CharRules Rules;

#if ENABLE_INPUT_SYSTEM
        [Header("New Input System")]
        public InputSheet InputSheet = InputSheet.Default();
        public InputDevice InputDevice = InputDevice.Keyboard | InputDevice.XboxController | InputDevice.PlayStationController;
        public InputActionAsset InputAsset;
#endif

        void OnValidate() {
#if ENABLE_INPUT_SYSTEM
            //if(InputAsset) CharInput.ConfigureInputAsset(this);
#endif
        }
    }
}