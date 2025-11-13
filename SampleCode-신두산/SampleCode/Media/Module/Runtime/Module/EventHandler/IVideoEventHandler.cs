namespace  UCF.Media.Module
{
	public interface IVideoEventHandler
    {
        void OnMediaPlayerEvent(EventType et, ErrorCode errorCode);
    }
}