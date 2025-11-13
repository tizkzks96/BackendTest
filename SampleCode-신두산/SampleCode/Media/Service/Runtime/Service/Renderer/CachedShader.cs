using System.Collections.Generic;
using UnityEngine;

namespace  UCF.Media.Service
{
    internal static class CachedShader
    {
        private static Dictionary<string, Shader> shaderDictionary;

        public const string RGB_SHADER_NAME = "il/Media/RGB";
        public const string YUV_SHADER_NAME = "il/Media/YUV";

        public static void Load()
        {
            shaderDictionary = new Dictionary<string, Shader>();
            shaderDictionary.Add(RGB_SHADER_NAME, Shader.Find(RGB_SHADER_NAME));
            shaderDictionary.Add(YUV_SHADER_NAME, Shader.Find(YUV_SHADER_NAME));
        }

        public static Shader GetShader(string shaderName)
        {
            Shader shader;
            shaderDictionary.TryGetValue(shaderName, out shader);
            return shader;
        }
    }
}