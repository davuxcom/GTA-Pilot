// @import windows/platform

// request a pipe for cross-process input
//send(JSON.stringify({pipe: "xbox"}));

//require('resume.js').invoke();

left_thumb_set = 0;
right_thumb_set = 0;

start_set = 0;
b_set = 0;
a_set = 0;

rt_set = 0;
rt_value = 0;

lt_set = 0;
lt_value = 0;

thumblx_set = 0;
thumblx_value = 0;

thumbly_set = 0;
thumbly_value = 0;

thumbry_set = 0;
thumbry_value = 0;

thumbrx_set = 0;
thumbrx_value = 0;

shoulder_left_set = 0;
shoulder_right_set = 0;

function ResetAll()
{
    left_thumb_set = 0;
    right_thumb_set = 0;

    start_set = 0;
    b_set = 0;
    a_set = 0;

    rt_set = 0;
    rt_value = 0;

    lt_set = 0;
    lt_value = 0;

    thumblx_set = 0;
    thumblx_value = 0;

    thumbly_set = 0;
    thumbly_value = 0;

    thumbry_set = 0;
    thumbry_value = 0;

    thumbrx_set = 0;
    thumbrx_value = 0;

    shoulder_left_set = 0;
    shoulder_right_set = 0;
}

// NOTE: When using frida node.js bindings, recv takes a string, on C# it's an object
function recv_proc(packetStr) {
	try
	{

        var packet = packetStr; // JSON.parse(packetStr);
	
   // console.log("RCV " + JSON.stringify(packet));
	//console.log("RCV: " + packetStr);

    if (packet.thumblx)
    {
        thumblx_set = parseInt(packet.thumblx_ticks);
        thumblx_value = parseInt(packet.thumblx);
    }
    
    if (packet.thumbly)
    {
        thumbly_set = parseInt(packet.thumbly_ticks);
        thumbly_value = parseInt(packet.thumbly);
    }

    if (packet.thumbry)
    {
        thumbry_set = parseInt(packet.thumbry_ticks);
        thumbry_value = parseInt(packet.thumbry);
    }
    
    if (packet.thumbrx)
    {
        thumbrx_set = parseInt(packet.thumbrx_ticks);
        thumbrx_value = parseInt(packet.thumbrx);
    }

    if (packet.rtrigger)
    {
        rt_set = parseInt(packet.rt_ticks);
        rt_value = parseInt(packet.rtrigger);
    }
    
    if (packet.ltrigger)
    {
        lt_set = parseInt(packet.lt_ticks);
        lt_value = parseInt(packet.ltrigger);
    }
    
    if (packet.rbumper)
    {
        shoulder_right_set = parseInt(packet.rbumper_ticks);
     //   shoulder_right_value = packet.rbumper;
    }
    
    if (packet.lbumper)
    {
        shoulder_left_set = parseInt(packet.lbumper_ticks);
      // shoulder_right_value = packet.lbumper;
    }
    
    if (packet.lthumb)
    {
        left_thumb_set = 20;
    }
    
    if (packet.rthumb)
    {
        right_thumb_set = 20;
    }
    
    if (packet.start)
    {
        start_set = 8;
    }
    
    if (packet.b)
    {
        b_set = 8;
    }
    
    if (packet.a)
    {
        a_set = 8;
    }
	}catch(e)
	{
		console.log("RCV-ERR: " + e);
			console.log("data: " + packetStr);
	}
	recv(recv_proc);
}

var ticks = 0;

function timeout() {
    setTimeout(function () {
        
        var t = ticks / 60;
        ticks = 0;
        
        console.log("Input calls per second: " + t);
        
        
        timeout();
    }, 60 * 1000);
}

timeout();

//console.log("READY");
console.log("ready");

function GetAbi() { return 'win64'; }

var Kernel32 = {
    LoadLibrary: new NativeFunction(Module.findExportByName("kernel32.dll", "LoadLibraryW"), 'pointer', ['pointer'], GetAbi()),
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

var Struct = function (structInfo) {
    this.Get = function () { return this.base_ptr; }
    this.base_ptr = null;
    this.get_base_ptr = function() { return this.base_ptr; }
    
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
            var member_size = SizeOfType(structInfo[member]);
            CreateGetterSetter(this, member, structInfo[member], base_ptr_size);
        }
        base_ptr_size += member_size;
    }
    this.base_ptr = Memory.alloc(base_ptr_size);
    Object.defineProperty(this, "Size", { get: function () { return base_ptr_size; } });
}


console.log("XInputUAP.dll: " + Kernel32.LoadLibrary(Memory.allocUtf16String("XInputUAP.dll")));

var XInputGetStateAddress = Module.findExportByName("XInputUAP.dll", "XInputGetState");
console.log("XInputGetStateAddress: " + XInputGetStateAddress);

var OriginalXInputGetState = new NativeFunction(XInputGetStateAddress, 'uint', ['uint', 'pointer'], GetAbi());


var t = 0;
Interceptor.replace(XInputGetStateAddress, new NativeCallback(function (dwUserIndex, pState) {
    var ret = OriginalXInputGetState(dwUserIndex, pState);
	try
	{
    var state = new Struct({
        'dwPacketNumber': 'uint',
        'wButtons': 'uint16',
        'bLeftTrigger': 'uchar',
        'bRightTrigger': 'uchar',
        'sThumbLX': 'int16',
        'sThumbLY': 'int16',
        'sThumbRX': 'int16',
        'sThumbRY': 'int16',
    })
    state.base_ptr = pState;
    
    if (state.wButtons & XInputButtons.BACK)
    {
        ResetAll();
    }

    if (thumblx_set > 0)
    {
        thumblx_set--;
        state.sThumbLX = thumblx_value;
    }
    
    if (thumbly_set > 0)
    {
        thumbly_set--;
        state.sThumbLY = thumbly_value;
    }
    
    if (thumbrx_set > 0)
    {
        thumbrx_set--;
        state.sThumbRX = thumbrx_value;
    }
    
    if (thumbry_set > 0)
    {
        thumbry_set--;
        state.sThumbRY = thumbry_value;
    }
    
    if (start_set > 0)
    {
        start_set--;
        state.wButtons |= XInputButtons.START;
    }
    else
    {
        if (rt_set > 0)
        {
            rt_set--;
            state.bRightTrigger = rt_value;
        }

        if (lt_set > 0)
        {
            lt_set--;
            state.bLeftTrigger = lt_value;
        }

        if (shoulder_right_set > 0)
        {
            shoulder_right_set--;
            state.wButtons |= XInputButtons.RIGHT_SHOULDER;
        }
            
        if (shoulder_left_set > 0)
        {
            shoulder_left_set--;
            state.wButtons |= XInputButtons.LEFT_SHOULDER;
        }  
        
        if (right_thumb_set > 0)
        {
            right_thumb_set--;
            state.wButtons |= XInputButtons.RIGHT_THUMB;
        }
        
        if (left_thumb_set > 0)
        {
            left_thumb_set--;
            state.wButtons |= XInputButtons.LEFT_THUMB;
        }
        
        if (b_set > 0)
        {
            b_set--;
            state.wButtons |= XInputButtons.B;
        }
        
        if (a_set > 0)
        {
            a_set--;
            state.wButtons |= XInputButtons.A;
        }
    }

    ticks++;
    
    if (ticks % 100 == 0)
    {
                
        //console.log("State: " + state.sThumbRX + " " + state.sThumbRY);
        
    }
    } catch(e)
	{
		console.log("BLOW: " + e);
	}
    return ret;
}, 'uint', ['uint', 'pointer']));


recv(recv_proc); // Start listening
