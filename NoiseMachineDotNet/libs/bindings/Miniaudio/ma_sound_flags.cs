namespace NoiseMachineDotNet.libs.bindings.Miniaudio
{
    public enum ma_sound_flags
    {
        MA_SOUND_FLAG_STREAM = 0x00000001,
        MA_SOUND_FLAG_DECODE = 0x00000002,
        MA_SOUND_FLAG_ASYNC = 0x00000004,
        MA_SOUND_FLAG_WAIT_INIT = 0x00000008,
        MA_SOUND_FLAG_UNKNOWN_LENGTH = 0x00000010,
        MA_SOUND_FLAG_NO_DEFAULT_ATTACHMENT = 0x00001000,
        MA_SOUND_FLAG_NO_PITCH = 0x00002000,
        MA_SOUND_FLAG_NO_SPATIALIZATION = 0x00004000,
    }
}