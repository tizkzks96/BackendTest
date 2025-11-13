using System;
using  UCF.Core.Helper.Telepresence;

namespace  UCF.Media.Service
{
    using Module;

    internal partial class MediaProvider : IMediaModuleEventHandler, IDisposable
    {
        private void ResumeMediaByPolicy(MediaUser userInfo, LandMediaData mediaData, Action<Enum> onResponse)
        {
            // FG에서 처리.
            if (MediaService.IsMainThread)
            {
                // CMS 일 경우에만 해당하는 case.
                if (mediaData.presentationType == CommonEnum.CommonConferenceURLFormat.Media)
                {
                    if (mediaData.isCalling)
                    {
                        // 아직 통화 중 일때,
                        // 상태 변경 후 업데이트.
                        mediaData.state = CommonEnum.CommonConferenceMediaStatus.Pause;
                        OnResponse(null);
                    }
                    else
                    {
                        mediaData.pauseByCalling = false;

                        // FG 전환, 통화 상태가 아닐 때.
                        // 동기화 완료된 데이터로 업데이트.
                        OnResponse(null);
                    }
                }
                else
                {
                    OnResponse(null);
                }
            }
            else
            {
                OnResponse(null);
            }

            void OnResponse(Enum result)
            {
                if (result == null)
                {
                    provider.UpdateState(userInfo, mediaData);
                }

                onResponse?.Invoke(result);
            }
        }

        public void ChangeCallState(MediaUser userData, LandMediaData mediaData, MediaDefinition.CallState state, Action<Enum> onResponse)
        {
            // 통화 상태 변경.
            mediaData.SetCallState(state.state);

            if (provider)
            {
                // CMS 일 경우에만 해당하는 case.
                if (mediaData.presentationType == CommonEnum.CommonConferenceURLFormat.Media)
                {
#if UNITY_ANDROID
                    OnProcessWhenPhoneCall_Android(userData, mediaData, onResponse);
#elif UNITY_IOS
                    OnProcessWhenPhoneCall_iOS(userData, mediaData, onResponse);
#else
                    onResponse?.Invoke(ePresentation.INVALID_PRESENTATION);
#endif
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
        /// Android 통화 상태 시 처리 로직
        /// </summary>
        private void OnProcessWhenPhoneCall_Android(MediaUser userData, LandMediaData mediaData, Action<Enum> onResponse)
        {
            // android는 mute처리만 함.
            provider.UpdateMuteState(userData, mediaData);
            onResponse?.Invoke(null);
        }

        /// <summary>
        /// iOS 통화 상태 시 처리 로직
        /// </summary>
        private void OnProcessWhenPhoneCall_iOS(MediaUser userData, LandMediaData mediaData, Action<Enum> onResponse)
        {
            var metaData = provider.GetMetaData() as VideoMetaData;

            if (mediaData.isCalling)
            {
                // 재생 중.
                if (mediaData.state == CommonEnum.CommonConferenceMediaStatus.Playing)
                {
                    // 통화에 의해 일시정지됨.
                    mediaData.pauseByCalling = true;

                    // FG에서만 처리
                    // why? BG에서는 이미 정지상태이기 때문.
                    if (MediaService.IsMainThread)
                    {
                        mediaData.state = CommonEnum.CommonConferenceMediaStatus.Pause;
                    }
                }

                OnResponse(null);
            }
            else
            {
                // 통화 끝.
                mediaData.pauseByCalling = false;

                // 재생 동기화 정보 요청 후 update.
                mediaSync.RequestCurrentMediaState(mediaData, OnResponse);
            }

            void OnResponse(Enum result)
            {
                provider.UpdateState(userData, mediaData);
                onResponse?.Invoke(result);
            }
        }

        public void ChangeAudioInterrupt(MediaUser userData, LandMediaData mediaData, MediaDefinition.AudioInterruptState state, Action<Enum> onResponse)
        {
            // 인터럽트 상태 변경.
            mediaData.SetInterruptState(state.state);

            if (provider)
            {
                // CMS 일 경우에만 해당하는 case.
                if (mediaData.presentationType == CommonEnum.CommonConferenceURLFormat.Media)
                {
#if UNITY_IOS
                    OnProcessWhenInterrupt_iOS(userData, mediaData, onResponse);
#else
                    onResponse?.Invoke(ePresentation.INVALID_PRESENTATION);
#endif
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
        /// iOS Audio interrupt 상태 변경 시 처리 로직
        /// </summary>
        private void OnProcessWhenInterrupt_iOS(MediaUser userData, LandMediaData mediaData, Action<Enum> onResponse)
        {
            var metaData = provider.GetMetaData() as VideoMetaData;

            if (mediaData.isInterrupt)
            {
                // 재생 중.
                if (mediaData.state == CommonEnum.CommonConferenceMediaStatus.Playing)
                {
                    // interrupt에 의해 일시정지됨.
                    mediaData.pauseByInterrupt = true;

                    // FG에서만 처리
                    // why? BG에서는 이미 정지상태이기 때문.
                    if (MediaService.IsMainThread)
                    {
                        mediaData.state = CommonEnum.CommonConferenceMediaStatus.Pause;
                    }
                }

                OnResponse(null);
            }
            else
            {
                // interrupt 끝.
                mediaData.pauseByInterrupt = false;

                // 재생 동기화 정보 요청 후 update.
                mediaSync.RequestCurrentMediaState(mediaData, OnResponse);
            }

            void OnResponse(Enum result)
            {
                provider.UpdateState(userData, mediaData);
                onResponse?.Invoke(result);
            }
        }

        public void ChangePresenter(MediaUser userData, LandMediaData mediaData, Action<Enum> onResponse)
        {
            if (provider)
            {
                // CMS 일 경우에만 해당하는 case.
                if (mediaData.presentationType == CommonEnum.CommonConferenceURLFormat.Media)
                {
                    // 발표자 권한 이관 받는 유저가 로컬 pause 상태 일때,
                    if (mediaData.isLocalControl)
                    {
                        // 현재 telp서버의 seek 데이터를 받아옴,
                        mediaSync.RequestCurrentMediaState(mediaData, (e) =>
                        {
                            if (e == null)
                            {
                                // 이전 발표자의 seek 위치 (Telp 서버의 seek 데이터)로 동기화.
                                mediaSync.UpdateMediaSync(mediaData, CommonEnum.CommonConferenceMediaStatus.Pause, mediaData.position, OnResponse);
                            }
                            else
                            {
                                OnResponse(e);
                            }
                        });
                    }
                }
            }

            void OnResponse(Enum result)
            {
                if (result == null)
                {
                    provider.UpdateState(userData, mediaData);
                    provider.UpdateMuteState(userData, mediaData);
                }

                onResponse?.Invoke(result);
            }
        }

        /// <summary>
        /// BG/통화/Interrupt에 의한 정지가 발생했는지에 대한 여부
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool CheckPolicy(LandMediaData data)
        {
            return data.pauseByCalling || data.pauseByInterrupt;
        }
    }
}