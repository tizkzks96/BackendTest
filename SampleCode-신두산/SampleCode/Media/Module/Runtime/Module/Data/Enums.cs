namespace  UCF.Media.Module
{
    // AVPro enum 재정의.
    public enum EventType
    {
        MetaDataReady,      // Triggered when meta data(width, duration etc) is available
        ReadyToPlay,        // Triggered when the video is loaded and ready to play
        Started,            // Triggered when the playback starts
        FirstFrameReady,    // Triggered when the first frame has been rendered
        FinishedPlaying,    // Triggered when a non-looping video has finished playing
        Closing,            // Triggered when the media is closed
        Error,              // Triggered when an error occurs
        SubtitleChange,     // Triggered when the subtitles change
        Stalled,            // Triggered when media is stalled (eg. when lost connection to media stream)
        Unstalled,          // Triggered when media is resumed form a stalled state (eg. when lost connection is re-established)
        ResolutionChanged,  // Triggered when the resolution of the video has changed (including the load) Useful for adaptive streams
        StartedSeeking,     // Triggered when seeking begins
        FinishedSeeking,    // Triggered when seeking has finished
        StartedBuffering,   // Triggered when buffering begins
        FinishedBuffering,  // Triggered when buffering has finished
        PropertiesChanged,  // Triggered when any properties (eg stereo packing are changed) - this has to be triggered manually
        PlaylistItemChanged,// Triggered when the new item is played in the playlist
        PlaylistFinished,   // Triggered when the playlist reaches the end

        TextTracksChanged,  // Triggered when the text tracks are added or removed
        Paused,             // Triggered when the player is paused
        Unpaused,           // Triggered when the player resumes playing

        // TODO: 
        //StartLoop,		// Triggered when the video starts and is in loop mode
        //EndLoop,			// Triggered when the video ends and is in loop mode
        //NewFrame			// Trigger when a new video frame is available

        TextCueChanged = SubtitleChange,    // Triggered when the text to display changes
    }

    // AVPro enum 재정의.
    public enum ErrorCode
    {
        None = 0,
        LoadFailed = 100,
        DecodeFailed = 200,
    }

    // AVPro enum 재정의.
    public enum TransparencyMode
    {
        Opaque,
        Transparent,
    }

    // AVPro enum 재정의.
    public enum AlphaPacking
    {
        None,
        TopBottom,
        LeftRight,
    }

    // AVPro enum 재정의.
    public enum StereoPacking : int
    {
        None = 0,                   // Monoscopic
        TopBottom = 1,              // Top is the left eye, bottom is the right eye
        LeftRight = 2,              // Left is the left eye, right is the right eye
        CustomUV = 3,               // Use the mesh UV to unpack, uv0=left eye, uv1=right eye
        TwoTextures = 4,            // First texture left eye, second texture is right eye
        Unknown = 10,
    }
}
