// SteamIgnoreParser.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BetterModUpload.IgnoreParser
{
    /// <summary>
    /// Gitignore模式解析器
    /// </summary>
    public class SteamIgnorePattern
    {
        public string OriginalPattern { get; }
        public string Pattern { get; private set; }
        public bool IsNegation { get; } = false;
        public bool IsDirectoryOnly { get; } = false;
        public bool IsAbsolute { get; } = false;
        public Regex? Regex { get; private set; }

        public SteamIgnorePattern(string pattern)
        {
            OriginalPattern = pattern;

            // 去除前后空格
            pattern = pattern.Trim();

            // 检查是否是否定规则（以!开头）
            if (pattern.StartsWith("!"))
            {
                IsNegation = true;
                pattern = pattern.Substring(1).Trim();
            }

            // 检查是否是目录规则（以/结尾）
            if (pattern.EndsWith("/"))
            {
                IsDirectoryOnly = true;
                pattern = pattern.Substring(0, pattern.Length - 1);
            }

            // 检查是否是绝对路径
            if (pattern.StartsWith("/"))
            {
                IsAbsolute = true;
                pattern = pattern.Substring(1);
            }

            Pattern = pattern;
            GenerateRegex();
        }

        private void GenerateRegex()
        {
            // 转义正则特殊字符
            Pattern = Regex.Escape(Pattern);

            // 将steamignore通配符转换为正则表达式
            Pattern = Pattern
                .Replace(@"\*\*", ".*")       // ** 匹配任意多级目录
                .Replace(@"\*", "[^/]*")      // * 匹配非斜杠字符
                .Replace(@"\?", "[^/]");      // ? 匹配单个非斜杠字符

            // 处理 [abc] 字符类
            Pattern = Regex.Replace(Pattern, @"\\\[(.*?)\\\]", m => "[" + m.Groups[1].Value.Replace(@"\", "") + "]");

            // 构建完整正则表达式
            string regexPattern;

            if (IsAbsolute)
            {
                // 绝对路径：从根目录开始匹配
                regexPattern = $"^{Pattern}";
            }
            else if (Pattern.Contains("**"))
            {
                // 包含**的模式
                regexPattern = Pattern;
            }
            else
            {
                // 相对路径：匹配任意前缀
                regexPattern = $"(.*/)?{Pattern}";
            }

            // 如果是目录规则，添加斜杠
            if (IsDirectoryOnly)
            {
                regexPattern = $"{regexPattern}/.*";
            }
            else
            {
                regexPattern = $"{regexPattern}$";
            }

            Regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public bool IsMatch(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return false;

            // 统一路径分隔符
            relativePath = relativePath.Replace('\\', '/');

            return Regex?.IsMatch(relativePath) ?? false && !IsNegation;
        }
    }

    /// <summary>
    /// Gitignore解析器主类
    /// </summary>
    public class SteamIgnoreParser
    {
        private readonly List<SteamIgnorePattern> _patterns = new List<SteamIgnorePattern>();
        private string _baseDirectory;

        public IReadOnlyList<SteamIgnorePattern> Patterns => _patterns.AsReadOnly();

        /// <summary>
        /// 从文件加载steamignore规则
        /// </summary>
        public SteamIgnoreParser(string steamignorePath)
        {
            _baseDirectory = Path.GetDirectoryName(steamignorePath);
            if (!File.Exists(steamignorePath))
                throw new FileNotFoundException($"Gitignore file not found: {steamignorePath}");

            var content = File.ReadAllLines(steamignorePath);
            ParseLines(content);
        }

        public SteamIgnoreParser(IEnumerable<string> lines, string? baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ParseLines(lines);
        }

        /// <summary>
        /// 从文本内容加载steamignore规则
        /// </summary>
        public SteamIgnoreParser(string content, string? baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            ParseLines(lines);
        }

        private void ParseLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                // 跳过空行和注释
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                _patterns.Add(new SteamIgnorePattern(trimmed));
            }
        }

        /// <summary>
        /// 检查文件是否被忽略
        /// </summary>
        public bool IsIgnored(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            // 转换为相对路径
            string relativePath;
            if (Path.IsPathRooted(filePath))
            {
                relativePath = Path.GetRelativePath(_baseDirectory, filePath);
            }
            else
            {
                relativePath = filePath;
            }

            // 统一路径分隔符
            relativePath = relativePath.Replace('\\', '/');

            bool isIgnored = false;

            // 按顺序应用规则
            foreach (var pattern in _patterns)
            {
                if (pattern.IsMatch(relativePath))
                {
                    isIgnored = !pattern.IsNegation;
                }
            }

            return isIgnored;
        }

        /// <summary>
        /// 检查目录是否被忽略
        /// </summary>
        public bool IsDirectoryIgnored(string directoryPath)
        {
            return IsIgnored(directoryPath + "/");
        }

        /// <summary>
        /// 过滤文件列表，返回被忽略的文件
        /// </summary>
        public IEnumerable<string> FilterIgnoredFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                if (IsIgnored(file))
                {
                    yield return file;
                }
            }
        }

        /// <summary>
        /// 过滤文件列表，返回未忽略的文件
        /// </summary>
        public IEnumerable<string> FilterNonIgnoredFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                if (!IsIgnored(file))
                {
                    yield return file;
                }
            }
        }

        /// <summary>
        /// 清空所有规则
        /// </summary>
        public void Clear()
        {
            _patterns.Clear();
        }
    }
}