using System;
using System.Threading;
using UnityEngine;

namespace  UCF.Media.Service
{
    using Module;

    internal sealed class VideoProvider : BaseProvider, IVideoEventHandler
    {
        private const int MILLISECOND = 1000;
        private const int LIMIT_SKIP_SEEK_TIME = 5;

        private VideoPlayer player;
        private VideoRenderer videoRenderer;
        private VideoMetaData metaData;

        private IMediaModuleEventHandler eventHandler;

        private bool isLoadComplete = false;
        private Action<Enum> onLoadComplete;
        private MediaDefinition.AVProEvent eventData;
        private bool isFinished = false;

        private bool isLocalControl = false;
        private ePlayerState playerState;
        internal string currentPath = string.Empty;
        private bool isLoop;
        private bool isCache;

        private bool isCall = false;
        private bool isInterrupt = false;

        private DateTime pauseTime = DateTime.Now;

        private CancellationTokenSource tokenSource;

        public override void Initialize(IMediaModuleEventHandler handler, Material material)
        {
            player = new VideoPlayer(transform, this);
            InitializeRenderer(material);

            eventHandler = handler;

            // 이벤트 데이터 초기화.
            eventData = new MediaDefinition.AVProEvent();
            eventData.details = new MediaDefinition.AVProEvent.Details();
        }

        private void InitializeRenderer(Material material)
        {
            if (material == null)
            {
                videoRenderer = new VideoRenderer(player.CreateVideoRender(transform));
            }
            else
            {
                material.shader = CachedShader.GetShader(CachedShader.RGB_SHADER_NAME);

                var render = player.CreateVideoRender(transform);
                render.SetMaterial(material);
            }
        }

        public override void Dispose()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
                tokenSource.Dispose();
                tokenSource = null;
            }

            videoRenderer?.Dispose();
            player.Dispose();

            Destroy(this.gameObject);
        }

        public override bool IsLoadProvider()
        {
            return isLoadComplete;
        }

        public override async void Load(string path, Action<Enum> onResponse)
        {
            onLoadComplete = onResponse;
            currentPath = path;

            isLoop = false;
            isCache = true;

            //loop video 처리를 위해 URL에 #loop을 붙여 전달한다.
            if (path.Contains(MediaDefinition.LOOP))
            {
                // #loop 제거.
                path = path.Replace(MediaDefinition.LOOP, "");
                isLoop = true;
            }

            if (path.Contains(MediaDefinition.NO_CACHE))
            {
                // #no-cache 제거.
                path = path.Replace(MediaDefinition.NO_CACHE, "");
                isCache = false;
            }
            else
            {
                tokenSource = new CancellationTokenSource();
                (bool success, bool cache) = await VideoHelper.ValidateCacheVideo(path, tokenSource.Token);
                if (success == true)
                {
                    // 캐시 미지원으로 인해 캐시 비활성화.
                    if (cache == false)
                    {
                        isCache = false;
                    }
                }
                else
                {
                    // Live Stream 파싱 실패. 캐시 비활성화.
                    isCache = false;
                }

                tokenSource = null;
                Debug.Log($"[VideoProvider] Check live stream. success ? {success} / cache ? {cache}");
            }

            player.CacheVideo(path, isCache, VideoConfig.StorageInfo.isAvailableStorage);
            player.LoadAsync(path, isLoop);
        }

        public override void WaitLoadMedia(MediaUser userInfo, LandMediaData data, Action<Enum> onResponse)
        {
            onResponse?.Invoke(null);
        }

        private void FireEventLoadComplete(bool success)
        {
            if (onLoadComplete != null)
            {
                Enum result = null;
                if (!success)
                {
                    result = ePresentation.FAILED_INITIALIZE;
                }

                isLoadComplete = success;

                onLoadComplete.Invoke(result);
                onLoadComplete = null;
            }
        }

        private void LateUpdate()
        {
            if (player == null)
                return;

            if (player.IsLoaded == false)
                return;

            CheckPlayerState();

            if (player.IsRunning)
            {
                eventHandler?.OnMediaProgress(this, player.GetCurrentTime(), player.GetVideoLength());
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                pauseTime = DateTime.Now;
                player.Pause();
            }
        }

        public override async void UpdateState(MediaUser userInfo, LandMediaData mediaData)
        {
            playerState = (ePlayerState)mediaData.state;

            isCall = mediaData.isCalling;
            isInterrupt = mediaData.isInterrupt;
            isLocalControl = mediaData.isLocalControl;

            if (!MediaService.IsMainThread)
            {
                Debug.Log("[VideoProvider] UpdateState() is call only main thread.");
                return;
            }

            // 통화/interrupt로 인한 local control 상태 일때,
            // 동기화 로직 수행하지 않도록.
            if (isLocalControl)
            {
                pauseTime = DateTime.Now;
                player.Pause();
                return;
            }

            double seek = mediaData.position / MILLISECOND;
            switch (playerState)
            {
                case ePlayerState.None:
                case ePlayerState.Pause:
                    pauseTime = DateTime.Now;
                    player.Pause(seek);
                    break;

                case ePlayerState.Playing:
                    isFinished = false;
                    player.Play(seek, CheckSkipSeek());
                    break;
            }

            player.Mute(GetMuteState(mediaData));
            
            bool CheckSkipSeek()
            {
                bool ret = false;
                double time = (DateTime.Now - pauseTime).TotalSeconds;
                Debug.Log($"CheckSkipSeek {time}");
                // pause 되어있던 시간이 3초 미만이면 skip seek.
                if (time < LIMIT_SKIP_SEEK_TIME)
                    ret = true;

                return ret;
            }
        }

        public override void UpdateMuteState(MediaUser userData, LandMediaData mediaData)
        {
            if (!MediaService.IsMainThread)
            {
                Debug.Log("[VideoProvider] UpdateMuteState() is call only main thread.");
                return;
            }

            if (userData != null)
            {
                // 사용자 마이크 변경 상태에 따라 발표 자료의 사운드를 줄일 지 결정.
                bool isMute = (eMicState)userData.MicState != eMicState.MUTE_OFF;
                float volume = isMute ? 100f : 7f;

                player.SetVolume(volume);
            }

            player.Mute(GetMuteState(mediaData));
        }

        public override int GetPrevPosition(bool isFirst)
        {
            return 0;
        }

        public override int GetNextPosition(bool isLast)
        {
            return 0;
        }

        public override int GetPosition()
        {
            if (player.IsLive)
            {
                return -1;
            }
            else
            {
                return (int)(player.GetPosition() * MILLISECOND);
            }
        }

        public override int GetMaxPosition()
        {
            return (int)player.GetVideoLength();
        }

        /// <summary>
        /// 드래그 상태 확인 용도로 사용.
        /// </summary>
        /// <param name="dragState"></param>
        /// <param name="position"></param>
        public override void SetPosition(eDragState dragState, float position)
        {
            // this.dragState = dragState;
        }

        private bool GetMuteState(LandMediaData mediaData)
        {
            bool isMute = false;
            if (mediaData.isCalling || mediaData.isInterrupt)
            {
                isMute = true;
            }
            else if (mediaData.isMediaMute == 1 || mediaData.IsLocalMute == 1)
            {
                // 둘 중 하나라도 mute이면 mute.
                isMute = true;
            }

            return isMute;
        }

        /// <summary>
        /// 현재 데이터와 동일한지 검사
        /// </summary>
        /// <param name="mediaData"></param>
        /// <returns></returns>
        public override bool CheckMediaData(LandMediaData mediaData)
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                return false;
            }

            return currentPath == mediaData.url;
        }

        private void CheckPlayerState()
        {
            switch (playerState)
            {
                case ePlayerState.Playing:
                    if (!isLocalControl)
                    { // local control 상태가 아닐 때.
                        if (!isFinished)
                        { // AVPro 재생 종료 이벤트가 오지 않았을 때.
                            // 통화/Audio interrupt에 의해 일시정지가 아닌 경우.
                            // 멈추면 자동 재생하도록 로직 추가.
                            if (isCall == false && isInterrupt == false)
                            {
                                if (player.IsPlaying == false)
                                {
                                    player.ForcePlay();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (player.IsPlaying)
                        {
                            player.Pause();
                        }
                    }
                    break;
            }
        }

        public override MediaMetaData GetMetaData()
        {
            if (metaData == null)
                metaData = new VideoMetaData();

            metaData.isLive = player.IsLive;
            metaData.isLoop = player.IsLoop;

            return metaData;
        }

        public override void UpdateResolution(int width, int height)
        {
        }

        public override bool IsPlayingAudio()
        {
            if (player.IsLoaded)
            {
                return player.IsPlaying;
            }
            return false;
        }

        void IVideoEventHandler.OnMediaPlayerEvent(EventType et, ErrorCode errorCode)
        {
            switch (et)
            {
                case EventType.FirstFrameReady:
                    FireEventLoadComplete(true);
                    break;

                case EventType.Error:
                    FireEventLoadComplete(false);
                    break;

                case EventType.FinishedPlaying:
                    isFinished = true;
                    eventHandler?.OnPlayerFinished(this, player.GetVideoLength());
                    break;
            }

            eventData.eventCode = (int)et;
            eventData.eventMsg = et.ToString();
            eventData.errorCode = (int)errorCode;
            eventData.errorMsg = errorCode.ToString();

            eventData.details.playing = player.IsPlaying;
            eventData.details.length = player.GetVideoLength();
            eventData.details.current = player.GetCurrentTime();

            Vector2 bufferedTime = player.GetBufferedTime();
            eventData.details.bufferedTimeMin = bufferedTime.x;
            eventData.details.bufferedTimeMax = bufferedTime.y;

            eventHandler?.OnVideoPlayerEvent(this, eventData);
        }
    }
}