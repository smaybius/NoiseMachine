namespace NoiseMachineDotNet.libs.bindings.Miniaudio
{
    public partial struct ma_panner
    {
        public ma_format format;

        [NativeTypeName("ma_uint32")]
        public uint channels;

        public ma_pan_mode mode;

        public float pan;
    }
}
