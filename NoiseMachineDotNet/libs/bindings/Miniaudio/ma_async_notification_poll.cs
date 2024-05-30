namespace NoiseMachineDotNet.libs.bindings.Miniaudio
{
    public partial struct ma_async_notification_poll
    {
        public ma_async_notification_callbacks cb;

        [NativeTypeName("ma_bool32")]
        public uint signalled;
    }
}
