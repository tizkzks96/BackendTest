using  UCF.Core.Helper.Telepresence;

namespace  UCF.Media.Service
{
    public interface IMediaEventHandler
    {
        /// <summary>
        /// 발표 자료 변경 이벤트
        /// </summary>
        /// <param name="type"></param>
        void OnChangedMedia(CommonEnum.CommonConferenceURLFormat type);

        /// <summary>
        /// 미디어 재생 위치 이벤트
        /// </summary>
        /// <param name="current"></param>
        /// <param name="length"></param>
        void OnProgressMedia(double current, double length);

        /// <summary>
        /// 미디어 재생 종료 이벤트
        /// </summary>
        /// <param name="time"></param>
        void OnFinishedMedia(double time);

        /// <summary>
        /// AVPro 플레이어 이벤트
        /// </summary>
        /// <param name="data"></param>
        void OnVideoEvent(object data);
    }
}