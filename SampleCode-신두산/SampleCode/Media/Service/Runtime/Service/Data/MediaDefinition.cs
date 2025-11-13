using System;
using  UCF.Core.Helper.Telepresence;

namespace  UCF.Media.Service
{
    internal enum ePresentation
    {
        SUCCESS = 0,

        /// <summary>
        /// 발표자가 아님.
        /// </summary>
        NOT_PRESENTER = 1,
        
        /// <summary>
        /// 등록된 발표자료가 없음.
        /// </summary>
        NOT_EXIST_PRESENTATION = 2,
        
        /// <summary>
        /// 발표자료 초기화 실패.
        /// </summary>
        FAILED_INITIALIZE = 3,

        /// <summary>
        /// 발표자료 URL이 없음
        /// </summary>
        NOT_EXIST_URL = 4,

        /// <summary>
        /// 잘못된 발표자료 타입
        /// </summary>
        INVALID_PRESENTATION = 5,

        /// <summary>
        /// 메인 쓰레드가 아닌 경우
        /// </summary>
        NOT_MAIN_THREAD = 6,
                
        /// <summary>
        /// 발표자료 등록 실패
        /// </summary>
        FAILED_REGISTER = 7,
    }

    internal enum ePresentationType
    {
        PDF = 0,
        Media = 1,
        MRLis_Media = 2,
        MRLis_Image = 3,
        MRLis_Screen = 4,
        Image = 10,
    }

    internal enum ePlayerState
    {
        None = 0,
        Playing = 1,
        Pause = 2,
        Stop = 3
    }

    internal enum eCallStateiOS
    {
        Ended = 0,
        Began = 1
    }

    internal enum eCallStateAndroid
    {
        Idle = 0,
        Ringing = 1,
        Offhook = 2
    }

    public enum eDragState
    {
        START = 0,
        DRAGGING = 1,
        END = 2,
    }

    internal enum eMicState
    {
        UNKNOWN = 0,
        MUTE_ON = 1,
        MUTE_OFF = 2
    }

    internal class MediaDefinition
    {
        internal const string LOOP = "#loop";
        internal const string NO_CACHE = "#no-cache";

        [Serializable]
        public struct RegisterPresentation
        {
            public string filePath;
            public CommonEnum.CommonConferenceURLFormat type;
        }

        [Serializable]
        public struct RegisterLocalPresentation
        {
            public string filePath;
            public CommonEnum.CommonConferenceURLFormat type;
        }

        [Serializable]
        public struct InitializePresentation
        {
            public CommonEnum.CommonConferenceURLFormat type;
            public CommonEnum.CommonConferenceMediaStatus state;
            public int current;
            public int max;
            public string filePath;
            public bool mute;
            public bool isLive;
        }

        [Serializable]
        public struct MuteVideo
        {
            public bool mute;
        }

        [Serializable]
        public struct MuteLocalMedia
        {
            public bool isMute;
        }

        [Serializable]
        public struct CallState
        {
            public int state;
        }

        [Serializable]
        public struct AudioInterruptState
        {
            public int state;
        }

        [Serializable]
        public struct CurrentVideoPosition
        {
            public int current;
        }

        [Serializable]
        public struct ChangeVideoState
        {
            public CommonEnum.CommonConferenceMediaStatus state;
        }

        [Serializable]
        public struct ChangePDFState
        {
            public CommonEnum.CommonConferenceURLFormat type;
            public int current;
            public int max;
        }

        [Serializable]
        public struct ChangeMRLisState
        {
            public CommonEnum.CommonConferenceMediaStatus state;
        }

        [Serializable]
        public class SeekbarUpdate
        {
            public int state; // 0: 드래그 시작// 1:드래그 // 2: 드래그 종료
            public long position;
        }

        [Serializable]
        public struct AVProEvent
        {
            public int eventCode;
            public string eventMsg;
            public int errorCode;
            public string errorMsg;
            public Details details;

            [Serializable]
            public struct Details
            {
                public bool playing;
                public double length;
                public double current;
                public double bufferedTimeMin;
                public double bufferedTimeMax;
            }
        }
    }
}