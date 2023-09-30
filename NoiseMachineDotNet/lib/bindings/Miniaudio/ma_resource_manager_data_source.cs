using System.Runtime.InteropServices;

namespace MiniAudioSharp;

public partial struct ma_resource_manager_data_source
{
    [NativeTypeName("__AnonymousRecord_miniaudio_L10404_C5")]
    public _backend_e__Union backend;

    [NativeTypeName("ma_uint32")]
    public uint flags;

    [NativeTypeName("ma_uint32")]
    public uint executionCounter;

    [NativeTypeName("ma_uint32")]
    public uint executionPointer;

    [StructLayout(LayoutKind.Explicit)]
    public partial struct _backend_e__Union
    {
        [FieldOffset(0)]
        public ma_resource_manager_data_buffer buffer;

        [FieldOffset(0)]
        public ma_resource_manager_data_stream stream;
    }
}
