namespace NoiseMachineDotNet.libs.bindings.Miniaudio
{
    public unsafe partial struct ma_data_source_node
    {
        public ma_node_base @base;

        [NativeTypeName("ma_data_source *")]
        public void* pDataSource;
    }
}
