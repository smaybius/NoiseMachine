namespace NoiseMachineDotNet.libs.bindings.Miniaudio
{
    public enum ma_node_flags
    {
        MA_NODE_FLAG_PASSTHROUGH = 0x00000001,
        MA_NODE_FLAG_CONTINUOUS_PROCESSING = 0x00000002,
        MA_NODE_FLAG_ALLOW_NULL_INPUT = 0x00000004,
        MA_NODE_FLAG_DIFFERENT_PROCESSING_RATES = 0x00000008,
        MA_NODE_FLAG_SILENT_OUTPUT = 0x00000010,
    }
}
