namespace NoiseMachineDotNet.libs.bindings.Miniaudio
{
    public partial struct ma_atomic_vec3f
    {
        public ma_vec3f v;

        [NativeTypeName("ma_spinlock")]
        public uint @lock;
    }
}
