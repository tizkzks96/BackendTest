using System;
using  UCF.Core.Helper.Telepresence;
using UnityEngine;

namespace  UCF.Media.Service
{
    using Module;

    internal abstract class BaseProvider : MonoBehaviour
    {
        public abstract void Initialize(IMediaModuleEventHandler handler, Material material = null);
        public abstract void Load(string path, Action<Enum> onResponse);
        public abstract void UpdateState(MediaUser userInfo, LandMediaData mediaData);

        /// <param name="userInfo"></param>
        /// <param name="data"></param>
        /// <param name="isLoadComplete"></param>
        /// <param name="onResponse"></param>
        public abstract void WaitLoadMedia(MediaUser userInfo, LandMediaData data, Action<Enum> onResponse);
        public abstract void UpdateMuteState(MediaUser userInfo, LandMediaData mediaData);
        public abstract void Dispose();

        public abstract bool IsLoadProvider();
        public abstract int GetPrevPosition(bool isFirst);
        public abstract int GetNextPosition(bool isLast);
        public abstract int GetPosition();
        public abstract int GetMaxPosition();
        public abstract MediaMetaData GetMetaData();

        public abstract void UpdateResolution(int width, int height);

        public abstract void SetPosition(eDragState dragState, float position);
        public abstract bool CheckMediaData(LandMediaData mediaData);

        public abstract bool IsPlayingAudio();

        public static Type GetProviderType(CommonEnum.CommonConferenceURLFormat type)
        {
            switch ((ePresentationType)type)
            {
                default:
                    throw new Exception($"Not supported provider type. type: {type}");

                case ePresentationType.PDF:
                    return typeof(DocumentProvider);

                case ePresentationType.Media:
                    return typeof(VideoProvider);

                case ePresentationType.MRLis_Media:
                case ePresentationType.MRLis_Image:
                case ePresentationType.MRLis_Screen:
                    return typeof(ShareProvider);

                case ePresentationType.Image:
                    return typeof(ImageProvider);
            }
        }

        public static BaseProvider CreateProvider(CommonEnum.CommonConferenceURLFormat type)
        {
            Type providerType = GetProviderType(type);
            GameObject go = new GameObject(providerType.Name, providerType);
            go.transform.parent = MediaService.Instance.transform;

            return go.GetComponent<BaseProvider>();
        }

		public virtual void WaitLoadMedia(Action<Enum> onResponse)
		{

		}
	}
}