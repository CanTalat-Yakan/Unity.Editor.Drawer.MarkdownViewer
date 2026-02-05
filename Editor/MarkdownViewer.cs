using UnityEditor;

namespace UnityEssentials
{
    public class MarkdownViewer
    {
        [InitializeOnLoadMethod]
        private static void Initialize() =>
            TextAssetHook.Add(MarkdownRenderer.RenderFunction, ".md", ".markdown");
    }
}