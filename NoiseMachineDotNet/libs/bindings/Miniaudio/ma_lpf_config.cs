namespace MiniAudioSharp;

public partial struct ma_lpf_config
{
    public ma_format format;

    [NativeTypeName("ma_uint32")]
    public uint channels;

    [NativeTypeName("ma_uint32")]
    public uint sampleRate;

    public double cutoffFrequency;

    [NativeTypeName("ma_uint32")]
    public uint order;
}
