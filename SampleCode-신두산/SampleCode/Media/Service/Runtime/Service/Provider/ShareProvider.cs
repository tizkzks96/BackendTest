using System;
using UnityEngine;

namespace  UCF.Media.Service
{
    using Module;

    internal sealed class ShareProvider : BaseProvider
    {
        private const float MIN_WAIT_TIME = 5f;

        internal MRLisSharePlayer player;
        private ShareRenderer shareRenderer;

        private IMediaModuleEventHandler eventHandler;
        private IMediaModuleRenderEventHandler renderEventHandler;

        private string filePath = string.Empty;
        private ePresentationType type;
        private ePlayerState playerState;
        private bool isPresenter;

        private bool isStalled;
        internal float waitTime;

        private Texture2D defaultTexture1;
        private Texture2D defaultTexture2;
        private Texture2D defaultTexture3;

        public override void Initialize(IMediaModuleEventHandler handler, Material material)
        {
            shareRenderer = new ShareRenderer();
            player = new MRLisSharePlayer(transform, shareRenderer);

            eventHandler = handler;
            renderEventHandler = shareRenderer;
        }

        public override void Dispose()
        {
            shareRenderer.Dispose();

            if (defaultTexture1)
                Destroy(defaultTexture1);

            if (defaultTexture2)
                Destroy(defaultTexture2);

            if (defaultTexture3)
                Destroy(defaultTexture3);

            Destroy(this.gameObject);
        }

        public override bool IsLoadProvider()
        {
            return true;
        }

        public override void Load(string path, Action<Enum> onResponse)
        {
            filePath = path;
            isStalled = false;
            waitTime = 0f;

            onResponse.Invoke(null);
        }

        public override void UpdateState(MediaUser userInfo, LandMediaData mediaData)
        {
            type = (ePresentationType)mediaData.presentationType;
            playerState = (ePlayerState)mediaData.state;
            isPresenter = userInfo.IsPresenter;

#if UNITY_ANDROID && UNITY_IOS
            // 모바일 환경에서만 로직 수행하도록.
            if (isPresenter)
            {
                if (MediaService.IsMainThread)
                {
                    // 발표자가 화면 공유가 시작되면 화면에 검정색으로 출력하도록 수정.
                    if (type == ePresentationType.MRLis_Screen)
                    {
                        EnsureDefaultTexture();
                        renderEventHandler.OnChangeResolution(1, 1, 0, defaultTexture1, defaultTexture2, defaultTexture3);
                    }
                }
            }
#endif
        }

        public override void WaitLoadMedia(MediaUser userInfo, LandMediaData data, Action<Enum> onResponse)
        {
            onResponse?.Invoke(null);
        }

        public override void UpdateMuteState(MediaUser userData, LandMediaData mediaData)
        {
        }

        public override int GetPrevPosition(bool isFirst)
        {
            int currPosition = (int)player.GetCurrentTime();
            int prevPosition = currPosition - 1;
            if (prevPosition < 0)
            {
                return currPosition;
            }

            return prevPosition;
        }

        public override int GetNextPosition(bool isLast)
        {
            int currPosition = (int)player.GetCurrentTime();
            int nextPosition = currPosition + 1;
            if (nextPosition > (int)player.GetVideoLength())
            {
                return currPosition;
            }

            return nextPosition;
        }

        public override int GetPosition()
        {
            return (int)player.GetCurrentTime();
        }

        public override int GetMaxPosition()
        {
            return (int)player.GetVideoLength();
        }

        public override void SetPosition(eDragState dragState, float position)
        {

        }

        private void LateUpdate()
        {
            if (player == null)
                return;

            // 발표자는 해당 로직을 수행하지 않음.
            if (isPresenter == false)
            {
                // 텍스쳐 정보 업데이트 되지 않음.
                if (!player.isUpdate)
                {
                    if (!isStalled) // 지연 상태가 아님.
                    {
                        switch (playerState)
                        {
                            default:
                                waitTime = 0f;
                                break;

                            // 재생 중 일경우.
                            case ePlayerState.Playing:
                                // 지연 시간 체크.
                                waitTime += Time.deltaTime;
                                if (waitTime >= MIN_WAIT_TIME)
                                {
                                    // 지연 상태로 변경.
                                    waitTime = 0f;
                                    isStalled = true;
                                    renderEventHandler.OnPlayerStalled(isStalled);
                                }
                                break;
                        }
                    }
                }
                else
                {
                    waitTime = 0f;

                    // 텍스쳐 정보 업데이트 됨.
                    if (isStalled)
                    {
                        isStalled = false;
                        renderEventHandler.OnPlayerStalled(isStalled);
                    }
                }
            }

            eventHandler?.OnMediaProgress(this, player.GetCurrentTime(), player.GetVideoLength());
        }

        public override bool CheckMediaData(LandMediaData mediaData)
        {
            return filePath == mediaData.url;
        }

        public override MediaMetaData GetMetaData()
        {
            return null;
        }

        public override void UpdateResolution(int width, int height)
        {
        }

        public override bool IsPlayingAudio()
        {
            // 이미지 공유 소리 안남.
            if (type == ePresentationType.Image)
                return false;

            return playerState == ePlayerState.Playing;
        }

        /// <summary>
        /// 검은색의 YUV 텍스쳐 3개를 생성합니다.
        /// </summary>
        private void EnsureDefaultTexture()
        {
            if (defaultTexture1 == null)
            {
                defaultTexture1 = new Texture2D(1, 1, TextureFormat.Alpha8, false);
                defaultTexture2 = new Texture2D(1, 1, TextureFormat.Alpha8, false);
                defaultTexture3 = new Texture2D(1, 1, TextureFormat.Alpha8, false);

                // https://docs.microsoft.com/en-us/windows/win32/medfound/about-yuv-video#yuv-in-computer-video
                defaultTexture1.LoadRawTextureData(new byte[] { 16 });
                defaultTexture2.LoadRawTextureData(new byte[] { 128 });
                defaultTexture3.LoadRawTextureData(new byte[] { 128 });

                defaultTexture1.Apply(false);
                defaultTexture2.Apply(false);
                defaultTexture3.Apply(false);
            }
        }
    }
}