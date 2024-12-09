using System;
using System.Collections.Generic;
using System.Linq;
using CoreUtility.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CharControl2D {
    public struct InputData {
        public Vector2 Direction;
        public bool IsJumpHolding;
        // Add value to data
        public float JumpReleaseTime;
    }

    [Serializable]
    public struct InputSheet {
        public string MapInputSheet;
        public string MoveInputSheet;
        public string JumpInputSheet;
        public string DashInputSheet;

        internal static InputSheet Default() {
            return new InputSheet {
                MapInputSheet = "Player",
                MoveInputSheet = "Move",
                JumpInputSheet = "Jump",
                DashInputSheet = "Dash"
            };
        }
    }
    
    internal static class CharInput{
#if ENABLE_INPUT_SYSTEM
        internal static void RegistryControllerListeners(CharController controller2D, InputActionAsset inputAsset, InputSheet inputSheet) {
            //TODO: change on inflowis
            var sheet = inputSheet;
            var asset = inputAsset;
            
            var map = asset.GetOrAdd(sheet.MapInputSheet);
            var move = map.GetOrAdd(sheet.MoveInputSheet);
            var jump = map.GetOrAdd(sheet.JumpInputSheet);
            var dash = map.GetOrAdd(sheet.DashInputSheet);

            move.performed += (context) => controller2D.UpdateInput(direction: context.ReadValue<Vector2>());
            move.canceled += (context) => controller2D.UpdateInput(direction : context.ReadValue<Vector2>());
            
            jump.performed += context => {
                controller2D.UpdateInput(isJumpHolding: context.performed);
                controller2D.RequestJump();
            };
            jump.canceled += context => {
                controller2D.UpdateInput(isJumpHolding: context.performed);
                controller2D.RequestCutJump();
            };;
            
            dash.performed += _ => controller2D.RequestDash();
            
            move.Enable();
            jump.Enable();
            dash.Enable();
        }
        
        internal static void ConfigureInputAsset(InputActionAsset inputAsset, InputSheet inputSheet, InputDevice inputDevice) {
            var asset = inputAsset;
            if (!inputAsset) 
                throw new Exception("Put reference input asset into char controller");
            
            var sheet = inputSheet;
            var device = inputDevice;
            var map = asset.GetOrAdd(sheet.MapInputSheet);
            // TODO: Refactor this
            var keyboardSchemeName = InputDevice.Keyboard.ToString(); 
            var xboxSchemeName = InputDevice.XboxController.ToString().SpaceBefore("Controller"); 
            var playStationSchemeName = InputDevice.PlayStationController.ToString().SpaceBefore("Controller"); 
            
            var inputScheme = new Dictionary<InputDevice, (string, string, Action)> { 
                    { InputDevice.Keyboard, (
                        keyboardSchemeName, 
                        "<Keyboard>",
                        () => KeyBoardSetup(map, sheet, keyboardSchemeName)) },
                    { InputDevice.XboxController, (
                        xboxSchemeName,
                        "<XInputController>",
                        () => XboxSetup(map, sheet, xboxSchemeName)) },
                    { InputDevice.PlayStationController, (
                        playStationSchemeName,
                        "<DualShockGamepad>",
                        () => PSSetup(map, sheet, playStationSchemeName)) },
            };

            inputScheme.
                Where((dict) => device.HasFlag(dict.Key)).
                ForEach((d) => asset.TryAddScheme(d.Value.Item1, d.Value.Item2));

            // create actions by selected
            inputScheme.
                Where(setupAction => device.HasFlag(setupAction.Key)).
                Select((actionsWithFlag) => actionsWithFlag.Value.Item3).
                ForEach((devices) => devices?.Invoke());
            
            //TODO: Remove from input asset binds on change InputDevice
            // -> Only Keyboard remove others np. xbox and ps
        }

        static void XboxSetup(InputActionMap map, InputSheet sheet, string schemeName) {
            map.GetOrAdd(sheet.MoveInputSheet).IsGroups(schemeName)?.
                AddBinding("<Gamepad>/leftStick", groups: schemeName);
            map.GetOrAdd(sheet.JumpInputSheet, type: InputActionType.Button).IsGroups(schemeName)?.
                AddBinding("<Gamepad>/buttonSouth", groups: schemeName);
            map.GetOrAdd(sheet.DashInputSheet, type: InputActionType.Button).IsGroups(schemeName)?.
                AddBinding("<Gamepad>/buttonEast", groups: schemeName);
        }
        
        static void KeyBoardSetup(InputActionMap map, InputSheet sheet, string schemeName) {
            map.GetOrAdd(sheet.MoveInputSheet).IsGroups(schemeName)?.
                AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", groups: schemeName)
                .With("Down", "<Keyboard>/s", groups: schemeName)
                .With("Left", "<Keyboard>/a", groups: schemeName)
                .With("Right", "<Keyboard>/d", groups: schemeName);
            
            map.GetOrAdd(sheet.JumpInputSheet, type: InputActionType.Button)?.
                IsGroups(schemeName)?.
                AddBinding("<Keyboard>/space", groups: schemeName);
            map.GetOrAdd(sheet.DashInputSheet, type: InputActionType.Button)?.
                IsGroups(schemeName)?.
                AddBinding("<Keyboard>/leftShift", groups: schemeName);
        }
        
        static void PSSetup(InputActionMap map, InputSheet sheet, string schemeName) {
            map.GetOrAdd(sheet.MoveInputSheet).IsGroups(schemeName)?.
                AddBinding("<Gamepad>/leftStick", groups: schemeName);
            map.GetOrAdd(sheet.JumpInputSheet, type: InputActionType.Button).IsGroups(schemeName)?.
                AddBinding("<Gamepad>/buttonSouth", groups: schemeName);
            map.GetOrAdd(sheet.DashInputSheet, type: InputActionType.Button).IsGroups(schemeName)?.
                AddBinding("<Gamepad>/buttonEast", groups: schemeName);
        }
#endif
    }
}