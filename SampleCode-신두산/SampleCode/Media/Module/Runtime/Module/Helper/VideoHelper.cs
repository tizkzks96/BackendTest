using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace  UCF.Media.Module
{
    using Core.Helper;

    public static class VideoHelper
    {
        private const string HTTP = "http";

        private const string EXTENSION_MP4 = ".mp4";
        private const string EXTENSION_M3U8 = ".m3u8";

        private const string TAG_M3U8 = "#EXTM3U";
        private const string TAG_PLAYLIST = "#EXT-X-STREAM-INF";
        private const string TAG_VOD = "#EXT-X-PLAYLIST-TYPE:VOD";
        private const string TAG_ENDLIST = "#EXT-X-ENDLIST";

        public static async Task<(bool success, bool cache)> ValidateCacheVideo(string url, CancellationToken token)
        {
            // URL이 빈 값임.
            if (string.IsNullOrEmpty(url) == true)
            {
                return (false, false);
            }

            // URL에 http가 없음 => 로컬파일.
            // 로컬 파일은 캐시 안함.
            if (url.Contains(HTTP) == false)
            {
                return (true, false);
            }

            Uri uri = new Uri(url);
#if UNITY_IOS
            // mp4 캐시 미지원.
            if (url.Contains(EXTENSION_MP4) == true)
            {
                return (true, false);
            }
#elif UNITY_STANDALONE || UNITY_EDITOR_WIN || UNITY_EDITOR_OSX
            // macOS, Windows 캐시 미지원.
            return (true, false);
#endif

            // m3u8이 아님.
            if (url.Contains(EXTENSION_M3U8) == false)
            {
                return (true, true);
            }

            // m3u8 파일 manifest 정보 요청.
            string[] manifest = await RequestM3U8(url, token);

            // 실패.
            if (manifest == null)
            {
                return (false, false);
            }

            string playlistURL = string.Empty;
            for (int index = 0; index < manifest.Length; index++)
            {
                // 파일 끝 태그.
                if (manifest[index].Contains(TAG_ENDLIST) == true)
                {
                    // VOD 파일임.
                    return (true, true);
                }

                if (string.IsNullOrEmpty(playlistURL) == true)
                {
                    if (manifest[index].Contains(TAG_PLAYLIST) == true)
                    {
                        // 해당 태그 다음 줄에 URL 정보가 있음.
                        playlistURL = manifest[index + 1];
                    }
                }
            }

            // 정보 없음?
            if (string.IsNullOrEmpty(playlistURL) == true)
            {
                return (false, false);
            }

            StringBuilder sb = new StringBuilder();

            if (playlistURL.Contains("http") == true)
            {
                // URL이 절대 경로.
                sb.Append(playlistURL);
            }
            else
            {
                // URL이 상대 경로.
                sb.Append(uri.GetLeftPart(UriPartial.Authority));

                // 마지막 세그먼트는 제외하고 url 생성.
                for (int index = 0; index < uri.Segments.Length - 1; index++)
                {
                    sb.Append(uri.Segments[index]);
                }

                // 플레이리스트 url 추가.
                sb.Append(playlistURL);
            }

            manifest = await RequestM3U8(sb.ToString(), token);
            for (int index = 0; index < manifest.Length; index++)
            {
                if (manifest[index].Contains(TAG_VOD))
                {
                    // VOD 파일임.
                    return (true, true);
                }
                // 파일 끝 태그.
                else if (manifest[index].Contains(TAG_ENDLIST) == true)
                {
                    // VOD 파일임.
                    return (true, true);
                }
            }

            // 라이브 스트림 파일임.
            return (true, false);
        }

        private static async Task<string[]> RequestM3U8(string url, CancellationToken token)
        {
            bool response = false;
            string[] lines = null;

            Requester.Instance.Request(url, text =>
            {
                lines = GetLines(text, true);
                response = true;
            }, () =>
            {
                lines = null;
                response = true;
            });

            while (response == false)
            {
                if (token.IsCancellationRequested == true)
                {
                    token.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            return lines;
        }

        private static string[] GetLines(string str, bool removeEmptyLines = false)
        {
            return str.Split(new[] { "\r\n", "\r", "\n" }, removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        }
    }
}
