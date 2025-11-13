using System;
using System.Collections.Generic;
using RenderHeads.Media.AVProVideo;
using UnityEngine;

namespace  UCF.Media.Module
{
    public static class VideoCache
    {
        private class ElementComparer : IComparer<Element>
        {
            public int Compare(Element x, Element y)
            {
                if (x == null || y == null)
                    return 0;

                int nx = int.Parse(x.usedDate);
                int ny = int.Parse(y.usedDate);

                // 사용 날짜 기준 비교 (오래된 순 => 최신 순)
                int ret1 = ny.CompareTo(nx);

                // 용량 비교 (큰 것 => 작은 것)
                int ret2 = ret1 != 0 ? ret1 : y.videoSize.CompareTo(x.videoSize);

                return ret2;
            }
        }

        [Serializable]
        private class CacheInfo
        {
            public List<Element> list;

            public CacheInfo()
            {
                list = new List<Element>();
            }
        }

        [Serializable]
        private class Element
        {
            public string url;
            public string usedDate;
            public long videoSize;
        }

        private const string CACHE_KEY = "VIDEO_CACHE";
        private const string DATETIME_FORMAT = "yyyyMMdd";

        private static CacheInfo cacheInfo = null;

        private static MediaCachingOptions options;

        /// <summary>
        /// 캐시된 영상을 일괄 삭제합니다.
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="storage"></param>
        public static void Cleanup(IMediaCache cache)
        {
            // 데이터 불러오기.
            LoadData();

            // 일괄 삭제.
            int count = cacheInfo.list.Count;
            for (int index = 0; index < count; index++)
            {
                // 캐시 제거.
                cache.RemoveMediaFromCache(cacheInfo.list[index].url);
            }

            Debug.Log($"[VideoCache] Clean up {count} videos.");

            // 리스트 제거.
            cacheInfo.list.Clear();

            // 데이터 저장.
            SaveData();
        }

        public static void Cache(IMediaCache cache, string url)
        {
            // cacheMaxCount가 0 미만일 경우, 캐시 비활성화
            if (VideoConfig.VideoCacheCount < 0)
            {
                Debug.Log("[VideoCache] Disabled cache.");
                return;
            }

            // 데이터 불러오기.
            LoadData();

            // 데이터 업데이트.
            UpdateData(url);

            // 캐시 추가.
            if (options == null)
            {
                options = new MediaCachingOptions();
                options.title = "[il] Video";
            }
            cache.AddMediaToCache(url, null, options);

            // 캐시 리스트 검사.
            ValidateCacheList(cache);

            // 데이터 저장.
            SaveData();
        }

        private static void LoadData()
        {
            if (cacheInfo == null)
            {
                string jsonData = PlayerPrefs.GetString(CACHE_KEY, string.Empty);
                if (string.IsNullOrEmpty(jsonData))
                {
                    // 저장된 데이터가 없으면 생성.
                    cacheInfo = new CacheInfo();
                }
                else
                {
                    // 저장된 데이터를 불러온다.
                    cacheInfo = JsonUtility.FromJson<CacheInfo>(jsonData);
                }
            }
        }

        private static void UpdateData(string url)
        {
            string today = DateTime.Now.ToString(DATETIME_FORMAT);
            int count = cacheInfo.list.Count;

            for (int index = 0; index < count; index++)
            {
                var item = cacheInfo.list[index];
                if (item.url == url)
                {
                    // 사용 날짜 갱신.
                    item.usedDate = today;
                    return;
                }
            }

            // not found.
            // create
            cacheInfo.list.Add(new Element {
                url = url,
                usedDate = today
            });
        }

        private static void ValidateCacheList(IMediaCache cache)
        {
            // 최대 캐싱 갯수를 초과했을 때.
            if (cacheInfo.list.Count > VideoConfig.VideoCacheCount)
            {
                // 기준에 맞춰 리스트 정렬.
                cacheInfo.list.Sort(new ElementComparer());

                // 순회돌면서 삭제하기위해 거꾸로..
                for (int index =  cacheInfo.list.Count - 1; index >= VideoConfig.VideoCacheCount; index--)
                {
                    var item = cacheInfo.list[index];

                    // 캐시 제거.
                    cache.RemoveMediaFromCache(item.url);

                    // 원소 제거.
                    cacheInfo.list.RemoveAt(index);
                }
            }
        }

        private static void SaveData()
        {
            if (cacheInfo != null)
            {
                if (cacheInfo.list.Count > 0)
                {
                    PlayerPrefs.SetString(CACHE_KEY, JsonUtility.ToJson(cacheInfo));
                    PlayerPrefs.Save();
                }
            }
        }
    }
}