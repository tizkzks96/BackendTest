using System;
using UnityEngine;
using Paroxe.PdfRenderer;

namespace  UCF.Media.Module
{
    public class PDFPlayer : IDisposable
    {
        private IMediaModuleRenderEventHandler handler;

        private PDFDocument document;
        private PDFRenderer renderer;
        private Texture2D texture;

        private int screenWidth;
        private int screenHeight;

        private int pageIndex;
        private int pageCount;
        private Vector2 pageSize;

        public bool isLoad { get; private set; }

        public PDFPlayer(IMediaModuleRenderEventHandler handler)
        {
            this.handler = handler;

            isLoad = false;
            pageIndex = 0;
            pageCount = 0;
            pageSize = Vector2.zero;
        }

        /// <summary>
        /// PDF 불러오기
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int Load(string path, string password = "")
        {
            document = new PDFDocument(path, password);
            return Initialize();
        }

        /// <summary>
        /// PDF 불러오기
        /// </summary>
        /// <param name="byteArray"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public int Load(byte[] byteArray, string password = "")
        {
            document = new PDFDocument(byteArray, password);
            return Initialize();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        /// <returns></returns>
        private int Initialize()
        {
            if (document.IsValid)
            {
                isLoad = true;

                if (renderer == null)
                    renderer = new PDFRenderer();

                pageCount = document.GetPageCount();

                MoveToPage(0);
                return 0;
            }

            var error = PDFLibrary.GetLastError();
            Debug.LogError($"[PDFPlayer] Load failed. {error}");
            return (int)error;
        }

        /// <summary>
        /// PDF 페이지 이동
        /// </summary>
        /// <param name="index"></param>
        public void Seek(int index)
        {
            if (isLoad)
            {
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= pageCount)
                {
                    index = pageCount - 1;
                }

                MoveToPage(index);
            }
        }

        protected virtual void MoveToPage(int index)
        {
            pageIndex = index;

            using (PDFPage page = document.GetPage(index))
            {
                float zoomFactor = 1.0f;

                // 원본(100%) 비율의 사이즈를 가져옴.
                Vector2 origin = page.GetPageSize(1f);

                // 해상도를 설정했을 경우에만 로직 실행.(PC만 해당)
                if (screenWidth > 0 && screenHeight > 0)
                {
                    // 해상도에 따른 비율 계산.
                    zoomFactor = ComputeZoomFactor(origin);
                }
                else // 모바일 적용
                {
                    int fhdW = 1920;
                    int fhdH = 1080;
                    if (origin.x > fhdW || origin.y > fhdW)
                    {
                        zoomFactor = ResolutionLimit(fhdW, fhdH, origin);
                    }
                }

                // 계산된 비율로 사이즈 다시 가져옴.
                Vector2 size = page.GetPageSize(zoomFactor);

                if (pageSize != size)
                {
                    pageSize = size;

                    int w = (int)pageSize.x;
                    int h = (int)pageSize.y;

                    // 텍스쳐 없으면 생성, 있으면 리사이징.
                    if (texture)
                    {
#if UNITY_2022_3_OR_NEWER
                        texture.Reinitialize(w, h);
#else
                        texture.Resize(w, h);
#endif
                    }
                    else
                    {
                        texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                        texture.filterMode = FilterMode.Bilinear;
                        texture.anisoLevel = 8;
                    }

                    // 해상도 변경 이벤트 발생.
                    handler.OnChangeResolution(w, h, texture);
                }

                // texture update.             
                renderer.RenderPageToExistingTexture(page, texture);
            }
        }


        /// <summary>
        /// 불러온 페이지의 size를 특정 비율로 해상도 조절
        /// </summary>
        /// <param name="resolutionW"> 원하는 해상도 길이 </param>>
        /// <param name="resolutionH"> 원하는 해상도 높이 </param>>
        /// <param name="size"> 페이지 크기 </param>
        /// <returns></returns>
        private float ResolutionLimit(int resolutionW, int resolutionH, Vector2 size)
        {
            float factor;
            if (size.x > size.y) // 가로가 더 긴 경우 가로의 비례에 맞게 줄임
            {
                factor = resolutionW / size.x;
            }
            else  // 세로가 더 긴경우 세로 길이에 비례
            {
                factor = resolutionH / size.y;
            }
            return factor;
        }

        /// <summary>
        /// PDF 페이지 갯수
        /// </summary>
        /// <returns></returns>
        public int GetPageCount()
        {
            return pageCount;
        }

        public void UpdateResolution(int width, int height)
        {
            if (screenWidth != width || screenHeight != height)
            {
                screenWidth = width;
                screenHeight = height;

                if (isLoad)
                {
                    // 로드된 경우에 현재 페이지 다시 그림.
                    MoveToPage(pageIndex);
                }
            }
        }

        private float ComputeZoomFactor(Vector2 size)
        {
            float factor;
            if (size.x > size.y)
            {
                factor = screenWidth / size.x;
            }
            else
            {
                factor = screenHeight / size.y;
            }
            return factor;
        }

        public void Dispose()
        {
            isLoad = false;

            if (texture)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }

            if (document != null)
                document.Dispose();

            if (renderer != null)
                renderer.Dispose();
        }
    }
}