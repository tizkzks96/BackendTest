using  UCF.Core.Helper.Telepresence;
using UnityEngine;
using UnityEngine.UI;

namespace  UCF.Media.Service
{
    using Core.Helper;
    using Core.Bridge;
    using TMPro;

    public sealed class MediaUI : MonoBehaviour
    {
        public static MediaUI Instance { get; private set; }

        private const float UI_HEIGHT_DP = 56f;
        private const float IfmeCast_HEIGHT_DP = 56;    
        private const float SEEKBAR_POSITION_X_DP = 168f;
        private const float SEEKBAR_HANDLER_DEFAULT_SIZE = 66f; // pixel
        private const float SEEKBAR_HANDLER_SIZE_NOR = 16f;     // dp

        private const int HOUR = 3600;

        private Canvas canvas;

        [Header("Seekbar")]
        [SerializeField] private MediaSlider seekbar;
        [SerializeField] private RectTransform handle;
        [SerializeField] private RectTransform timeArea;
        [SerializeField] private TextMeshProUGUI playingTime;
        [SerializeField] private Image liveImage;

        private RectTransform seekbarRectTransform;
        private RectTransform anchorRectTransform;
        private Vector2 timeAreaPosition;
        private Vector2 liveImagePosition;

        private Vector2 timeAreaAdjustPosition;
        private Vector2 liveImageAdjustPosition;

        private int savedScreenDPI;
        private int savedScreenHeight;
        private int savedScreenLeft;
        private float adjustHeight;
        private bool adjustSeekbarPosition;
        private Vector2 seekbarPosition = Vector3.zero;

        // Seekbar Handle Size
        private float adjustHandleSize;
        private Vector3 seekbarHandleScale = Vector3.one;

        private bool isLive;
        private bool isIfmeCast;
        private double prevCurrent;
        private double prevLength;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            TryGetComponent(out canvas);

            seekbar.TryGetComponent(out seekbarRectTransform);
            transform.parent.TryGetComponent(out anchorRectTransform);

            timeAreaPosition = timeArea.anchoredPosition;
            liveImagePosition = liveImage.rectTransform.anchoredPosition;

            timeAreaAdjustPosition = timeAreaPosition;
            liveImageAdjustPosition = liveImagePosition;
        }

        private void OnRectTransformDimensionsChange()
        {
            OnSeekBarCasterMode();
        }

        public void ResetMediaUIData()
        {
            isLive = false;
            isIfmeCast = false;
        }

        public void ShowSeekbar(bool enable, ilUser localUser, ilInfo landInfo)
        {
            canvas.enabled = enable;

            if (enable)
            {
                RefreshMedia(localUser, landInfo);
            }
            else
            {
                seekbar.gameObject.SetActive(false);
            }
        }

        public void SetScreenSize(int screenDpi, int screenHeight, int screenLeft)
        {
            // 값 변경이 있을때만 업데이트.
            if (savedScreenDPI != screenDpi || savedScreenHeight != screenHeight)
            {
                savedScreenDPI = screenDpi;
                savedScreenHeight = screenHeight;
                OnSeekBarCasterMode();
            }

            // 값 변경이 있을때만 업데이트.
            if (savedScreenLeft != screenLeft)
            {
                timeAreaAdjustPosition.x = timeAreaPosition.x + screenLeft;
                timeArea.anchoredPosition = timeAreaAdjustPosition;

                liveImageAdjustPosition.x = liveImagePosition.x + screenLeft;
                liveImage.rectTransform.anchoredPosition = liveImageAdjustPosition;

                savedScreenLeft = screenLeft;
            }
        }

        /// <summary>
        /// 발표 자료 크게 보기 화면 갱신
        /// 발표 자료, 호스트, 발표자 변경 시 호출.
        /// </summary>
        /// <param name="localUser"></param>
        /// <param name="landInfo"></param>
        public void RefreshMedia(ilUser localUser, ilInfo landInfo)
        {
            var mediaUser = localUser as MediaUser;
            var mediaData = landInfo as LandMediaData;

            if (mediaUser == null)
            {
                Debug.LogError("mediaUser is null");
                return;
            }
            
            if (mediaData == null)
            {
                Debug.LogError("mediaData is null");
                return;
            }

            bool isShow = CheckDisplaySeekbar(mediaUser, mediaData);
            seekbar.gameObject.SetActive(isShow);

            if (isShow)
            {
                liveImage.enabled = false;

                // seekbar 제어 가능 여부. 발표자만.
                seekbar.interactable = mediaUser.IsPresenter && !mediaData.isCalling && !mediaData.isInterrupt;

                // seekbar 위치 조정.
                CheckSeekbarPosition(mediaUser, mediaData);
            }
        }

        /// <summary>
        /// 발표자료 타입에 따른 재생바 표시 여부
        /// </summary>
        /// <param name="landInfo"></param>
        /// <returns></returns>
        private bool CheckDisplaySeekbar(MediaUser userInfo, LandMediaData landInfo)
        {
            if (gameObject.activeSelf)
            {
                switch ((ePresentationType)landInfo.presentationType)
                {
                    // Media 타입은 항상 보여줌.
                    case ePresentationType.Media:
                        return true;

                    // 로컬 영상 공유 타입은 발표자만 보여줌.
                    case ePresentationType.MRLis_Media:
                        return userInfo.IsPresenter;

                    default:
                        return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void CheckSeekbarPosition(MediaUser userData, LandMediaData landData)
        {
            switch ((ePresentationType)landData.presentationType)
            {
                case ePresentationType.Media:
                    adjustSeekbarPosition = true;
                    break;

                default:
                    // 호스트 또는 발표자 일 경우에만 seekbar 위치 조정.
                    adjustSeekbarPosition = userData.IsMaster || userData.IsPresenter;
                    break;
            }

            //UpdateSeekbarPosition();
            OnSeekBarCasterMode();
        }

        private void UpdateSeekbarPosition()
        {
            if (seekbarRectTransform == null)
                return;

            seekbarPosition.y = adjustHeight;
            seekbarRectTransform.anchoredPosition = seekbarPosition;
        }

        private void UpdateSeekbarScale()
        {
            if (seekbarRectTransform == null)
                return;

            handle.localScale = seekbarHandleScale;
        }

        public void RegisterSliderEvent(UnityEngine.Events.UnityAction<eDragState, float> action)
        {
            seekbar.onValueChanged.RemoveAllListeners();
            seekbar.onValueChanged.AddListener(action);
        }

        /// <summary>
        /// 제스쳐 컨트롤이 가능한 상태인지 여부
        /// </summary>
        /// <param name="landInfo"></param>
        /// <returns></returns>
        public bool CheckSwipeControl(ilUser userInfo, LandMediaData landInfo)
        {
            if (userInfo.IsPresenter == false)
                return false;

            switch ((ePresentationType)landInfo.presentationType)
            {
                case ePresentationType.PDF:
                case ePresentationType.MRLis_Image:
                    return true;

                default:
                    return false;
            }
        }

        public void OnChangedMedia(CommonEnum.CommonConferenceURLFormat type)
        {
            // 줌 리셋.
            //displayMedia.ResetZoom();
        }

        public void OnProgressMedia(double current, double length)
        {
            // 값이 -1이면 Live.
            isLive = current == -1;
            RefreshPlayerInfo();

            if (isLive == false)
            {
                if (liveImage.enabled)
                    liveImage.enabled = false;

                UpdateTimeAreaText(current, length);
                UpdateSlider(current, length);

                prevCurrent = current;
                prevLength = length;
            }
            else
            {
                seekbar.interactable = false;
                UpdateSlider(1, 1);
            }
        }

        /// <summary>
        /// 플레이어 상태 업데이트
        /// </summary>
        private void RefreshPlayerInfo()
        {
            if (isIfmeCast)
            {
                liveImage.enabled = false;
                timeArea.gameObject.SetActive(false);
            }

            if (liveImage.enabled != isLive && !isIfmeCast)
                liveImage.enabled = isLive;

            if (timeArea.gameObject.activeSelf != !isLive && !isIfmeCast)
            {
                timeArea.gameObject.SetActive(!isLive);
            }
        }

        /// <summary>
        /// 시간 표시 업데이트
        /// </summary>
        /// <param name="current"></param>
        /// <param name="length"></param>
        private void UpdateTimeAreaText(double current, double length)
        {
            if (prevCurrent != current || prevLength != length)
            {
                playingTime.text = TimeToString((int)current) + " / " + TimeToString((int)length);
            }
        }

        /// <summary>
        /// Seekbar 업데이트
        /// </summary>
        /// <param name="current"></param>
        /// <param name="length"></param>
        private void UpdateSlider(double current, double length)
        {
            if (seekbar.value != current)
                seekbar.SetValue((float)current);

            if (seekbar.maxValue != length)
                seekbar.maxValue = (float)length;
        }

        /// <summary>
        /// 시간을 문자열로 변환
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        private string TimeToString(int time)
        {
            int min = time / 60;
            int sec = time % 60;

            // 1시간 넘어감.
            if (time >= 3600)
            {
                int hour = time / HOUR;
                min = (time - HOUR * hour) / 60;
                sec = (time - HOUR * hour) % 60;
                return $"{hour:D2}:{min:D2}:{sec:D2}";
            }
            else
            {
                return $"{min:D2}:{sec:D2}";
            }
        }

        public void SetCasterMode(bool enable)
        {
            isIfmeCast = enable;
        }

        private void OnSeekBarCasterMode()
        {
            if (anchorRectTransform == null)
                return;

            adjustHeight = 0;
            adjustHandleSize = 0;

            float canvasWidth = this.GetComponent<RectTransform>().rect.width;
            float anchorWidthScale = anchorRectTransform.anchorMax.x - anchorRectTransform.anchorMin.x;

            if(adjustSeekbarPosition)
            {
                if (isIfmeCast)
                {
                    // canvasWidth * anchorWidthScale - Fold 단말에 대한 예외처리
                    adjustHeight = Utility.ToPixel(canvasWidth, IfmeCast_HEIGHT_DP, Screen.width, savedScreenDPI);
                }
                else
                {
                    adjustHeight = Utility.ToPixel(canvasWidth, UI_HEIGHT_DP, Screen.width, savedScreenDPI);
                }
            }
            else
            {
                adjustHeight = 0;
            }

            adjustHandleSize = Utility.ToPixel(canvasWidth, SEEKBAR_HANDLER_SIZE_NOR, Screen.width, savedScreenDPI);



            adjustHeight /= anchorWidthScale;
            adjustHandleSize /= anchorWidthScale;

            // Handle 사이즈의 절반 크기만큼 높이 보정
            float heightCorrection = adjustHandleSize - SEEKBAR_HANDLER_DEFAULT_SIZE;
            adjustHeight += heightCorrection / 2.0f;

            float handleSizeRate = adjustHandleSize / SEEKBAR_HANDLER_DEFAULT_SIZE;
            seekbarHandleScale = Vector3.one * handleSizeRate;

            UpdateSeekbarPosition();
            UpdateSeekbarScale();
        }
    }
}