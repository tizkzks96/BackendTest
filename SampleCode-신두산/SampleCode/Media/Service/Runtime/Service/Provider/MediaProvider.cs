using System;
using  UCF.Core.Helper.Telepresence;
using UnityEngine;

namespace  UCF.Media.Service
{
    using Core.Bridge;
    using Module;

    internal partial class MediaProvider : IMediaModuleEventHandler, IDisposable
    {
        private const int MILLISECOND = 1000;

        internal MediaSync mediaSync;
        internal BaseProvider provider;

        private IMediaEventHandler eventHandler;

        internal ilUserDataContainer.ilUserDataTable userTable;
        internal LandMediaData landData;

        public MediaProvider()
        {
            mediaSync = new MediaSync();
        }

        public void Dispose()
        {
            DestroyProvider();
        }

        public void OnDisconnectedServer()
        {
            mediaSync.OnDisconnectedServer();
        }

        public void UpdateConfig(GlobalSettingData.UcfConfigData config, StorageInfo storageInfo)
        {
            VideoConfig.UpdateConfig(config.videoCacheCount, config.useVideoOptimizeOption, storageInfo);
        }

        public void SetDataContainers(ilUserDataContainer.ilUserDataTable table, LandMediaData data)
        {
            userTable = table;
            landData = data;
        }

        private void DestroyProvider(bool refreshRender = false)
        {
            if (provider)
            {
                provider.Dispose();
                GameObject.Destroy(provider.gameObject);
            }

            if (refreshRender)
            {
                //if (mediaRenderer != null)
                //    mediaRenderer.RefreshRenderer();
            }

            provider = null;
        }

        private void EnsureMediaProvider(MediaUser userInfo, LandMediaData data, Action<Enum> onResponse)
        {
            if (provider)
            {
                if (BaseProvider.GetProviderType(data.presentationType) != provider.GetType())
                {
                    // 생성되어있는 타입과 다르면 삭제.
                    DestroyProvider(true);
                }
                else if (provider.CheckMediaData(data) == false)
                {
                    // 기존에 설정했던 데이터와 다르면 삭제
                    DestroyProvider(true);
                }
            }

            if (!provider)
            {
                // Provider 생성.
                provider = BaseProvider.CreateProvider(data.presentationType);
                provider.Initialize(this);
                provider.Load(data.url, onResponse);
                provider.UpdateState(userInfo, data);

                // 발표자료 변경 이벤트 발생.
                eventHandler?.OnChangedMedia(data.presentationType);
            }
            else
            {
                // BG/통화/Interrupt에 의한 정지가 발생했는지에 대한 여부
                if (CheckPolicy(data))
                {
                    ResumeMediaByPolicy(userInfo, data, onResponse);
                }
                else
                {
                    provider.WaitLoadMedia(userInfo, data, (result) =>
                    {
                        if (result == null)
                        {
                            provider.UpdateState(userInfo, data);
                        }
                        onResponse?.Invoke(result);
                    });
                }
            }
        }

        private void ReleaseMediaProvider(Action<Enum> onResponse)
        {
            // 발표자료 변경 이벤트 발생.
            eventHandler?.OnChangedMedia((CommonEnum.CommonConferenceURLFormat)(-1));

            DestroyProvider(true);
            onResponse?.Invoke(null);
        }

        public void UpdateState(MediaUser userInfo, LandMediaData data)
        {
            if (provider)
            {
                provider.UpdateState(userInfo, data);
            }
        }

        public void RegisterMediaEventHander(IMediaEventHandler handler)
        {
            eventHandler = handler;
        }

        public void SyncMediaState(MediaUser userInfo, LandMediaData mediaData, Action<Enum> onResponse)
        {
            switch (mediaData.presentationType)
            {
                default:
                    EnsureMediaProvider(userInfo, mediaData, onResponse);
                    break;

                case (CommonEnum.CommonConferenceURLFormat)(-1): // NONE
                    ReleaseMediaProvider(onResponse);
                    break;
            }
        }

        public void SyncMuteState(MediaUser userInfo, LandMediaData mediaData)
        {
            if (provider)
            {
                provider.UpdateMuteState(userInfo, mediaData);
            }
        }

        public bool IsPlayingAudio()
        {
            if (provider)
            {
                return provider.IsPlayingAudio();
            }
            return false;
        }

        #region Control
        public void ChangeMedia(MediaUser userInfo, LandMediaData mediaData, CommonEnum.CommonConferenceURLFormat type, string url, Action<Enum> onRegister, Action<Enum> onInitialize)
        {
            // MRLis 타입의 발표 자료의 경우,
            // Native에서 등록 호출 시 url값을 고정된 값으로 전달해줌.(MRLis, MRLis_Image, MRLis_Screen)
            // UCF 입장에서 같은 타입의 발표 자료를 파일만 변경했을 때 변경 여부를 알 수 없음.
            // MRLis 타입 발표 자료 등록 시 URL을 Unix timestamp로 등록.
            switch ((ePresentationType)type)
            {
                case ePresentationType.MRLis_Media:
                case ePresentationType.MRLis_Image:
                case ePresentationType.MRLis_Screen:
                    url = DateTime.Now.Subtract(new DateTime(1970, 1, 1, 9, 0, 0)).TotalSeconds.ToString("F0");
                    break;
            }

            mediaSync.RequestChangePresentation(mediaData, type, url, OnResponseSync);

            void OnResponseSync(Enum result)
            {
                // 등록에 대한 callback.
                onRegister?.Invoke(result);

                if (result == null)
                {
                    SyncMediaState(userInfo, mediaData, onInitialize);
                }
                else
                {
                    // 실패 시 initialize 콜백 호출
                    onInitialize?.Invoke(result);
                }
            }
        }

        public void PlayMedia(MediaUser userInfo, LandMediaData mediaData, Action<Enum> onResponse)
        {
            // 통화/Audio interrupt 상태 리셋.
            mediaData.ClearCallAndInterrupt();

            if (provider)
            {
                switch ((ePresentationType)mediaData.presentationType)
                {
                    case ePresentationType.Media:
                        // 로컬 컨트롤 리셋.
                        mediaData.isLocalControl = false;

                        // 메타데이터.
                        var metaData = provider.GetMetaData() as VideoMetaData;
                        if (metaData != null)
                        {
                            // 현재 위치.
                            int position = provider.GetPosition();

                            // loop가 아닌 일반 CMS 영상일 경우.
                            if (metaData.isLive == false && metaData?.isLoop == false)
                            {
                                // 현재 재생 위치가 비디오 재생 길이보다 같거나 크면,
                                // 0부터 시작하도록 동기화 요청.
                                int length = provider.GetMaxPosition() * MILLISECOND;
                                if (position >= length)
                                    position = 0;
                            }

                            mediaSync.UpdateMediaSync(mediaData, CommonEnum.CommonConferenceMediaStatus.Playing, position, OnResponseSync);
                        }
                        break;

                    case ePresentationType.MRLis_Media:
                    case ePresentationType.MRLis_Image:
                    case ePresentationType.MRLis_Screen:
                        mediaSync.UpdateMRLisSync(mediaData, CommonEnum.CommonConferenceMediaStatus.Playing, OnResponseSync);
                        break;
                }
            }
            else
            {
                onResponse?.Invoke(ePresentation.NOT_EXIST_PRESENTATION);
            }

            void OnResponseSync(Enum result)
            {
                // 성공
                if (result == null)
                {
                    // 상태 동기화
                    provider.UpdateState(userInfo, mediaData);
                }

                onResponse?.Invoke(result);
            }
        }

        public void PauseMedia(MediaUser userInfo, LandMediaData mediaData, Action<Enum> onResponse)
        {
            if (provider)
            {
                switch ((ePresentationType)mediaData.presentationType)
                {
                    case ePresentationType.Media:
                        // 현재 위치.
                        int position = provider.GetPosition();
                        mediaSync.UpdateMediaSync(mediaData, CommonEnum.CommonConferenceMediaStatus.Pause, position, OnResponseSync);
                        break;

                    case ePresentationType.MRLis_Media:
                    case ePresentationType.MRLis_Image:
                    case ePresentationType.MRLis_Screen:
                        mediaSync.UpdateMRLisSync(mediaData, CommonEnum.CommonConferenceMediaStatus.Pause, OnResponseSync);
                        break;
                }
            }
            else
            {
                onResponse?.Invoke(ePresentation.NOT_EXIST_PRESENTATION);
            }

            void OnResponseSync(Enum result)
            {
                // 성공
                if (result == null)
                {
                    // 상태 동기화
                    provider.UpdateState(userInfo, mediaData);
                }

                onResponse?.Invoke(result);
            }
        }
        public void PauseMedia(double time, MediaUser userInfo, LandMediaData mediaData, Action<Enum> onResponse)
        {
            if (provider)
            {
                mediaSync.UpdateMediaSync(mediaData, CommonEnum.CommonConferenceMediaStatus.Pause, (int)(time * MILLISECOND), OnResponseSync);
            }
            else
            {
                onResponse?.Invoke(ePresentation.NOT_EXIST_PRESENTATION);
            }

            void OnResponseSync(Enum result)
            {
                // 성공
                if (result == null)
                {
                    // 상태 동기화
                    provider.UpdateState(userInfo, mediaData);
                }

                onResponse?.Invoke(result);
            }
        }

        public void PrevMedia(MediaUser userInfo, LandMediaData mediaData, bool isFirst, Action<Enum> onResponse)
        {
            if (provider)
            {
                int position = provider.GetPrevPosition(isFirst);
                switch ((ePresentationType)mediaData.presentationType)
                {
                    case ePresentationType.PDF:
                        // 변경할 위치.
                        if (provider.IsLoadProvider())
                        {
                            mediaSync.UpdatePDFSync(mediaData, position, OnResponseSync);
                        }
                        else
                        {
                            OnResponseSync(ePresentation.FAILED_INITIALIZE);
                        }
                        break;

                    case ePresentationType.MRLis_Image:
                        mediaData.position = position;
                        OnResponseSync(null);
                        break;
                }
            }
            else
            {
                onResponse?.Invoke(ePresentation.NOT_EXIST_PRESENTATION);
            }

            void OnResponseSync(Enum result)
            {
                // 성공
                if (result == null)
                {
                    // 상태 동기화
                    provider.UpdateState(userInfo, mediaData);
                }

                onResponse?.Invoke(result);
            }
        }

        public void NextMedia(MediaUser userInfo, LandMediaData mediaData, bool isLast, Action<Enum> onResponse)
        {
            if (provider)
            {
                int position = provider.GetNextPosition(isLast);
                switch ((ePresentationType)mediaData.presentationType)
                {
                    case ePresentationType.PDF:
                        // 변경할 위치.
                        if (provider.IsLoadProvider())
                        {
                            mediaSync.UpdatePDFSync(mediaData, position, OnResponseSync);
                        }
                        else
                        {
                            OnResponseSync(ePresentation.FAILED_INITIALIZE);
                        }
                        break;

                    case ePresentationType.MRLis_Image:
                        mediaData.position = position;
                        OnResponseSync(null);
                        break;
                }
            }
            else
            {
                onResponse?.Invoke(ePresentation.NOT_EXIST_PRESENTATION);
            }

            void OnResponseSync(Enum result)
            {
                // 성공
                if (result == null)
                {
                    // 상태 동기화
                    provider.UpdateState(userInfo, mediaData);
                }

                onResponse?.Invoke(result);
            }
        }

        public void MuteMedia(MediaUser userData, LandMediaData mediaData, bool isMute, Action<Enum> onResponse)
        {
            if (provider)
            {
                mediaSync.UpdateMediaMute(mediaData, (byte)(isMute ? 1 : 0), OnResponseSync);
            }
            else
            {
                onResponse?.Invoke(ePresentation.NOT_EXIST_PRESENTATION);
            }

            void OnResponseSync(Enum result)
            {
                // 성공
                if (result == null)
                {
                    // 상태 동기화
                    provider.UpdateMuteState(userData, mediaData);
                }

                onResponse?.Invoke(result);
            }
        }

        public void PlayLocalMedia(MediaUser userInfo, LandMediaData mediaData, Action<Enum> onResponse)
        {
            mediaData.isLocalControl = false;

            // 통화/Audio interrupt 상태 리셋.
            mediaData.ClearCallAndInterrupt();

            if (provider)
            {
                if ((ePresentationType)mediaData.presentationType == ePresentationType.Media)
                {
                    mediaSync.RequestCurrentMediaState(mediaData, OnResponse);
                }
                else
                {
                    onResponse?.Invoke(ePresentation.INVALID_PRESENTATION);
                }
            }
            else
            {
                onResponse?.Invoke(ePresentation.NOT_EXIST_PRESENTATION);
            }

            void OnResponse(Enum result)
            {
                if (result == null)
                {
                    // 상태 동기화
                    provider.UpdateState(userInfo, mediaData);
                    onResponse?.Invoke(null);
                }
                else
                {
                    onResponse.Invoke(result);
                }
            }
        }

        public void PauseLocalMedia(MediaUser userInfo, LandMediaData mediaData, Action<Enum> onResponse)
        {
            mediaData.isLocalControl = true;

            if (provider)
            {
                if ((ePresentationType)mediaData.presentationType == ePresentationType.Media)
                {
                    provider.UpdateState(userInfo, mediaData);
                    onResponse?.Invoke(null);
                }
                else
                {
                    onResponse?.Invoke(ePresentation.INVALID_PRESENTATION);
                }
            }
            else
            {
                onResponse?.Invoke(ePresentation.NOT_EXIST_PRESENTATION);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mediaData"></param>
        /// <param name="dragState"></param>
        /// <param name="position"></param>
        public void MoveVideoPosition(MediaUser userInfo, LandMediaData mediaData, eDragState dragState, float position, Action<int, float> onChangedSeekbar)
        {
            if (provider)
            {
                switch ((ePresentationType)mediaData.presentationType)
                {
                    // telp 서버 동기화 요청.
                    case ePresentationType.Media:
                    {
                        CommonEnum.CommonConferenceMediaStatus mediaState = CommonEnum.CommonConferenceMediaStatus.None;
                        switch (dragState)
                        {
                            // 드래그 시작 시 현재 플레이어 상태 저장.
                            case eDragState.START:
                                mediaState = CommonEnum.CommonConferenceMediaStatus.Pause;
                                mediaData.savedStateForSeek = mediaData.state;
                                break;

                            case eDragState.DRAGGING:
                                mediaState = CommonEnum.CommonConferenceMediaStatus.Pause;
                                break;

                            // 드래그 종료 시 저장했던 상태로.
                            case eDragState.END:
                                mediaState = (CommonEnum.CommonConferenceMediaStatus)mediaData.savedStateForSeek;
                                break;
                        }
                        // dragState를 드래그 상태 확인용으로 사용하기 위해 전달. 기능 동작 X.
                        provider.SetPosition(dragState, position * MILLISECOND);
                        mediaSync.UpdateMediaSync(mediaData, mediaState, (int)(position * MILLISECOND), OnResponseSync);
                        break;
                    }

                    // MRLisCore 호출.
                    case ePresentationType.MRLis_Media:
                    {
                        // dragState를 전달해 영상 재생 위치 변경.
                        provider.SetPosition(dragState, position * MILLISECOND);
                        onChangedSeekbar?.Invoke((int)dragState, position * MILLISECOND);
                        break;
                    }
                }
            }

            void OnResponseSync(Enum result)
            {
                // 성공
                if (result == null)
                {
                    // 상태 동기화
                    provider.UpdateState(userInfo, mediaData);
                }
            }
        }

        public bool GetCurrentPosition(LandMediaData mediaData, out int position)
        {
            position = 0;
            if (provider)
            {
                switch ((ePresentationType)mediaData.presentationType)
                {
                    case ePresentationType.Media:
                        position = provider.GetPosition();
                        return true;

                    default: return false;
                }
            }
            return false;
        }

        public int GetCurrentPosition(LandMediaData mediaData)
        {
            if (provider)
            {
                switch ((ePresentationType)mediaData.presentationType)
                {
                    case ePresentationType.MRLis_Media:
                        return provider.GetPosition() * MILLISECOND;

                    default:
                        return provider.GetPosition();
                }
            }
            return 0;
        }

        public int GetMaxPosition(LandMediaData mediaData)
        {
            if (provider)
            {
                return provider.GetMaxPosition();
            }
            return 0;
        }

        public bool GetLiveFlag()
        {
            if (provider)
            {
                if (provider.GetMetaData() is VideoMetaData data)
                {
                    return data.isLive;
                }
            }
            return false;
        }

        public void UpdateResolution(int width, int height)
        {
            if (provider)
            {
                provider.UpdateResolution(width, height);
            }
        }
        #endregion

        #region Event handler
        void IMediaModuleEventHandler.OnMediaProgress(BaseProvider provider, double current, double length)
        {
            eventHandler?.OnProgressMedia(current, length);
        }
        void IMediaModuleEventHandler.OnPlayerFinished(BaseProvider provider, double time)
        {
            eventHandler?.OnFinishedMedia(time);
        }
        void IMediaModuleEventHandler.OnVideoPlayerEvent(BaseProvider provider, object data)
        {
            eventHandler?.OnVideoEvent(data);
        }
        #endregion
    }
}