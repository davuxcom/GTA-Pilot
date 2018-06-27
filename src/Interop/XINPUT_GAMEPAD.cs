using System;

namespace GTAPilot.Interop
{
    [Flags]
    public enum XINPUT_GAMEPAD_BUTTONS : int
    {
        DPAD_UP = 0x0001,
        DPAD_DOWN = 0x0002,
        DPAD_LEFT = 0x0004,
        DPAD_RIGHT = 0x0008,
        START = 0x0010,
        BACK = 0x0020,
        LEFT_THUMB = 0x0040,
        RIGHT_THUMB = 0x0080,
        LEFT_SHOULDER = 0x0100,
        RIGHT_SHOULDER = 0x0200,
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000,
    };

    public enum XINPUT_GAMEPAD_AXIS : int
    {
        RIGHT_TRIGGER = 0,
        LEFT_TRIGGER = 1,
        RIGHT_THUMB_X = 2,
        RIGHT_THUMB_Y = 3,
        LEFT_THUMB_X = 4,
        LEFT_THUMB_Y = 5,
    };
}
