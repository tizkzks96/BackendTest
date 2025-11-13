using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace  UCF.Media.Service
{
    internal sealed class MediaDebug : MonoBehaviour
    {
        private struct DebugCommand
        {
            public string Service { get; private set; }
            public Dictionary<string, string> Properties { get; private set; }

            public static DebugCommand? Parse(string str)
            {
                if (string.IsNullOrEmpty(str))
                    return null;

                string command = str.ToLower();
                if (command.Contains("ucf") == false)
                    return null;

                DebugCommand debugCommand = new DebugCommand();
                debugCommand.Properties = new Dictionary<string, string>();

                command = command.Replace("ucf ", "");
                string[] properties = command.Split(' ');
                foreach (string property in properties)
                {
                    string[] key_value = property.Split('=');

                    if (key_value.Length != 2)
                    {
                        Debug.Log($"[DebugCommand] Parse error. {property}");
                        continue;
                    }

                    if (key_value[0] == "service")
                    {
                        debugCommand.Service = key_value[1];
                        continue;
                    }

                    debugCommand.Properties.Add(key_value[0], key_value[1]);
                }

                return debugCommand;
            }
        }

        #region UI components
        [Header("Common")]
        public Text providerText;
        public Text displayModeText;
        public Text shaderText;
        public Text resolutionText; // 1280x720 0 degree
        public Text isCallingText;
        public Text isInterruptText;
        public Text pauseByCallingText;
        public Text pauseByInterruptText;

        [Header("Document")]
        public GameObject documentPanel;
        public Text dp_LoadedText;
        public Text dp_LocalLoadText;
        public Text dp_CurrentPathText;
        public Text dp_CurrentIndexText;

        [Header("Video")]
        public GameObject videoPanel;
        public Button[] vp_EventButtons;
        public Text vp_PathText;
        public Text vp_LiveText;
        public Text vp_LoopText;
        public Text vp_TimeText;
        public Text vp_LengthText;
        public Text vp_BufferedTimeText;
        public Text vp_VolumeText;

        [Header("Share")]
        public GameObject sharePanel;
        public Text sp_PositionText;
        public Text sp_LengthText;
        public Text sp_TextureUpdateText;
        public Text sp_WaitTimeText;
        #endregion

        private Canvas canvas;
        private MediaProvider mediaProvider;
        private bool isEnable = false;

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            canvas.enabled = false;

            ApplyCommand(GUIUtility.systemCopyBuffer);
        }

        private void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                ApplyCommand(GUIUtility.systemCopyBuffer);
            }
        }

        /// <summary>
        /// command: ucf service=media debug=true
        /// </summary>
        /// <param name="str"></param>
        private void ApplyCommand(string str)
        {
            DebugCommand? cmd = DebugCommand.Parse(str);
            if (cmd != null)
            {
                if (cmd.Value.Service == "media")
                {
                    if (cmd.Value.Properties.TryGetValue("debug", out string value))
                    {
                        Enable(bool.Parse(value));
                    }
                }
            }
        }

        public void Enable(bool enable)
        {
            isEnable = enable;
            canvas.enabled = enable;
        }

        public void Initialize(MediaProvider p)
        {
            mediaProvider = p;
        }

        private void LateUpdate()
        {
            if (!isEnable)
                return;

            if (mediaProvider == null)
                return;

            DrawCommonInfo();
            DrawProviderInfo();
        }

        private void DrawCommonInfo()
        {
            providerText.text = mediaProvider.provider == null ? "None" : mediaProvider.provider.name;

            if (mediaProvider.landData != null)
            {
                isCallingText.text = mediaProvider.landData.isCalling.ToString();
                isInterruptText.text = mediaProvider.landData.isInterrupt.ToString();
                pauseByCallingText.text = mediaProvider.landData.pauseByCalling.ToString();
                pauseByInterruptText.text = mediaProvider.landData.pauseByInterrupt.ToString();
            }
        }

        private void DrawProviderInfo()
        {
            documentPanel.SetActive(false);
            videoPanel.SetActive(false);
            sharePanel.SetActive(false);

            if (mediaProvider.provider == null)
                return;

            var type = mediaProvider.provider.GetType();

            if (type == typeof(DocumentProvider))
            {
                DrawDocumentProvider(mediaProvider.provider as DocumentProvider);
            }
            else if (type == typeof(VideoProvider))
            {
                DrawVideoProvider(mediaProvider.provider as VideoProvider);
            }
            else if (type == typeof(ShareProvider))
            {
                DrawShareProvider(mediaProvider.provider as ShareProvider);
            }
        }

        private void DrawDocumentProvider(DocumentProvider provider)
        {
            if (provider.player == null)
                return;

            documentPanel.SetActive(true);

            dp_LoadedText.text = provider.player.isLoad.ToString();
            dp_LocalLoadText.text = provider.isLocal.ToString();
            dp_CurrentPathText.text = provider.currentPath;
            dp_CurrentIndexText.text = provider.currentIndex.ToString();
        }

        private void DrawVideoProvider(VideoProvider provider)
        {
            //if (provider.player == null || provider.player.mediaPlayer == null)
            //    return;

            //var mediaPlayer = provider.player.mediaPlayer;
            //if (mediaPlayer.Control != null && mediaPlayer.Info != null)
            //{

            //    videoPanel.SetActive(true);

            //    // events
            //    vp_EventButtons[0].interactable = mediaPlayer.Control.HasMetaData();
            //    vp_EventButtons[1].interactable = mediaPlayer.Control.IsPaused();
            //    vp_EventButtons[2].interactable = mediaPlayer.Control.IsPlaying();
            //    vp_EventButtons[3].interactable = mediaPlayer.Control.IsSeeking();
            //    vp_EventButtons[4].interactable = mediaPlayer.Control.IsBuffering();
            //    vp_EventButtons[5].interactable = mediaPlayer.Info.IsPlaybackStalled();
            //    vp_EventButtons[6].interactable = mediaPlayer.Control.IsFinished();

            //    vp_PathText.text = provider.currentPath;
            //    vp_LiveText.text = provider.player.IsLive.ToString();
            //    vp_LoopText.text = provider.player.IsLoop.ToString();
            //    vp_TimeText.text = mediaPlayer.Control.GetCurrentTime().ToString();
            //    vp_LengthText.text = mediaPlayer.Info.GetDuration().ToString();

            //    var times = mediaPlayer.Control.GetBufferedTimes();
            //    vp_BufferedTimeText.text = $"{times.MinTime} - {times.MaxTime}";

            //    vp_VolumeText.text = mediaPlayer.Control.GetVolume().ToString();
            //}
        }

        private void DrawShareProvider(ShareProvider provider)
        {
            if (provider.player == null)
                return;

            sharePanel.SetActive(true);

            //sp_PositionText.text = provider.player.playTime.ToString();
            //sp_LengthText.text = provider.player.maxTime.ToString();
            sp_TextureUpdateText.text = provider.player.isUpdate.ToString();
            sp_WaitTimeText.text = provider.waitTime.ToString();
        }
    }
}