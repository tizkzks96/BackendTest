using System;
using System.Collections.Generic;
using  UCF.Core.Helper.Telepresence;
using UnityEngine;

namespace  UCF.Media.Service
{
    internal class MediaProviderExtension : IMediaModuleEventHandler
    {
        private Dictionary<int, ExtensionProvider> extensionDictionary;

        private MediaDefinition.AVProEvent eventData;

        public MediaProviderExtension()
        {
            extensionDictionary = new Dictionary<int, ExtensionProvider>();
        }

        public bool RegisterProp(int instanceId, CommonEnum.CommonConferenceURLFormat type, Material material, Action<int> onNotifyChangeMediaState, Action<double, double> onNotifyChangeMediaProgress)
        {
            ePresentationType mediaType = (ePresentationType)type;
            if (mediaType != ePresentationType.Media && mediaType != ePresentationType.Image)
            {
                Debug.LogError($"지원하지 않는 타입입니다.: {instanceId}");
                return false;
            }

            if (extensionDictionary.ContainsKey(instanceId) == false)
            {
                var provider = BaseProvider.CreateProvider(type);
                provider.Initialize(this, material);

                extensionDictionary.Add(instanceId, new ExtensionProvider(provider, onNotifyChangeMediaState, onNotifyChangeMediaProgress));
                return true;
            }

            Debug.LogError($"이미 등록되어 있습니다. instanceId: {instanceId}");
            return false;
        }

        public bool UnregisterProp(int instanceId)
        {
            if (extensionDictionary.TryGetValue(instanceId, out ExtensionProvider extension))
            {
                extension.Dispose();

                extensionDictionary.Remove(instanceId);
                return true;
            }
            Debug.LogError($"프랍이 등록되어 있지 않습니다. instanceId: {instanceId}");
            return false;
        }

        public bool UpdateMediaInfo(int instanceId, string url, int width, int height)
        {
            if (extensionDictionary.TryGetValue(instanceId, out ExtensionProvider extension))
            {
                extension.mediaData.state = CommonEnum.CommonConferenceMediaStatus.Pause;
                extension.mediaData.position = 0;

                if (extension.provider is ImageProvider imageProvider)
                {
                    imageProvider.LoadExtension(url, width, height, OnLoad);
                }
                else
                {
                    extension.provider.Load(url, OnLoad);
                }

                extension.provider.UpdateState(null, extension.mediaData);
                return true;
            }
            Debug.LogError($"프랍이 등록되어 있지 않습니다. instanceId: {instanceId}");
            return false;

            void OnLoad(Enum _)
            {
                extension.onNotifyChangeMediaState?.Invoke(1);
            }
        }

        public bool UpdateMediaState(int instanceId, bool isPlay, int position)
        {
            if (extensionDictionary.TryGetValue(instanceId, out ExtensionProvider extension))
            {
                extension.mediaData.state = (isPlay ? CommonEnum.CommonConferenceMediaStatus.Playing : CommonEnum.CommonConferenceMediaStatus.Pause);

                if (position < 0)
                {
                    extension.mediaData.position = extension.provider.GetPosition();
                }
                else
                {
                    extension.mediaData.position = position;
                }

                extension.provider.UpdateState(null, extension.mediaData);
                return true;
            }
            Debug.LogError($"프랍이 등록되어 있지 않습니다. instanceId: {instanceId}");
            return false;
        }

        public bool UpdateMuteState(int instanceId, bool isMute)
        {
            if (extensionDictionary.TryGetValue(instanceId, out ExtensionProvider extension))
            {
                extension.mediaData.IsLocalMute = isMute ? 1 : 0;

                extension.provider.UpdateMuteState(null, extension.mediaData);
                return true;
            }
            Debug.LogError($"프랍이 등록되어 있지 않습니다. instanceId: {instanceId}");
            return false;
        }
        
        public bool GetMediaPosition(int instanceId, Action<int> callback)
        {
            if (extensionDictionary.TryGetValue(instanceId, out ExtensionProvider extension))
            {
                int val = extension.provider.GetPosition() / 1000;
                callback?.Invoke(val);
                return true;
            }
            Debug.LogError($"프랍이 등록되어 있지 않습니다. instanceId: {instanceId}");
            return false;
        }

        public bool GetMediaLength(int instanceId, Action<int> callback)
        {
            if (extensionDictionary.TryGetValue(instanceId, out ExtensionProvider extension))
            {
                int val = extension.provider.GetMaxPosition();
                callback?.Invoke(val);
                return true;
            }
            Debug.LogError($"프랍이 등록되어 있지 않습니다. instanceId: {instanceId}");
            return false;
        }

        public void CheckMediaState(LandMediaData landInfo)
        {
            // 랜드 내에 발표자료가 화면공유일 경우.
            bool isLocalControl = landInfo.presentationType == CommonEnum.CommonConferenceURLFormat.MRLis_Screen;
            foreach (var ext in extensionDictionary)
            {
                if (ext.Value.provider is VideoProvider a)
                {
                    ext.Value.mediaData.isLocalControl = isLocalControl;
                    a.UpdateState(null, ext.Value.mediaData);
                }
            }
        }

        void IMediaModuleEventHandler.OnVideoPlayerEvent(BaseProvider provider, object data)
        {
            foreach(var extenion in extensionDictionary)
            {
                if (extenion.Value.provider == provider)
                {
                    eventData = (MediaDefinition.AVProEvent)data;
                    extenion.Value.onNotifyChangeMediaState?.Invoke(eventData.eventCode);
                    break;
                }
            }
        }

        void IMediaModuleEventHandler.OnMediaProgress(BaseProvider provider, double current, double length)
        {
            foreach (var extenion in extensionDictionary)
            {
                if (extenion.Value.provider == provider)
                {
                    extenion.Value.onNotifyChangeMediaProgress?.Invoke(current, length);
                    break;
                }
            }
        }

        void IMediaModuleEventHandler.OnPlayerFinished(BaseProvider provider, double time)
        {
        }
    }
}
