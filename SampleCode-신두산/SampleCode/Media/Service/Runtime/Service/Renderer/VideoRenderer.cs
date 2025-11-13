using UnityEngine;

namespace  UCF.Media.Service
{
    using Module;
    using ScreenService;

    internal sealed class VideoRenderer : MediaRenderer, IMediaModuleRenderEventHandler
    {
        private VideoRender render;

        public VideoRenderer(VideoRender render)
        {
            material = new Material(CachedShader.GetShader(CachedShader.RGB_SHADER_NAME));
            material.renderQueue = 3000;

            this.render = render;
            this.render.SetMaterial(material);
            this.render.SetChangedTextureCallback(OnChangedDefaultTexture);
            this.render.SetAppliedMappingCallback(OnAppliedMapping);

            UpdateMaterial(material);
        }

        public override void Dispose()
        {
            UpdateMediaState(eMediaState.DEFAULT);
        }

        private void OnChangedDefaultTexture(bool isDefault)
        {
            if (isDefault == true)
            {
                UpdateMediaState(eMediaState.DEFAULT);
            }
            else
            {
                UpdateMediaState(eMediaState.PLAYING);
            }
        }

        private void OnAppliedMapping(Texture texture)
        {
            if (texture != null)
            {
                UpdateResolution(texture.width, texture.height);
            }

            SetDirty();
        }

        /// <summary>
        /// 해상도가 변경되거나 텍스쳐가 변경될 경우 발생하는 이벤트
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="texture"></param>
        void IMediaModuleRenderEventHandler.OnChangeResolution(int width, int height, Texture texture)
        {
            //UpdateResolution(width, height);
            //UpdateMediaState(eMediaState.PLAYING);
        }

        /// <summary>
        /// 해상도가 변경되거나 텍스쳐가 변경될 경우 발생하는 이벤트
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="texture"></param>
        void IMediaModuleRenderEventHandler.OnChangeResolution(int width, int height, int rotation, Texture2D texture1, Texture2D texture2, Texture2D texture3)
        {

        }

        /// <summary>
        /// 버퍼링 발생 이벤트
        /// </summary>
        /// <param name="enable"></param>
        void IMediaModuleRenderEventHandler.OnPlayerStalled(bool enable)
        {

        }
    }
}