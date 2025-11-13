using System;
using UnityEngine;

namespace  UCF.Media.Module
{
    using MRLisCore;

    public class MRLisSharePlayer : IDisposable, IMRLisRTCPlayerHandler, IMRLisEventHander
    {
        private IMediaModuleRenderEventHandler handler;

        private MRLisRTCPlayer rtcPlayer;
        internal long playTime = 0;
        internal long maxTime = 0;

        public bool isUpdate
        {
            get
            {
                if (rtcPlayer)
                    return rtcPlayer.isUpdate;
                return false;
            }
        }

        public MRLisSharePlayer(Transform transform, IMediaModuleRenderEventHandler handler)
        {
            this.handler = handler;
            MRLisCore.Instance.RegisterEventHandler(this);

            rtcPlayer = CreatePlayer(transform);
            rtcPlayer.RegisterHandler(this);
        }

        public void Dispose()
        {
            MRLisCore.Instance?.UnregisterEventHandler(this);
        }

        private MRLisRTCPlayer CreatePlayer(Transform transform)
        {
            MRLisRTCPlayer player = transform.gameObject.AddComponent<MRLisRTCPlayer>();
            return player;
        }

        public double GetCurrentTime() => playTime;

        public double GetVideoLength() => maxTime;

        void IMRLisRTCPlayerHandler.OnUpdateTextures(Texture2D texture1, Texture2D texture2, Texture2D texture3, int width, int height, int rotation)
        {
            handler.OnChangeResolution(width, height, rotation, texture1, texture2, texture3);
        }

        void IMRLisEventHander.UpdateMediaState(long playTime, long maxTime)
        {
            this.playTime = playTime;
            this.maxTime = maxTime;
        }

        void IMRLisEventHander.UpdateLipsync(int uid, int volume) { }
    }
}