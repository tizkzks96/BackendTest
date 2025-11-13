using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace  UCF.Media.Service
{
    using Module;
    using Core.Helper;

    internal sealed class ImageProvider : BaseProvider
    {
        private ImageRenderer imageRenderer;
        private IMediaModuleRenderEventHandler renderEventHandler;

        private Action<Enum> onLoadComplete;
        private bool isLoadComplete = false;

        private Material targetMaterial;
        private Texture2D texture2D;

        public override void Initialize(IMediaModuleEventHandler handler, Material material = null)
        {
            if (material == null)
            {
                imageRenderer = new ImageRenderer();
                renderEventHandler = imageRenderer;
            }
            else
            {
                targetMaterial = material;
            }
        }

        public override void Dispose()
        {
            imageRenderer?.Dispose();

            StartCoroutine(DestroyObject());
        }

        private IEnumerator DestroyObject()
        {
            // 다운로드 중이라면, 끝날때 까지 대기.
            yield return new WaitUntil(() => isLoadComplete);

            Destroy(this.gameObject);
        }

        private void OnDestroy()
        {
            if (texture2D != null)
            {
                Destroy(texture2D);
                texture2D = null;
            }

            targetMaterial = null;
        }

        public override bool IsLoadProvider()
        {
            return isLoadComplete;
        }

        public override void Load(string path, Action<Enum> onResponse)
        {
            onLoadComplete = onResponse;

            StartCoroutine(DownloadFile(path, 0, 0, OnDownloadFile));
        }

        public void LoadExtension(string path, int width, int height, Action<Enum> onResponse)
        {
            onLoadComplete = onResponse;

            StartCoroutine(DownloadFile(path, width, height, OnDownloadFile));
        }

        private IEnumerator DownloadFile(string path, int width, int height, Action<bool, int, int> onDownload)
        {
            bool success;
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(path))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    success = true;

                    // 기존 텍스쳐 삭제.
                    if (texture2D != null)
                    {
                        Destroy(texture2D);
                        texture2D = null;
                    }

                    texture2D = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;

                    if (texture2D != null)
                    {
                        texture2D.name = $"[MediaService.ImageProvider] Origin {texture2D.width}x{texture2D.height}";
                    }
                    else
                    {
                        success = false;
                        Debug.LogError("Download success. but Texture2D is null.");
                    }
                }
                else
                {
                    success = false;
                    Debug.LogError($"Download failed. path:{path} reason: {webRequest.result}");
                }
            }

            onDownload?.Invoke(success, width, height);
        }

        private void OnDownloadFile(bool success, int width, int height)
        {
            if (success)
            {
                if (width != 0 && height != 0)
                {
                    float ratio;
                    if (texture2D.width > texture2D.height)
                    {
                        ratio = (float)texture2D.width / width;
                    }
                    else
                    {
                        ratio = (float)texture2D.height / height;
                    }

                    if (ratio > 1.0f)
                    {
                        var resizedTexture = TextureUtility.ResizeTexture(texture2D, TextureUtility.ImageFilterMode.Average, 1f / ratio);

                        if (resizedTexture != null)
                        {
                            // 기존꺼 삭제.
                            Destroy(texture2D);

                            // 리사이징한걸로 교체.
                            texture2D = resizedTexture;
                            texture2D.name = $"[MediaService.ImageProvider] Resized {texture2D.width}x{texture2D.height}";
                        }
                    }
                }

                if (targetMaterial == null)
                {
                    renderEventHandler.OnChangeResolution(texture2D.width, texture2D.height, texture2D);
                }
                else
                {
                    targetMaterial.SetTexture("_MainTex", texture2D);
                }

                onLoadComplete?.Invoke(null);
                onLoadComplete = null;
            }
            else
            {
                // log error.
                onLoadComplete?.Invoke(ePresentation.FAILED_INITIALIZE);
                onLoadComplete = null;
            }
            isLoadComplete = true;
        }

        public override void UpdateState(MediaUser userInfo, LandMediaData mediaData)
        {

        }

        public override void UpdateMuteState(MediaUser userInfo, LandMediaData mediaData)
        {
        }

        public override bool CheckMediaData(LandMediaData mediaData)
        {
            return true;
        }

        public override int GetMaxPosition()
        {
            return 0;
        }

        public override MediaMetaData GetMetaData()
        {
            return null;
        }

        public override int GetNextPosition(bool isLast)
        {
            return 0;
        }

        public override int GetPosition()
        {
            return 0;
        }

        public override int GetPrevPosition(bool isFirst)
        {
            return 0;
        }

        public override void WaitLoadMedia(MediaUser userInfo, LandMediaData data, Action<Enum> onResponse)
        {
            onResponse?.Invoke(null);
        }

        public override void SetPosition(eDragState dragState, float position)
        {
        }

        public override void UpdateResolution(int width, int height)
        {
        }

        public override bool IsPlayingAudio()
        {
            return false;
        }        
    }
}