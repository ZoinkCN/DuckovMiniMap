using Duckov.Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ZoinkModdingLibrary.Utils
{
    public static class ModFileOperations
    {
        private static string? DirectoryName = null;
        private static Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>();

        public static string GetDirectory()
        {
            if (DirectoryName == null)
            {
                DirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            return DirectoryName;
        }

        public static Sprite? LoadSprite(string? texturePath)
        {
            lock (LoadedSprites)
            {
                if (string.IsNullOrEmpty(texturePath))
                {
                    return null;
                }
                if (LoadedSprites.ContainsKey(texturePath))
                {
                    return LoadedSprites[texturePath];
                }
                string directoryName = GetDirectory();
                string path = Path.Combine(directoryName, "textures");
                string text = Path.Combine(path, texturePath);
                if (File.Exists(text))
                {
                    byte[] data = File.ReadAllBytes(text);
                    Texture2D texture2D = new Texture2D(2, 2);
                    if (texture2D.LoadImage(data))
                    {
                        Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), new Vector2(0.5f, 0.5f));
                        if (!LoadedSprites.ContainsKey(texturePath))
                        {
                            LoadedSprites[texturePath] = sprite;
                        }
                        return sprite;
                    }
                }
                return null;
            }
        }

        public static JObject? LoadJson(string filePath, ModLogger? logger = null)
        {
            logger ??= ModLogger.DefultLogger;
            string directoryName = GetDirectory();
            string path = Path.Combine(directoryName, "config");
            string text = Path.Combine(path, filePath);
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
                logger.LogError($"Failed to Load Json({text}):\n{e.Message}");
                return null;
            }
        }

        public static void SaveJson(string modConfigFileName, JObject modConfig, ModLogger? logger = null)
        {
            logger ??= ModLogger.DefultLogger;
            string directoryName = GetDirectory();
            string path = Path.Combine(directoryName, "config");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string text = Path.Combine(path, modConfigFileName);
            try
            {
                File.WriteAllText(text, modConfig.ToString(Formatting.Indented));
            }
            catch (Exception e)
            {
                logger.LogError($"Failed to Save Json: {e.Message}");
                throw;
            }
        }
    }
}
