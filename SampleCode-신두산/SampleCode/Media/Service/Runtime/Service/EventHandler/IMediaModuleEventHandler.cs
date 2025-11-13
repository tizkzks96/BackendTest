namespace  UCF.Media.Service
{
    internal interface IMediaModuleEventHandler
    {
        /// <summary>
        /// 미디어 진행률 이벤트
        /// </summary>
        /// <param name="current"></param>
        /// <param name="length"></param>
        void OnMediaProgress(BaseProvider provider, double current, double length);

        /// <summary>
        /// 재생 종료 이벤트
        /// </summary>
        void OnPlayerFinished(BaseProvider provider, double time);

        /// <summary>
        /// AVPro 플레이어 이벤트
        /// </summary>
        void OnVideoPlayerEvent(BaseProvider provider, object data);
    }
}