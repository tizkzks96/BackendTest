using System;
using System.Collections;
using UnityEngine.Networking;

namespace  UCF.Media.Service
{
    using Module;
    using UnityEngine;

    internal sealed class DocumentProvider : BaseProvider
    {
        private Action<Enum> onLoadComplete;

        internal PDFPlayer player;
        internal DocumentRenderer docRenderer;

        internal bool isLocal = false;
        internal string currentPath = string.Empty;
        internal int currentIndex = 0;

        private bool isLoadComplete = false;

        public override void Initialize(IMediaModuleEventHandler handler, Material material)
        {
            docRenderer = new DocumentRenderer();
            player = new PDFPlayer(docRenderer);
        }

        public override void Dispose()
        {
            docRenderer.Dispose();
            player.Dispose();

            Destroy(this.gameObject);
        }

        public override bool IsLoadProvider()
        {
            return isLoadComplete;
        }

        public override void Load(string path, Action<Enum> onResponse)
        {
            onLoadComplete = onResponse;

            if (!currentPath.Equals(path))
            {
                currentPath = path;
                if (ValidateHTTPURL(path))
                {
                    isLocal = false;
                    StartCoroutine(DownloadFile(path, OnDownloadFile));
                }
                else
                {
                    isLocal = true;
                    LoadLocalFile(path);
                }
            }
            else
            {
                onLoadComplete?.Invoke(ePresentation.NOT_EXIST_URL);
                onLoadComplete = null;
            }
        }

        private IEnumerator DownloadFile(string path, Action<bool, byte[]> onDownload)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(path))
            {
                yield return req.SendWebRequest();

                bool success = false;
                byte[] rawData = null;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    success = true;
                    rawData = req.downloadHandler.data;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Download failed. path:{path} reason: {req.result}");
                }

                onDownload?.Invoke(success, rawData);
            }
        }

        private void OnDownloadFile(bool success, byte[] rawData)
        {
            if (success)
            {
                if (player.Load(rawData) == 0)
                {
                    player.Seek(currentIndex);
                }

                isLoadComplete = true;
                onLoadComplete?.Invoke(null);
                onLoadComplete = null;
            }
            else
            {
                // log error.
                onLoadComplete?.Invoke(ePresentation.FAILED_INITIALIZE);
                onLoadComplete = null;
            }
        }

        public override void WaitLoadMedia(MediaUser userInfo, LandMediaData data, Action<Enum> onResponse)
        {
            if (isLoadComplete)
            {
                onResponse?.Invoke(null);
            }
            else
            {
                onLoadComplete += onResponse;
            }
        }

        private void LoadLocalFile(string path)
        {
            if (player.Load(path) == 0)
            {
                player.Seek(currentIndex);
            }
            isLoadComplete = true;
            onLoadComplete?.Invoke(null);
            onLoadComplete = null;
        }

        public override void UpdateState(MediaUser userInfo, LandMediaData mediaData)
        {
            if (isLocal)
            {
                // 예외처리.
                // 이전에 등록했던 자료가 local 등록이었다면,
                // 상태 변경 때, remote path로 변경.
                if (ValidateHTTPURL(mediaData.url))
                {
                    currentPath = mediaData.url;
                    isLocal = false;
                }
            }

            if (currentIndex != mediaData.position)
            {
                currentIndex = mediaData.position;
                player.Seek(mediaData.position);
            }
        }

        public override void UpdateMuteState(MediaUser userData, LandMediaData mediaData)
        {
        }

        public override int GetPrevPosition(bool isFirst)
        {
            int targetPosition = 0;
            if (isFirst == false)
            {
                if (currentIndex > 0)
                    targetPosition = currentIndex - 1;
            }
            return targetPosition;
        }

        public override int GetNextPosition(bool isLast)
        {
            int lastPosition = player.GetPageCount() - 1;
            int targetPosition = lastPosition;

            if (isLast == false)
            {
                if (currentIndex < lastPosition)
                {
                    targetPosition = currentIndex + 1;
                }
            }
            return targetPosition;
        }

        public override int GetPosition()
        {
            return currentIndex;
        }

        public override int GetMaxPosition()
        {
            return player.GetPageCount() - 1;
        }

        public override void SetPosition(eDragState dragState, float position)
        {

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

            // 예외처리.
            // Local 등록 직후 RegisterPresentation이 오기 때문에,
            // 동일한 파일로 간주. true.
            if (isLocal)
            {
                return true;
            }

            return currentPath == mediaData.url;
        }

        private bool ValidateHTTPURL(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        public override MediaMetaData GetMetaData()
        {
            return null;
        }

        public override void UpdateResolution(int width, int height)
        {
            player.UpdateResolution(width, height);
        }

        public override bool IsPlayingAudio()
        {
            return false;
        }
    }
}