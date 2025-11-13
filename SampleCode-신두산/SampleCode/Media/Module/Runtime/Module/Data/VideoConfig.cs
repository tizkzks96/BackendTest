namespace  UCF.Media.Module
{
    using Core.Bridge;

    public static class VideoConfig
    {
        public static int VideoCacheCount { get; private set; }
        public static bool UseVideoOptimizeOption { get; private set; }
        public static StorageInfo StorageInfo { get; private set; }

        public static void UpdateConfig(int videoCacheCount, bool useVideoOptimizeOption, StorageInfo info)
        {
            if (videoCacheCount == 0)
            {
                videoCacheCount = 20;
            }

            VideoCacheCount = videoCacheCount;
            UseVideoOptimizeOption = useVideoOptimizeOption;
            StorageInfo = info;
        }
    }
}