using System;
using  UCF.Core.Helper.Telepresence;
using UnityEngine;

namespace  UCF.Media.Service
{
    using Core.Bridge;

    public partial class MediaService : MonoBehaviour, IMediaExtension
    {
        private MediaProviderExtension extension;

        bool IMediaExtension.RegisterProp(int instanceId, CommonEnum.CommonConferenceURLFormat type, Material material,
            Action<int> onNotifyChangeMediaState, Action<double, double> onNotifyChangeMediaProgress)
        {
            extension ??= new MediaProviderExtension();
            return extension.RegisterProp(instanceId, type, material, onNotifyChangeMediaState, onNotifyChangeMediaProgress);
        }

        bool IMediaExtension.UnregisterProp(int instanceId)
        {
            extension ??= new MediaProviderExtension();
            return extension.UnregisterProp(instanceId);
        }

        bool IMediaExtension.UpdateMediaInfo(int instanceId, string url, int width, int height)
        {
            extension ??= new MediaProviderExtension();
            return extension.UpdateMediaInfo(instanceId, url, width, height);
        }

        bool IMediaExtension.UpdateMediaState(int instanceId, bool isPlay, int position)
        {
            extension ??= new MediaProviderExtension();
            return extension.UpdateMediaState(instanceId, isPlay, position);
        }

        bool IMediaExtension.UpdateMuteState(int instanceId, bool isMute)
        {
            extension ??= new MediaProviderExtension();
            return extension.UpdateMuteState(instanceId, isMute);
        }

        bool IMediaExtension.GetMediaPosition(int instanceId, Action<int> callback)
        {
            extension ??= new MediaProviderExtension();
            return extension.GetMediaPosition(instanceId, callback);
        }

        bool IMediaExtension.GetMediaLength(int instanceId, Action<int> callback)
        {
            extension ??= new MediaProviderExtension();
            return extension.GetMediaLength(instanceId, callback);
        }

        bool IMediaExtension.ShowPresentation(int instanceId)
        {
            //TODO::구현 필요, 임시로 True 반환
            return true;
        }

        bool IMediaExtension.IsPlayingAudio()
        {
            if (mediaProvider.provider != null)
            {
                return mediaProvider.IsPlayingAudio();
            }
            return false;
        }
    }
}