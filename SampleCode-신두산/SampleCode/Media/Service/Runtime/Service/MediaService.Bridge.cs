using System;
using  UCF.Core.Helper.Telepresence;
using UnityEngine;

namespace  UCF.Media.Service
{
    using Core.Bridge;

    public partial class MediaService : MonoBehaviour
    {
        public void SyncMediaData(ilUser userInfo, LandMediaData landInfo, bool isSnapshot, Action<ResponseService> onResponse)
        {
            var mediaUser = userInfo as MediaUser;
            if (mediaUser == null)
            {
                Debug.LogError("mediaUser is null");
                return;
            }

            // TotalSnapshot에서 호출 됐는지 여부. (BG->FG에 의해 발생한 이벤트인지 여부).
            landInfo.isSnapshot = isSnapshot;

            mediaProvider.SyncMediaState(mediaUser, landInfo, OnSync);
            mediaProvider.SyncMuteState(mediaUser, landInfo);

            void OnSync(Enum result)
            {
                // 값 초기화.
                landInfo.isSnapshot = false;

                MediaDefinition.InitializePresentation responseData = new MediaDefinition.InitializePresentation
                {
                    type = landInfo.presentationType,
                    state = landInfo.state,
                    current = mediaProvider.GetCurrentPosition(landInfo),
                    max = mediaProvider.GetMaxPosition(landInfo),
                    filePath = landInfo.url,
                    mute = landInfo.isMediaMute == 1,
                    isLive = mediaProvider.GetLiveFlag()
                };

                MediaDefinition.ChangeVideoState? videoState = null;
                MediaDefinition.ChangePDFState? pdfState = null;

                switch ((ePresentationType)landInfo.presentationType)
                {
                    case ePresentationType.PDF:
                        pdfState = new MediaDefinition.ChangePDFState
                        {
                            type = responseData.type,
                            current = responseData.current,
                            max = responseData.max
                        };
                        break;

                    case ePresentationType.Media:
                        videoState = new MediaDefinition.ChangeVideoState
                        {
                            state = responseData.state
                        };
                        break;
                }

                onResponse?.Invoke(new ResponseService(responseData, videoState, pdfState));
            }
        }

        #region Telepresence notifications
        /// <summary>
        /// PDF 상태 변경 알림
        /// </summary>
        /// <param name="landInfo"></param>
        /// <param name="url"></param>
        /// <param name="page"></param>
        public void NotifyPresentationPDF(ilUser userInfo, LandMediaData landInfo, string url, int page, Action<ResponseService> onResponse)
        {
            landInfo.url = url;
            landInfo.position = page;

            mediaProvider.UpdateState(userInfo as MediaUser, landInfo);
            onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangePDFState
            {
                type = landInfo.presentationType,
                current = landInfo.position,
                max = mediaProvider.GetMaxPosition(landInfo)
            }));
        }

        /// <summary>
        /// Media 상태 변경 알림
        /// </summary>
        /// <param name="landInfo"></param>
        /// <param name="url"></param>
        /// <param name="status"></param>
        /// <param name="seek"></param>
        public void NotifyPresentationMedia(ilUser userInfo, LandMediaData landInfo, string url, CommonEnum.CommonConferenceMediaStatus status, int seek, Action<ResponseService> onResponse)
        {
            var mediaUser = userInfo as MediaUser;
            landInfo.url = url;
            landInfo.state = status;
            landInfo.position = seek;

            mediaProvider.SyncMediaState(mediaUser, landInfo, OnResponse);
            mediaProvider.SyncMuteState(mediaUser, landInfo);

            void OnResponse(Enum result)
            {
                onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeVideoState { state = landInfo.state }));
            }
        }

        /// <summary>
        /// MRLis 상태 변경 알림
        /// </summary>
        /// <param name="landInfo"></param>
        /// <param name="url"></param>
        /// <param name="status"></param>
        /// <param name="seek"></param>
        public void NotifyPresentationMRLis(ilUser userInfo, LandMediaData landInfo, CommonEnum.CommonConferenceMediaStatus status, Action<ResponseService> onResponse)
        {
            landInfo.state = status;

            mediaProvider.UpdateState(userInfo as MediaUser, landInfo);
            onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeMRLisState { state = landInfo.state }));
        }

        /// <summary>
        /// 발표자료 상태 변경 알림
        /// </summary>
        /// <param name="landInfo"></param>
        /// <param name="type"></param>
        /// <param name="url"></param>
        public void NotifyPresentationFile(ilUser userInfo, LandMediaData landInfo, CommonEnum.CommonConferenceURLFormat type, string url, Action<ResponseService> onResponse)
        {
            landInfo.Clear();
            landInfo.presentationType = type;
            landInfo.url = url;
            var mediaUser = userInfo as MediaUser;

            if (IsMainThread)
            {
                mediaProvider.SyncMediaState(mediaUser, landInfo, OnSync);
                mediaProvider.SyncMuteState(mediaUser, landInfo);
            }
            else
            {
                OnSync(null);
            }

            void OnSync(Enum result)
            {
                MediaDefinition.ChangeVideoState? videoState = null;
                MediaDefinition.ChangePDFState? pdfState = null;

                MediaDefinition.InitializePresentation responseData = new MediaDefinition.InitializePresentation
                {
                    type = landInfo.presentationType,
                    state = landInfo.state,
                    current = landInfo.position,
                    max = mediaProvider.GetMaxPosition(landInfo),
                    filePath = landInfo.url,
                    mute = landInfo.isMediaMute == 1,
                    isLive = mediaProvider.GetLiveFlag()
                };

                switch ((ePresentationType)landInfo.presentationType)
                {
                    case ePresentationType.PDF:
                        pdfState = new MediaDefinition.ChangePDFState
                        {
                            type = responseData.type,
                            current = responseData.current,
                            max = responseData.max
                        };
                        break;

                    case ePresentationType.Media:
                        videoState = new MediaDefinition.ChangeVideoState
                        {
                            state = responseData.state
                        };
                        break;
                }

                onResponse?.Invoke(new ResponseService(responseData, videoState, pdfState));
            }
        }

        /// <summary>
        /// 발표자료 음소거 상태 변경 알림
        /// </summary>
        /// <param name="landInfo"></param>
        /// <param name="isMute"></param>
        /// <param name="onResponse"></param>
        public void NotifyPresentationMute(ilUser userInfo, LandMediaData landInfo, byte isMute, Action<ResponseService> onResponse)
        {
            landInfo.isMediaMute = isMute;

            mediaProvider.SyncMuteState(userInfo as MediaUser, landInfo);

            MediaDefinition.MuteVideo responseData = new MediaDefinition.MuteVideo
            {
                mute = isMute == 1
            };
            onResponse?.Invoke(new ResponseService(responseData));
        }
        #endregion

        #region NativeBridge interfaces
        public void RegisterPresentation(string jsonData, ilUser localUser, LandMediaData landInfo, Action<ResponseService> onRegister, Action<ResponseService> onInitialize)
        {
            if (ValidateControlAuth(localUser))
            {
                var data = JsonUtility.FromJson<MediaDefinition.RegisterPresentation>(jsonData);
                mediaProvider.ChangeMedia(localUser as MediaUser, landInfo, data.type, data.filePath, OnRegister, OnInitialize);
            }
            else
            {
                OnRegister(ePresentation.NOT_PRESENTER);
            }

            // 등록 완료 콜백.
            void OnRegister(Enum result)
            {
                if (result == null)
                {
                    MediaDefinition.RegisterPresentation ackData = new MediaDefinition.RegisterPresentation
                    {
                        type = landInfo.presentationType,
                        filePath = landInfo.url
                    };

                    onRegister?.Invoke(new ResponseService(ackData));

                    extension?.CheckMediaState(landInfo);
                }
                else
                {
                    onRegister?.Invoke(new ResponseService(new FailResult(result)));
                }
            }

            // 초기화 완료 콜백.
            void OnInitialize(Enum result)
            {
                MediaDefinition.InitializePresentation ntyData = new MediaDefinition.InitializePresentation
                {
                    type = landInfo.presentationType,
                    state = landInfo.state,
                    current = landInfo.position,
                    max = mediaProvider.GetMaxPosition(landInfo),
                    filePath = landInfo.url,
                    mute = landInfo.isMediaMute == 1,
                    isLive = mediaProvider.GetLiveFlag()
                };

                onInitialize?.Invoke(new ResponseService(ntyData));
            }
        }

        public void RegisterLocalPresentation(string jsonData, ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {        
                var data = JsonUtility.FromJson<MediaDefinition.RegisterLocalPresentation>(jsonData);
                landInfo.presentationType = data.type;
                landInfo.url = data.filePath;

                mediaProvider.SyncMediaState(localUser as MediaUser, landInfo, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService());
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void UnregisterPresentation(string jsonData, ilUser localUser, LandMediaData landInfo, Action<ResponseService> onRegister, Action<ResponseService> onInitialize)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.ChangeMedia(localUser as MediaUser, landInfo, (CommonEnum.CommonConferenceURLFormat)(-1), "", OnRegister, OnInitialize);
            }
            else
            {
                OnRegister(ePresentation.NOT_PRESENTER);
            }

            void OnRegister(Enum result)
            {
                if (result == null)
                {
                    onRegister?.Invoke(new ResponseService());

                    extension?.CheckMediaState(landInfo);
                }
                else
                {
                    onRegister?.Invoke(new ResponseService(new FailResult(result)));
                }
            }

            void OnInitialize(Enum result)
            {
                MediaDefinition.InitializePresentation ntyData = new MediaDefinition.InitializePresentation
                {
                    type = landInfo.presentationType,
                    state = landInfo.state,
                    current = landInfo.position,
                    max = mediaProvider.GetMaxPosition(landInfo),
                    filePath = landInfo.url,
                    mute = landInfo.isMediaMute == 1,
                    isLive = mediaProvider.GetLiveFlag()
                };

                onInitialize?.Invoke(new ResponseService(ntyData));
            }
        }

        #region PDF control
        public void MoveFirstPage(ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.PrevMedia(localUser as MediaUser, landInfo, true, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    var responseData = new MediaDefinition.ChangePDFState
                    {
                        type = landInfo.presentationType,
                        current = landInfo.position,
                        max = mediaProvider.GetMaxPosition(landInfo)
                    };
                    onResponse?.Invoke(new ResponseService(responseData));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void MoveLastPage(ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.NextMedia(localUser as MediaUser, landInfo, true, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    var responseData = new MediaDefinition.ChangePDFState
                    {
                        type = landInfo.presentationType,
                        current = landInfo.position,
                        max = mediaProvider.GetMaxPosition(landInfo)
                    };
                    onResponse?.Invoke(new ResponseService(responseData));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void MovePrevPage(ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.PrevMedia(localUser as MediaUser, landInfo, false, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    MediaDefinition.ChangePDFState? responseData = null;
                    MediaDefinition.SeekbarUpdate seekbarData = null;

                    switch ((ePresentationType)landInfo.presentationType)
                    {
                        case ePresentationType.PDF:
                            responseData = new MediaDefinition.ChangePDFState
                            {
                                type = landInfo.presentationType,
                                current = landInfo.position,
                                max = mediaProvider.GetMaxPosition(landInfo)
                            };
                            break;

                        case ePresentationType.MRLis_Image:
                            seekbarData = new MediaDefinition.SeekbarUpdate
                            {
                                state = (int)eDragState.END,
                                position = landInfo.position
                            };
                            break;
                    }

                    onResponse?.Invoke(new ResponseService(responseData, seekbarData));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void MoveNextPage(ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.NextMedia(localUser as MediaUser, landInfo, false, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    MediaDefinition.ChangePDFState? responseData = null;
                    MediaDefinition.SeekbarUpdate seekbarData = null;

                    switch ((ePresentationType)landInfo.presentationType)
                    {
                        case ePresentationType.PDF:
                            responseData = new MediaDefinition.ChangePDFState
                            {
                                type = landInfo.presentationType,
                                current = landInfo.position,
                                max = mediaProvider.GetMaxPosition(landInfo)
                            };
                            break;

                        case ePresentationType.MRLis_Image:
                            seekbarData = new MediaDefinition.SeekbarUpdate
                            {
                                state = (int)eDragState.END,
                                position = landInfo.position
                            };
                            break;
                    }

                    onResponse?.Invoke(new ResponseService(responseData, seekbarData));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }
        #endregion

        #region Video control
        public void PlayVideo(ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            mediaProvider.PlayMedia(localUser as MediaUser, landInfo, OnResponse);

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeVideoState { state = landInfo.state }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void PauseVideo(ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.PauseMedia(localUser as MediaUser, landInfo, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeVideoState { state = landInfo.state }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void PauseVideo(double time, ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.PauseMedia(time, localUser as MediaUser, landInfo, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeVideoState { state = landInfo.state }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void MuteVideo(string jsonData, ilUser localUser, ilInfo landInfo, Action<ResponseService> onResponse)
        {
            MediaDefinition.MuteVideo data = default;
            if (ValidateControlAuth(localUser))
            {
                data = JsonUtility.FromJson<MediaDefinition.MuteVideo>(jsonData);
                mediaProvider.MuteMedia(localUser as MediaUser, landInfo as LandMediaData, data.mute, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.MuteVideo { mute = data.mute }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void ChangeVideoVolume(string jsonData, ilUser localUser, ilInfo landInfo, Action<ResponseService> onResponse)
        {
        }

        public void CurrentVideoPosition(string jsonData, ilInfo landInfo, Action<ResponseService> onResponse)
        {
            if (mediaProvider.GetCurrentPosition(landInfo as LandMediaData, out int position))
            {
                onResponse?.Invoke(new ResponseService(new MediaDefinition.CurrentVideoPosition { current = position }));
            }
        }

        public void MuteLocalMedia(string jsonData, ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            MediaDefinition.MuteLocalMedia data = JsonUtility.FromJson<MediaDefinition.MuteLocalMedia>(jsonData);

            // 값 변경.
            landInfo.IsLocalMute = data.isMute ? 1 : 0;

            // 뮤트 상태 동기화
            mediaProvider.SyncMuteState(localUser as MediaUser, landInfo);

            onResponse?.Invoke(new ResponseService(new MediaDefinition.MuteLocalMedia { isMute = data.isMute }));
        }

        public void PlayLocalVideo(ilUser localUser, ilInfo landInfo, Action<ResponseService> onResponse)
        {
            mediaProvider.PlayLocalMedia(localUser as MediaUser, landInfo as LandMediaData, OnResponse);

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService());
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }
        public void PauseLocalVideo(ilUser localUser, ilInfo landInfo, Action<ResponseService> onResponse)
        {
            mediaProvider.PauseLocalMedia(localUser as MediaUser, landInfo as LandMediaData, OnResponse);

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService());
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }
        #endregion

        #region MRLis control
        public void PlayMRLis(ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.PlayMedia(localUser as MediaUser, landInfo, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeMRLisState { state = landInfo.state }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void PauseMRLis(ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            if (ValidateControlAuth(localUser))
            {
                mediaProvider.PauseMedia(localUser as MediaUser, landInfo, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeMRLisState { state = landInfo.state }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void MoveVideoPosition(ilUser userInfo, ilInfo landInfo, eDragState dragState, float position, Action<ResponseService> onResponse)
        {
            var mediaData = landInfo as LandMediaData;
            var mediaUser = userInfo as MediaUser;

            mediaProvider.MoveVideoPosition(mediaUser, mediaData, dragState, position, OnChangedSeekbar);

            void OnChangedSeekbar(int state, float pos)
            {
                onResponse?.Invoke(new ResponseService(new MediaDefinition.SeekbarUpdate { state = state, position = (long)pos }));
            }
        }
        #endregion

        public void ChangeCallState(string jsonData, ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            MediaDefinition.CallState data = JsonUtility.FromJson<MediaDefinition.CallState>(jsonData);

            mediaProvider.ChangeCallState(localUser as MediaUser, landInfo, data, OnResponse);

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeVideoState { state = landInfo.state }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void ChangeAudioInterrupt(string jsonData, ilUser localUser, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            MediaDefinition.AudioInterruptState data = JsonUtility.FromJson<MediaDefinition.AudioInterruptState>(jsonData);

            mediaProvider.ChangeAudioInterrupt(localUser as MediaUser, landInfo, data, OnResponse);

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeVideoState { state = landInfo.state }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        public void ChangePresenter(int userIndex, ilUserDataContainer.ilUserDataTable dataTable, LandMediaData landInfo, Action<ResponseService> onResponse)
        {
            var presenter = dataTable.GetUserData<MediaUser>(userIndex);

            // 변경된 발표자가 본인 일때만 처리.
            if (presenter.IsLocal)
            {
                mediaProvider.ChangePresenter(presenter, landInfo, OnResponse);
            }
            else
            {
                OnResponse(ePresentation.NOT_PRESENTER);
            }

            void OnResponse(Enum enumResult)
            {
                if (enumResult == null)
                {
                    onResponse?.Invoke(new ResponseService(new MediaDefinition.ChangeVideoState { state = landInfo.state }));
                }
                else
                {
                    onResponse?.Invoke(new ResponseService(new FailResult(enumResult)));
                }
            }
        }

        /// <summary>
        /// 해상도 변경 이벤트
        /// </summary>
        /// <param name="landInfo"></param>
        public void ChangeResolution(int width, int height)
        {
            mediaProvider.UpdateResolution(width, height);
        }

        #endregion

        /// <summary>
        /// 유저 발표자료 제어 권한 검사
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        private bool ValidateControlAuth(ilUser userInfo)
        {
            return userInfo.IsPresenter;
        }
    }
}