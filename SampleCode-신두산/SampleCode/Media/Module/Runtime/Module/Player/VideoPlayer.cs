using System;
using System.Threading.Tasks;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

namespace  UCF.Media.Module
{
    [Serializable]
    public class VideoPlayer : IDisposable
    {
        public bool IsRunning { get; private set; }
        public bool IsLoaded { get; private set; }
        public string URL { get; private set; }
        public bool IsLive { get; private set; }
        public bool IsLoop { get; private set; }
        public bool IsCache { get; private set; }

        private IVideoEventHandler handler;

        private MediaPlayerWrapper mediaPlayer;
        private VideoRender videoRender;

        private bool isMetaReady;

        private MediaPathType mediaPathType;

        private double savedPosition;
        private bool savedPlayState = false;

        public bool IsPlaying
        {
            get
            {
                return mediaPlayer.Control.IsPlaying();
            }
        }

        public VideoPlayer (Transform transform, IVideoEventHandler handler)
        {
            this.handler = handler;

            mediaPlayer = CreatePlayer(transform);
        }

        public void Dispose()
        {
            mediaPlayer.ForceDispose();
        }

        public VideoRender CreateVideoRender(Transform transform)
        {
            videoRender = new VideoRender(transform, mediaPlayer);
            return videoRender;
        }

        private MediaPlayerWrapper CreatePlayer(Transform transform)
        {
            MediaPlayerWrapper mediaPlayer = transform.gameObject.AddComponent<MediaPlayerWrapper>();
            mediaPlayer.enabled = false;
            mediaPlayer.AutoOpen = false;
            mediaPlayer.AutoStart = false;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            mediaPlayer.PlatformOptionsWindows.audioOutput = Windows.AudioOutput.Unity;
            AddAudioOutput(transform, mediaPlayer);
#elif !UNITY_EDITOR && UNITY_ANDROID
            mediaPlayer.PlatformOptionsAndroid.audioOutput = Android.AudioOutput.Unity;
            AddAudioOutput(transform, mediaPlayer);
#endif

#if UNITY_IOS
            if (VideoConfig.UseVideoOptimizeOption)
            {
                mediaPlayer.PlatformOptionsIOS.textureFormat = MediaPlayer.OptionsApple.TextureFormat.YCbCr420;
            }
#endif

            mediaPlayer.Events.AddListener(OnMediaPlayerEvent);
            mediaPlayer.Initialize();
            return mediaPlayer;
        }

        private void AddAudioOutput(Transform transform, MediaPlayer mediaPlayer)
        {
            AudioOutput audioOutput = transform.gameObject.AddComponent<AudioOutput>();
            audioOutput.Player = mediaPlayer;

            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        private void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            Debug.Log($"[VideoPlayer] OnAudioConfigurationChanged: {deviceWasChanged}");
            if (mediaPlayer == null || mediaPlayer.Control == null)
            {
                return;
            }

            Debug.Log("[VideoPlayer] re-open for bluetooth issue.");

            var tracks = mediaPlayer.AudioTracks.GetAudioTracks();
            if (tracks != null)
            {
                // no sound.
                if (tracks.Count == 0)
                {
                    Debug.Log("[VideoPlayer] skip re-open cuz there is no sound.");
                    return;
                }
            }

            IsRunning = false;
            videoRender.CopyTempTexture();
            
            // 현재 플레이어 상태 저장.
            double seek = mediaPlayer.Control.GetCurrentTime();
            bool playing = mediaPlayer.Control.IsPlaying();
            mediaPlayer.CloseMedia();

            // 이전 상태 복원.
            mediaPlayer.OpenMedia(mediaPathType, URL, false);

            if (IsLive == false)
            {
                mediaPlayer.Control.Seek(seek);
            }
            if (playing)
            {
                mediaPlayer.Control.Play();
            }
        }

        public void Load(string url, bool isLoop)
        {
            if (!mediaPlayer.enabled)
            {
                mediaPlayer.enabled = true;
            }

            URL = url;
            IsLoop = isLoop;

            mediaPlayer.Loop = isLoop;

            mediaPathType = url.Contains("http") ? MediaPathType.AbsolutePathOrURL : MediaPathType.RelativeToPersistentDataFolder;
            mediaPlayer.OpenMedia(mediaPathType, url, false);
        }

        public async void LoadAsync(string url, bool isLoop)
        {
            if (!mediaPlayer.enabled)
            {
                mediaPlayer.enabled = true;
            }

            URL = url;
            IsLoop = isLoop;

            mediaPlayer.Loop = isLoop;

            // 캐시 활성화.
            if (IsCache == true)
            {
                float progress = 0f;
                var status = mediaPlayer.Cache.GetCachedMediaStatus(URL, ref progress);
                Debug.Log($"[VideoPlayer] Cache State {status} : {progress}");

                if (progress <= 0f)
                {
                    Debug.Log($"[VideoPlayer] Try wait for cache progress over the 0.");
                    while (progress <= 0f)
                    {
                        await Task.Yield();

                        status = mediaPlayer.Cache.GetCachedMediaStatus(URL, ref progress);
                        Debug.Log($"[VideoPlayer] Cache State {status} : {progress}");
                    }
                }
            }

            mediaPathType = url.Contains("http") ? MediaPathType.AbsolutePathOrURL : MediaPathType.RelativeToPersistentDataFolder;
            mediaPlayer.OpenMedia(mediaPathType, url, false);
        }

        /// <summary>
        /// 영상 재생 설정 옵션 변경
        /// </summary>
        /// <param name="transparency"></param>
        /// <param name="alphaPacking"></param>
        /// <param name="stereoPacking"></param>
        public void SetMediaHints(TransparencyMode transparency = TransparencyMode.Opaque, AlphaPacking alphaPacking = AlphaPacking.None, StereoPacking stereoPacking = StereoPacking.None)
        {
            if (mediaPlayer != null)
            {
                MediaHints mediaHints = new MediaHints();
                mediaHints.transparency = (RenderHeads.Media.AVProVideo.TransparencyMode)transparency;
                mediaHints.alphaPacking = (RenderHeads.Media.AVProVideo.AlphaPacking)alphaPacking;
                mediaHints.stereoPacking = (RenderHeads.Media.AVProVideo.StereoPacking)stereoPacking;

                mediaPlayer.FallbackMediaHints = mediaHints;
            }
        }

        public void CacheVideo(string url, bool isCache, bool storage)
        {
            IsCache = isCache;

            if (storage == false)
            { // 저장 공간 여유 없음.
                // 캐시 일괄 삭제.
                Debug.Log("[VideoCache] Not enough storage space.");
                VideoCache.Cleanup(mediaPlayer.Cache);
            }

            if (isCache == true)
            {
                if (IsLive)
                {
                    Debug.Log("[VideoCache] Live stream skip the cache.");
                }
                else
                {
                    // 저장 정책.
                    Debug.Log("[VideoCache] Cache Video.");
                    VideoCache.Cache(mediaPlayer.Cache, url);
                }
            }
        }

        private void OnLoadedMediaPlayer()
        {
            IsLoaded = true;

            // 영상 load 전 seek 변경이 왔을때,
            // load 이후에 seek 변경을 함.
            if (savedPosition >= 0)
            {
                Seek(savedPosition);

                if (savedPlayState)
                {
                    mediaPlayer.Play();
                }

                savedPlayState = false;
                savedPosition = -1;
            }
        }

        private void ValideMetaData()
        {
            IsRunning = true;
            isMetaReady = true;
            IsLive = double.IsInfinity(mediaPlayer.Info.GetDuration());
        }

        public void Seek(double position, bool skipSeekForLive = false)
        {
            // 미디어 정보 불러오기 전 재생하지 못하도록.
            if (IsLoaded)
            {
                if (IsLive)
                {
                    // BG-FG 전환 시 Seek 하지 않도록 수정.
                    if (!skipSeekForLive)
                    {
                        //TimeRange seekableRange = Helper.GetTimelineRange(mediaPlayer.Info.GetDuration(), mediaPlayer.Control.GetSeekableTimes());
                        mediaPlayer.SeekToLiveTime(0);

                        Debug.Log("[VideoPlayer] Live video seek.");
                    }
                    else
                    {
                        Debug.Log("[VideoPlayer] Live video skip seek.");
                    }
                }
                else
                {
                    double mediaLength = mediaPlayer.Info.GetDuration();
                    if (mediaPlayer.Loop)
                    {
                        // loop 상태일때, 계속 재생하도록.
                        position = position % mediaLength;
                    }
                    else
                    {
                        // 영상 재생이 끝났을 경우, seek를 영상의 마지막으로.
                        if (position >= mediaLength)
                        {
                            position = 0;
                        }
                    }

                    mediaPlayer.Control.Seek(position);
                }
            }
            else
            {
                // 영상 load 전 seek 변경이 왔을 때 값 저장 후 load 이후에 seek.
                savedPosition = position;
            }
        }

        public void Play(double position, bool skipSeekForLive)
        {
            // 미디어 정보 불러오기 전 재생하지 못하도록.
            if (IsLoaded)
            {
                Seek(position, skipSeekForLive);

                if (IsLive)
                {
                    mediaPlayer.Play();
                }
                else
                {
                    if (IsCanPlay(position))
                    {
                        mediaPlayer.Play();
                    }
                    else
                    {
                        mediaPlayer.Pause();
                    }
                }
            }
            else
            {
                // 영상 load 전 seek 변경이 왔을 때 값 저장 후 load 이후에 seek.
                savedPosition = position;
                savedPlayState = true;
            }
        }

        public void ForcePlay()
        {
            mediaPlayer.Play();
        }

        public void Pause(double position = -1)
        {
            if (!IsLive)
            {
                if (position >= 0)
                {
                    Seek(position);
                }
            }

            mediaPlayer.Pause();
        }

        public void Mute(bool enable)
        {
            mediaPlayer.AudioMuted = enable;
        }

        public void SetVolume(float volume)
        {
            mediaPlayer.AudioVolume = volume * 0.01f;
        }

        public double GetPosition()
        {
            double position = mediaPlayer.Control.GetCurrentTime();
            return position;
        }

        public double GetCurrentTime()
        {
            if (!isMetaReady)
                return 0;

            if (IsLive)
                return -1;

            return mediaPlayer.Control.GetCurrentTime();
        }

        public double GetVideoLength()
        {
            if (!isMetaReady)
                return 0;

            if (IsLive)
            {
                return 0;
            }
            else
            {
                // Live 영상 구분하는 값이 늦게 업데이트되는 이슈 발생.
                if (double.IsInfinity(mediaPlayer.Info.GetDuration()))
                {
                    IsLive = true;
                    return 0;
                }
            }

            return mediaPlayer.Info.GetDuration();
        }

        public Vector2 GetBufferedTime()
        {
            var bufferedTimes = mediaPlayer.Control.GetBufferedTimes();
            return new Vector2((float)bufferedTimes.MinTime, (float)bufferedTimes.MaxTime);
        }

        /// <summary>
        /// 재생 가능한 상태인지 확인.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool IsCanPlay(double position)
        {
            if (IsLoaded)
            {
                if (!IsLive)
                {
                    if (!mediaPlayer.Loop)
                    {
                        // 영상 재생이 끝났을 경우, 재생 금지.
                        if (position >= mediaPlayer.Info.GetDuration())
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task WaitForLoadPlayer()
        {
            while (isMetaReady == false)
            {
                await Task.Yield();
            }
        }

        private void OnMediaPlayerEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, RenderHeads.Media.AVProVideo.ErrorCode errorCode)
        {
            Debug.Log($"[VideoPlayer] EventType: {et} / Error: {errorCode}");
            switch (et)
            {
                case MediaPlayerEvent.EventType.MetaDataReady:
                case MediaPlayerEvent.EventType.ReadyToPlay:
                    ValideMetaData();
                    break;

                case MediaPlayerEvent.EventType.FirstFrameReady:
                    ValideMetaData();
                    OnLoadedMediaPlayer();
                    break;

                case MediaPlayerEvent.EventType.StartedSeeking:
                    break;

                case MediaPlayerEvent.EventType.Closing:
                    videoRender.ApplyTempTexture();
                    break;
            }

            handler?.OnMediaPlayerEvent((EventType)et, (ErrorCode)errorCode);
        }
    }
}