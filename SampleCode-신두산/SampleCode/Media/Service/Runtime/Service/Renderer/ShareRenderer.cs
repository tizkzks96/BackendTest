using UnityEngine;

namespace  UCF.Media.Service
{
    using Module;
    using ScreenService;

    internal sealed class ShareRenderer : MediaRenderer, IMediaModuleRenderEventHandler
    {
        private const string _YTex = "_MainTex";
        private const string _UTex = "_MainTex2";
        private const string _VTex = "_MainTex3";
        private const string _Angle = "_Angle";

        public ShareRenderer()
        {
            material = new Material(CachedShader.GetShader(CachedShader.YUV_SHADER_NAME));

            UpdateMaterial(material);
        }

        public override void Dispose()
        {
            UpdateMediaState(eMediaState.DEFAULT);
        }

        /// <summary>
        /// 해상도가 변경되거나 텍스쳐가 변경될 경우 발생하는 이벤트
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="texture"></param>
        void IMediaModuleRenderEventHandler.OnChangeResolution(int width, int height, Texture texture)
        {
        }

        /// <summary>
        /// 해상도가 변경되거나 텍스쳐가 변경될 경우 발생하는 이벤트
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="texture"></param>
        void IMediaModuleRenderEventHandler.OnChangeResolution(int width, int height, int rotation, Texture2D texture1, Texture2D texture2, Texture2D texture3)
        {
            material.SetTexture(_YTex, texture1);
            material.SetTexture(_UTex, texture2);
            material.SetTexture(_VTex, texture3);

            material.SetFloat(_Angle, rotation);

            UpdateResolution(width, height);
            UpdateMediaState(eMediaState.PLAYING);
        }

        /// <summary>
        /// 버퍼링 발생 이벤트
        /// </summary>
        /// <param name="enable"></param>
        void IMediaModuleRenderEventHandler.OnPlayerStalled(bool enable)
        {
            if (enable)
            {
                UpdateMediaState(eMediaState.WAITING);
            }
            else
            {
                UpdateMediaState(eMediaState.PLAYING);
            }
        }
    }
}
