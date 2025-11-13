using  UCF.Core.Helper.Telepresence;

namespace  UCF.Media.Service
{
    using Core.Bridge;

    public sealed class LandMediaData : ilInfo
    {
        public LandMediaData(int meetupNo) : base(meetupNo)
        {
            this.meetupNo = meetupNo;
        }
        public int screenPropID;
        public CommonEnum.CommonConferenceURLFormat presentationType;
        public string url;
        public int position;
        public CommonEnum.CommonConferenceMediaStatus state;
        public int isMediaMute;

        #region MediaService 내부에서만 사용.
        public CommonEnum.CommonConferenceMediaStatus savedStateForSeek; // seeking 이전의 재생 상태.

        public bool isLocalControl; // local control 여부.

        public bool isCalling = false; // 통화 중 여부.
        public bool pauseByCalling = false; // 통화에 의해 pause된 이력 존재 여부.

        public bool isInterrupt = false; // Audio interrupt 여부.
        public bool pauseByInterrupt = false;

        public bool isSnapshot = false; // TotalSnapshot에서 호출 됐는지 여부. (BG->FG에 의해 발생한 이벤트인지 여부).
        #endregion

        public void Clear()
        {
            state = 0;
            position = 0;
        }

        public void SetCallState(int state)
        {
#if UNITY_ANDROID
            switch ((eCallStateAndroid)state)
            {
                case eCallStateAndroid.Idle:
                    isCalling = false;
                    break;

                case eCallStateAndroid.Ringing:
                case eCallStateAndroid.Offhook:
                    isCalling = true;
                    break;
            }
#elif UNITY_IOS
            isCalling = (eCallStateiOS)state == eCallStateiOS.Began;
#endif
        }

        public void SetInterruptState(int state)
        {
#if UNITY_ANDROID
            switch ((eCallStateAndroid)state)
            {
                case eCallStateAndroid.Idle:
                    isInterrupt = false;
                    break;

                case eCallStateAndroid.Ringing:
                case eCallStateAndroid.Offhook:
                    isInterrupt = true;
                    break;
            }
#elif UNITY_IOS
            isInterrupt = (eCallStateiOS)state == eCallStateiOS.Began;
#endif
        }

        /// <summary>
        /// 통화/Audio interrupt 상태 리셋.
        /// </summary>
        public void ClearCallAndInterrupt()
        {
            // iOS만 리셋.
#if UNITY_IOS
            isCalling = false;
            isInterrupt = false;

            pauseByCalling = false;
            pauseByInterrupt = false;
#endif
        }
    }
}