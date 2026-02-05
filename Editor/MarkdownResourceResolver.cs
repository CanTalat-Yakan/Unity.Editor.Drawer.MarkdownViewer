using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    internal static class MarkdownResourceResolver
    {
        public static bool IsGif(string urlOrPath)
        {
            var ext = Path.GetExtension(urlOrPath ?? string.Empty);
            return ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);
        }

        public static Texture2D TryResolveImageTexture(string urlOrPath, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath))
                return null;

            // Remote images: intentionally not supported in Inspector.
            if (urlOrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                urlOrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return null;

            // Assets/... absolute project path
            if (urlOrPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(urlOrPath);

            // relative path next to markdown asset
            var baseDir = Path.GetDirectoryName(assetPath) ?? "Assets";
            var combined = Path.GetFullPath(Path.Combine(baseDir, urlOrPath));
            var relativeToProject = TryMakeAssetPathFromAbsolute(combined);
            if (!string.IsNullOrEmpty(relativeToProject))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(relativeToProject);

            return null;
        }

        public static void HandleLinkClick(string urlOrPath, string currentAssetPath)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath))
                return;

            // External links
            if (urlOrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                urlOrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                urlOrPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                Application.OpenURL(urlOrPath);
                return;
            }

            // Anchors: not supported in inspector yet.
            if (urlOrPath.StartsWith("#", StringComparison.Ordinal))
            {
                Debug.Log($"Markdown anchor links aren’t supported in the Inspector: {urlOrPath}");
                return;
            }

            // Resolve to a project asset and select it.
            var targetAbsolute = urlOrPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFullPath(urlOrPath)
                : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(currentAssetPath) ?? "Assets", urlOrPath));

            var assetPath = urlOrPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                ? urlOrPath
                : TryMakeAssetPathFromAbsolute(targetAbsolute);

            if (!string.IsNullOrEmpty(assetPath))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                    return;
                }
            }

            Debug.LogWarning($"Markdown link target not found: {urlOrPath}");
        }

        private static string TryMakeAssetPathFromAbsolute(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return null;

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var full = Path.GetFullPath(absolutePath);

            if (!full.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return null;

            var rel = full.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            rel = rel.Replace(Path.DirectorySeparatorChar, '/');
            if (!rel.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return null;

            return rel;
        }
    }
}
