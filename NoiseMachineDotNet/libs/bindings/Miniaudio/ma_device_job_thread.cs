namespace NoiseMachineDotNet.libs.bindings.Miniaudio
{
    public unsafe partial struct ma_device_job_thread
    {
        [NativeTypeName("ma_thread")]
        public void* thread;

        public ma_job_queue jobQueue;

        [NativeTypeName("ma_bool32")]
        public uint _hasThread;
    }
}
