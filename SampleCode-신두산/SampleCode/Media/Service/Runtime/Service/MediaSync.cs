using System;
using  UCF.Core.Helper.Telepresence;

namespace  UCF.Media.Service
{
    using Telepresence;

    internal class MediaSync
    {
        private LandMediaData mediaData;
        private Action<Enum> onChangePresentation;

        /// <summary>
        /// 서버 연결이 끊어졌을 때
        /// </summary>
        public void OnDisconnectedServer()
        {
            this.onChangePresentation?.Invoke(Core.Bridge.UCFError.Module.Common.TELEPRESENCE_SERVER_DISCONNECTED);
            this.onChangePresentation = null;
        }

        /// <summary>
        /// 발표자료 변경 요청
        /// </summary>
        /// <param name="mediaData"></param>
        /// <param name="type"></param>
        /// <param name="url"></param>
        /// <param name="onCallback"></param>
        public void RequestChangePresentation(LandMediaData mediaData, CommonEnum.CommonConferenceURLFormat type, string url, Action<Enum> onCallback)
        {
            this.mediaData = mediaData;
            this.onChangePresentation = onCallback;

            PresentationSyncProvider.ChangePresentationFile(new ConferenceUrlChangeReq(type,url), OnResponseChangePresentation, OnFailedChangePresentation);
        }

        /// <summary>
        /// 발표 자료 변경 요청에 대한 응답
        /// </summary>
        /// <param name="ack"></param>
        private void OnResponseChangePresentation(ConferenceUrlChangeAck ack)
        {
            this.mediaData.Clear();
            mediaData.presentationType = ack.urlFormat;
            mediaData.url = ack.url;

            this.onChangePresentation?.Invoke(null);
            this.onChangePresentation = null;
        }

        /// <summary>
        /// 발표 자료 변경 요청에 대한 실패
        /// </summary>
        /// <param name="result"></param>
        private void OnFailedChangePresentation(Enum result)
        {
            this.onChangePresentation?.Invoke(result);
            this.onChangePresentation = null;
        }

        /// <summary>
        /// 현재 미디어 상태 정보 요청
        /// </summary>
        /// <param name="mediaData"></param>
        /// <param name="onCallback"></param>
        public void RequestCurrentMediaState(LandMediaData mediaData, Action<Enum> onCallback)
        {
            PresentationSyncProvider.CurrentMediaState(new ConferenceCurrentMediaStateReq(mediaData.url), OnResponse, OnFailed);

            void OnResponse(ConferenceCurrentMediaStateAck ack)
            {
                mediaData.url = ack.mediaUrl;
                mediaData.state = ack.status;
                mediaData.position = ack.seek;

                if (onCallback != null)
                    onCallback.Invoke(null);
            }

            void OnFailed(Enum result)
            {
                if (onCallback != null)
                    onCallback.Invoke(result);
            }
        }

        /// <summary>
        /// Media 동기화 요청
        /// </summary>
        /// <param name="mediaData"></param>
        /// <param name="state"></param>
        /// <param name="position"></param>
        /// <param name="onCallback"></param>
        public void UpdateMediaSync(LandMediaData mediaData, CommonEnum.CommonConferenceMediaStatus state, int position, Action<Enum> onCallback)
        {
            PresentationSyncProvider.SyncPresentationMedia(new ConferenceMediaAccessReq(mediaData.url,state,position), OnResponse, OnFailed);

            void OnResponse(ConferenceMediaAccessAck ack)
            {
                mediaData.url = ack.mediaUrl;
                mediaData.state = ack.status;
                mediaData.position = ack.seek;

                if (onCallback != null)
                    onCallback.Invoke(null);
            }

            void OnFailed(Enum result)
            {
                if (onCallback != null)
                    onCallback.Invoke(result);
            }
        }

        /// <summary>
        /// PDF 동기화 요청
        /// </summary>
        /// <param name="mediaData"></param>
        /// <param name="position"></param>
        /// <param name="onCallback"></param>
        public void UpdatePDFSync(LandMediaData mediaData, int position, Action<Enum> onCallback)
        {
            // PDF 동기화 요청.
            PresentationSyncProvider.SyncPresentationPdf(new ConferencePaperAccessReq(mediaData.url,position), OnResponse, OnFailed);

            void OnResponse(ConferencePaperAccessAck ack)
            {
                mediaData.url = ack.paperUrl;
                mediaData.position = ack.page;

                if (onCallback != null)
                    onCallback.Invoke(null);
            }

            void OnFailed(Enum result)
            {
                if (onCallback != null)
                    onCallback.Invoke(result);
            }
        }

        /// <summary>
        /// MRLis 동기화 요청
        /// </summary>
        /// <param name="mediaData"></param>
        /// <param name="position"></param>
        /// <param name="onCallback"></param>
        public void UpdateMRLisSync(LandMediaData mediaData, CommonEnum.CommonConferenceMediaStatus state, Action<Enum> onCallback)
        {
            // PDF 동기화 요청.
            PresentationSyncProvider.SyncPresentationMrlisMedia(new ConferenceMrlisMediaAccessReq(state), OnResponse, OnFailed);

            void OnResponse(ConferenceMrlisMediaAccessAck ack)
            {
                mediaData.state = ack.status;

                if (onCallback != null)
                    onCallback.Invoke(null);
            }

            void OnFailed(Enum result)
            {
                if (onCallback != null)
                    onCallback.Invoke(result);
            }
        }

        /// <summary>
        /// Media Mute/Unmute 요청
        /// </summary>
        /// <param name="mediaData"></param>
        /// <param name="isMute"></param>
        /// <param name="onCallback"></param>
        public void UpdateMediaMute(LandMediaData mediaData, byte isMute, Action<Enum> onCallback)
        {
            // Media mute 요청.
            PresentationSyncProvider.ChangePresentationMute(new ConferenceMuteReq(isMute), OnResponse, OnFailed);

            void OnResponse(ConferenceMuteAck ack)
            {
                mediaData.isMediaMute = ack.isMute;

                if (onCallback != null)
                    onCallback.Invoke(null);
            }

            void OnFailed(Enum result)
            {
                if (onCallback != null)
                    onCallback.Invoke(result);
            }
        }
    }
}