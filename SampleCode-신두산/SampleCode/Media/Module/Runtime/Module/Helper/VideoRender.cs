using System;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

namespace  UCF.Media.Module
{
    public class VideoRender
    {
        private Material material;
        private ApplyToMaterialExtension applyToMaterial;

        private Action<bool> onChangedTexture;
        private Action<Texture> onAppliedMapping;

        /// <summary>
        /// Android Bluetooth 이슈로 인해 특정 상황에
        /// 해당 클래스의 일부 기능을 동작하지 않도록 해야하는 케이스에 사용.
        /// </summary>
        private bool ignoreProcess;
        private RenderTexture tempTexture;

        public VideoRender(Transform transform, MediaPlayer player)
        {
            ignoreProcess = false;

            applyToMaterial = transform.gameObject.AddComponent<ApplyToMaterialExtension>();
            applyToMaterial.Player = player;
            applyToMaterial.onAppliedMapping = OnAppliedMapping;
            applyToMaterial.onChangedDefaultTexture = OnChangedDefaultTexture;
        }

        public void SetDefaultTexture(Texture2D texture)
        {
            applyToMaterial.DefaultTexture = texture;
        }

        public Material GetMaterial()
        {
            if (material == null)
            {
                return null;
            }
            else
            {
                return material;
            }
        }

        public void SetMaterial(Material material)
        {
            this.material = material;
            applyToMaterial.Material = material;
        }

        public void SetChangedTextureCallback(Action<bool> callback)
        {
            onChangedTexture = callback;
        }

        public void SetAppliedMappingCallback(Action<Texture> callback)
        {
            onAppliedMapping = callback;
        }

        void OnChangedDefaultTexture(bool isDefault)
        {
            if (ignoreProcess)
            {
                return;
            }

            onChangedTexture?.Invoke(isDefault);
        }

        void OnAppliedMapping(Texture texture)
        {
            if (texture == null)
            {
                // 빈 텍스쳐 일 경우, 임시 텍스쳐를 넣어줌.
                texture = ApplyTempTexture();
            }
            else
            {
                if (ignoreProcess)
                {
                    if (texture != tempTexture)
                    {
                        ClearTempTexture();
                    }
                }
            }

            onAppliedMapping?.Invoke(texture);
        }

        public void CopyTempTexture()
        {
            // 일부 기능 제한.
            ignoreProcess = true;

            // 현재 texture.
            var origin = material.mainTexture;

            if (tempTexture == null)
            {
                // texture 없으면 신청
                tempTexture = RenderTexture.GetTemporary(origin.width, origin.height);
            }

            // 임시 texture에 현재 texture blit.
            Graphics.Blit(origin, tempTexture);
        }

        public void ClearTempTexture()
        {
            if (tempTexture != null)
            {
                RenderTexture.ReleaseTemporary(tempTexture);
                tempTexture = null;
            }

            // 일부 기능 제한 해제.
            ignoreProcess = false;
        }

        public Texture ApplyTempTexture()
        {
            if (tempTexture != null)
            {
                material.mainTexture = tempTexture;
                applyToMaterial.onAppliedMapping?.Invoke(tempTexture);
            }
            return tempTexture;
        }
    }
}