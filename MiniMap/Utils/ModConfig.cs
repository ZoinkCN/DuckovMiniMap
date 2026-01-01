using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using UnityEngine;

namespace MiniMap.Utils
{
    public static class ModConfig
    {
        public static string? DirectoryName = null;
        public static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();

        public static string GetDirectory()
        {
            if (DirectoryName == null)
            {
                DirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            return DirectoryName;
        }

        public static string? GetFilePath(string fileName, string? subFolder = null)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }
            string directoryName = GetDirectory();
            if (!string.IsNullOrEmpty(subFolder))
            {
                directoryName = Path.Combine(directoryName, subFolder);
            }
            return Path.Combine(directoryName, fileName);
        }
        public static Sprite? LoadSprite(string? textureName)
        {
            lock (LoadedSprites)
            {
                if (string.IsNullOrEmpty(textureName))
                {
                    return null;
                }
                if (LoadedSprites.ContainsKey(textureName))
                {
                    return LoadedSprites[textureName];
                }
                string directoryName = GetDirectory();
                string path = Path.Combine(directoryName, "textures");
                string text = Path.Combine(path, textureName);
                if (File.Exists(text))
                {
                    byte[] data = File.ReadAllBytes(text);
                    Texture2D texture2D = new Texture2D(2, 2);
                    if (texture2D.LoadImage(data))
                    {
                        Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
                        if (!LoadedSprites.ContainsKey(textureName))
                        {
                            LoadedSprites[textureName] = sprite;
                        }
                        return sprite;
                    }
                }
                return null;
            }
        }
        public static JObject? LoadConfig(string fileName)
        {
            string directoryName = GetDirectory();
            string path = Path.Combine(directoryName, "config");
            string text = Path.Combine(path, fileName);
            try
            {
                if (File.Exists(text))
                {
                    string jsonText = File.ReadAllText(text);
                    return JObject.Parse(jsonText);
                }
                return null;
            }
            catch (Exception e)
            {
                ModBehaviour.Logger.LogError($"Failed to Load Congig({text}):\n{e.Message}");
                return null;
            }
        }

        public static void SaveConfig(string modConfigFileName, JObject modConfig)
        {
            string directoryName = GetDirectory();
            string path = Path.Combine(directoryName, "config");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string text = Path.Combine(path, modConfigFileName);
            File.WriteAllText(text, modConfig.ToString(Formatting.Indented));
        }
    }
}
