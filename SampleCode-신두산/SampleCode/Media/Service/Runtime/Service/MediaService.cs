using System.Threading;
using UnityEngine;

namespace  UCF.Media.Service
{
    using Core.Bridge;
    using Telepresence;

    public partial class MediaService : MonoBehaviour
    {
        public static MediaService Instance { get; private set; }

        #region DEBUG
        [SerializeField]
        private UnityEngine.Object debugPrefab;
        private MediaDebug mediaDebug;
        #endregion

        private MediaProvider mediaProvider;

        /// <summary>
        /// 현재 쓰레드가 유니티 쓰레드인가?.
        /// </summary>
        private static Thread mainThread = null;
        internal static bool IsMainThread
        {
            get
            {
                return Thread.CurrentThread == mainThread;
            }
        }

        private void Awake()
        {
            Instance = this;
            mediaProvider = new MediaProvider();

            mainThread = Thread.CurrentThread;

            // Load shaders.
            CachedShader.Load();

            // UCF Log on.
            //if (Debug.unityLogger.logEnabled)
            //    CreateDebugObject();
        }

        public void OnDisconnectedServer()
        {
            mediaProvider.OnDisconnectedServer();
        }

        //private void CreateDebugObject()
        //{
        //    if (!mediaDebug)
        //    {
        //        GameObject go = Instantiate(debugPrefab, transform) as GameObject;
        //        go.TryGetComponent(out mediaDebug);
        //        mediaDebug?.Initialize(mediaProvider);
        //    }
        //}

        //public void EnableDebug(bool enable)
        //{
        //    if (enable)
        //        CreateDebugObject();

        //    if (mediaDebug)
        //        mediaDebug.Enable(enable);
        //}

        private void OnDestroy()
        {
            mediaProvider.Dispose();
        }

        /// <summary>
        /// 미디어가 실행 중인지?.
        /// </summary>
        /// <returns></returns>
        public bool IsPlaying()
        {
            return mediaProvider.provider;
        }

        public void UpdateConfig(GlobalSettingData.UcfConfigData config, StorageInfo storageInfo)
        {
            mediaProvider.UpdateConfig(config, storageInfo);
        }

        public void SetDataContainers(ilUserDataContainer.ilUserDataTable table, LandMediaData data)
        {
            mediaProvider.SetDataContainers(table, data);
        }

        public void Restore()
        {
            mediaProvider.Dispose();
            mediaProvider = new MediaProvider();
        }

        public void RegisterMediaEventHander(IMediaEventHandler handler)
        {
            mediaProvider.RegisterMediaEventHander(handler);
        }

        public void SyncMuteState(ilUser userInfo, ilInfo landInfo)
        {
            mediaProvider.SyncMuteState(userInfo as MediaUser, landInfo as LandMediaData);
        }

        #region Data sync
        public void UpdateLandInfo(LandMediaData landInfo, CommonRoomBaseInfo roombaseInfo)
        {
            landInfo.presentationType = roombaseInfo.urlFormat; //
            landInfo.url = roombaseInfo.urlPostReference;
            landInfo.isMediaMute = roombaseInfo.isMute;

            switch ((ePresentationType)landInfo.presentationType)
            {
                case ePresentationType.PDF:
                    landInfo.position = roombaseInfo.pdfPage;
                    break;

                case ePresentationType.Media:
                    landInfo.state = roombaseInfo.mediaStatus;
                    landInfo.position = roombaseInfo.mediaSeek;
                    break;

                case ePresentationType.MRLis_Media:
                case ePresentationType.MRLis_Image:
                case ePresentationType.MRLis_Screen:
                    landInfo.state = roombaseInfo.mrlisMediaStatus;
                    break;
            }
        }

        public void UpdateLandInfo(LandMediaData landInfo, InitializationAck data)
        {
            landInfo.presentationType = data.urlFormat;
            landInfo.url = data.url;
            landInfo.isMediaMute = data.isMute;

            switch ((ePresentationType)landInfo.presentationType)
            {
                case ePresentationType.PDF:
                    landInfo.position = data.pdfPage;
                    break;

                case ePresentationType.Media:
                    landInfo.state = data.mediaStatus;
                    landInfo.position = data.mediaSeek;
                    break;

                case ePresentationType.MRLis_Media:
                case ePresentationType.MRLis_Image:
                case ePresentationType.MRLis_Screen:
                    landInfo.state = data.mrlisMediaStatus;
                    break;
            }
        }
        #endregion
    }
}