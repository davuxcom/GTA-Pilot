// This script is injected into the process space of XboxApp.exe using Frida.
// We hook XInputGetState and use the Frida IPC to transmit I/O to/from the main app.

var Kernel32 = {
    LoadLibrary: new NativeFunction(Module.findExportByName("kernel32.dll", "LoadLibraryW"), 'pointer', ['pointer'], 'win64'),
};

var XInputButtons = {
    DPAD_UP: 0x0001,
    DPAD_DOWN: 0x0002,
    DPAD_LEFT: 0x0004,
    DPAD_RIGHT: 0x0008,
    START: 0x0010,
    BACK: 0x0020,
    LEFT_THUMB: 0x0040,
    RIGHT_THUMB: 0x0080,
    LEFT_SHOULDER: 0x0100,
    RIGHT_SHOULDER: 0x0200,
    A: 0x1000,
    B: 0x2000,
    X: 0x4000,
    Y: 0x8000,
}

// C++ Struct helper
var Struct = function (structInfo) {
    this.Get = function () { return this.base_ptr; }
    this.base_ptr = null;
    this.get_base_ptr = function () { return this.base_ptr; }

    var TypeMap = {
        'pointer': [Process.pointerSize, Memory.readPointer, Memory.writePointer],
        'char': [1, Memory.readS8, Memory.writeS8], 'uchar': [1, Memory.readU8, Memory.writeU8],
        'int8': [1, Memory.readS8, Memory.writeS8], 'uint8': [1, Memory.readU8, Memory.writeU8],
        'int16': [2, Memory.readS16, Memory.writeS16], 'uint16': [2, Memory.readU16, Memory.writeU16],
        'int': [4, Memory.readS32, Memory.writeS32], 'uint': [4, Memory.readU32, Memory.writeU32],
        'int32': [4, Memory.readS32, Memory.writeS32], 'uint32': [4, Memory.readU32, Memory.writeU32],
        'long': [4, Memory.readS32, Memory.writeS32], 'ulong': [4, Memory.readU32, Memory.writeU32],
        'float': [4, Memory.readFloat, Memory.writeFloat], 'double': [8, Memory.readDouble, Memory.writeDouble],
        'int64': [8, Memory.readS64, Memory.writeS64], 'uint64': [8, Memory.readU64, Memory.writeU64],
    };

    function LookupType(stringType) {
        for (var type in TypeMap) { if (stringType == type) { return TypeMap[type]; } }
        throw Error("Didn't find " + JSON.stringify(stringType) + " in TypeMap");
    }

    var setter_result_cache = {};
    function CreateGetterSetter(self, name, type, offset) {
        Object.defineProperty(self, name, {
            get: function () { return LookupType(type)[1](this.get_base_ptr().add(offset)); },
            set: function (newValue) { setter_result_cache[name] = LookupType(type)[2](this.get_base_ptr().add(offset), newValue); }
        });
    };

    function SizeOfType(stringType) { return LookupType(stringType)[0]; }

    var base_ptr_size = 0;
    for (var member in structInfo) {
        var member_size = 0;
        if (member == "union") {
            var union = structInfo[member];
            for (var union_member in union) {
                var union_member_type = union[union_member];
                var union_member_size = SizeOfType(union_member_type);
                if (member_size < union_member_size) { member_size = union_member_size; }
                CreateGetterSetter(this, union_member, union_member_type, base_ptr_size);
            }
        } else {
            member_size = SizeOfType(structInfo[member]);
            CreateGetterSetter(this, member, structInfo[member], base_ptr_size);
        }
        base_ptr_size += member_size;
    }
    this.base_ptr = Memory.alloc(base_ptr_size);
    Object.defineProperty(this, "Size", { get: function () { return base_ptr_size; } });
}

var SavedState = { // Value is value from XInput controller.
    Buttons: 0,
    RIGHT_TRIGGER: 0,
    LEFT_TRIGGER: 0,
    RIGHT_THUMB_X: 0,
    RIGHT_THUMB_Y: 0,
    LEFT_THUMB_X: 0,
    LEFT_THUMB_Y: 0,

    Type: "InputData" // For sending to the host.
}

var NextState = { // Ticks to applly NextStateValue
    DPAD_UP: 0,
    DPAD_DOWN: 0,
    DPAD_LEFT: 0,
    DPAD_RIGHT: 0,
    START: 0,
    BACK: 0,
    LEFT_THUMB: 0,
    RIGHT_THUMB: 0,
    LEFT_SHOULDER: 0,
    RIGHT_SHOULDER: 0,
    A: 0,
    B: 0,
    X: 0,
    Y: 0,

    RIGHT_TRIGGER: 0,
    LEFT_TRIGGER: 0,
    RIGHT_THUMB_X: 0,
    RIGHT_THUMB_Y: 0,
    LEFT_THUMB_X: 0,
    LEFT_THUMB_Y: 0
}

var NextStateValue = { // Control value data
    DPAD_UP: 0,
    DPAD_DOWN: 0,
    DPAD_LEFT: 0,
    DPAD_RIGHT: 0,
    START: 0,
    BACK: 0,
    LEFT_THUMB: 0,
    RIGHT_THUMB: 0,
    LEFT_SHOULDER: 0,
    RIGHT_SHOULDER: 0,
    A: 0,
    B: 0,
    X: 0,
    Y: 0,

    RIGHT_TRIGGER: 0,
    LEFT_TRIGGER: 0,
    RIGHT_THUMB_X: 0,
    RIGHT_THUMB_Y: 0,
    LEFT_THUMB_X: 0,
    LEFT_THUMB_Y: 0
}

// Ticks to ignore the state of a button (de-bounce)
var ButtonTicksBackoff = {
    DPAD_UP: 0,
    DPAD_DOWN: 0,
    DPAD_LEFT: 0,
    DPAD_RIGHT: 0,
    START: 0,
    BACK: 0,
    LEFT_THUMB: 0,
    RIGHT_THUMB: 0,
    LEFT_SHOULDER: 0,
    RIGHT_SHOULDER: 0,
    A: 0,
    B: 0,
    X: 0,
    Y: 0,
}

// Inputs missing from DefaultTicks will have their ticks set to their value.
var DefaultTicks = {
    RIGHT_TRIGGER: 12,
    LEFT_TRIGGER: 12,
    RIGHT_THUMB_X: 5000000, // View
    RIGHT_THUMB_Y: 5000000, // View
    LEFT_THUMB_X: 12,
    LEFT_THUMB_Y: 12
}

Kernel32.LoadLibrary(Memory.allocUtf16String("XInputUAP.dll"));
var XInputGetStateAddress = Module.findExportByName("XInputUAP.dll", "XInputGetState");
var OriginalXInputGetState = new NativeFunction(XInputGetStateAddress, 'uint', ['uint', 'pointer'], 'win64');

var ticks = 0;
Interceptor.replace(XInputGetStateAddress, new NativeCallback(function (dwUserIndex, pState) {
    try {
        ticks++;
        var retValFromOriginal = OriginalXInputGetState(dwUserIndex, pState);
        var controllerStateData = new Struct({ // XINPUT_GAMEPAD
            'dwPacketNumber': 'uint',
            'Buttons': 'uint16', // wButtons
            'LEFT_TRIGGER': 'uchar', // bLeftTrigger
            'RIGHT_TRIGGER': 'uchar', // bRightTrigger
            'LEFT_THUMB_X': 'int16', // sThumbLX
            'LEFT_THUMB_Y': 'int16', // sThumbLY
            'RIGHT_THUMB_X': 'int16', // sThumbRX
            'RIGHT_THUMB_Y': 'int16', // sThumbRY
        })
        controllerStateData.base_ptr = pState;

        for (var input in SavedState) {
            SavedState[input] = controllerStateData[input];
        }

        for (var btn in ButtonTicksBackoff) {
            if (ButtonTicksBackoff[btn]) {
                ButtonTicksBackoff[btn]--;
            }
        }

        // We could control input feedback with some eventing, it can't
        // be left on all the time due to performance impact.
        //send(JSON.stringify(SavedState));

        for (var input in SavedState) {
            if (input == "Buttons") {
                for (var btn in XInputButtons) {
                    // Collect buttons and send events.
                    if (controllerStateData.Buttons & XInputButtons[btn]) {
                        if (ButtonTicksBackoff[btn] == 0) {
                            ButtonTicksBackoff[btn] = 20;
                            OnButtonPressed(XInputButtons[btn]);
                        }
                    }
                }

                // Disable back/view.
                controllerStateData.Buttons &= ~XInputButtons.BACK;

                for (var btn in XInputButtons) {
                    // Replace buttons
                    if (NextState[btn]) {
                        controllerStateData.Buttons |= XInputButtons[btn]
                        NextState[btn]--;
                    }
                }
            } else {
                // Replace axis'
                if (NextState[input]) {
                    controllerStateData[input] = NextStateValue[input];
                    NextState[input]--;
                }
            }
        }
    } catch (e) {
        console.log("Failure in XInputGetState: " + e);
    }
    return retValFromOriginal;
}, 'uint', ['uint', 'pointer']));

function OnButtonPressed(btn) {
    send(JSON.stringify({
        Type: "ButtonPress",
        Value: btn
    }));
}

// NOTE/BUG: When using frida node.js bindings, recv takes a string, using C# bindings it's an object
function recv_one_message(packet) {
    try {
        if (packet.PING) {
            send(packet);
        }
        else {
            for (var input in NextState) {
                if (packet[input]) {
                    NextState[input] = DefaultTicks[input] ? DefaultTicks[input] : parseInt(packet[input], 10);
                    NextStateValue[input] = parseInt(packet[input], 10);
                }
            }
        }
    } catch (e) {
        console.log("recv_proc Error: " + e + " in " + JSON.stringify(packet));
    }
    recv(recv_one_message);
}

function printInputFpsEachMinute() {
    setTimeout(function () {
        var fps = (ticks / 60);
        ticks = 0;

        send(JSON.stringify({
            Type: "XInputFPS",
            Value: parseInt(fps, 10)
        }));

        printInputFpsEachMinute();
    }, 60 * 1000);
}

recv(recv_one_message);
printInputFpsEachMinute();
console.log("Ready.");