using System;

namespace CharControl2D {
    [Flags]
    public enum InputDevice {
        Keyboard = 1 << 0,
        XboxController = 1 << 1,
        PlayStationController = 1 << 2
    }
}