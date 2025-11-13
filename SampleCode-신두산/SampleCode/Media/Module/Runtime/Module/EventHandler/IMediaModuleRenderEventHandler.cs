using UnityEngine;

namespace  UCF.Media.Module
{
    public interface IMediaModuleRenderEventHandler
    {
        /// <summary>
        /// 해상도가 변경되거나 텍스쳐가 변경될 경우 발생하는 이벤트
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="texture"></param>
        void OnChangeResolution(int width, int height, Texture texture);

        /// <summary>
        /// 해상도가 변경되거나 텍스쳐가 변경될 경우 발생하는 이벤트
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="texture"></param>
        void OnChangeResolution(int width, int height, int rotation, Texture2D texture1, Texture2D texture2, Texture2D texture3);

        /// <summary>
        /// 버퍼링 발생 이벤트
        /// </summary>
        /// <param name="enable"></param>
        void OnPlayerStalled(bool enable);
    }
}