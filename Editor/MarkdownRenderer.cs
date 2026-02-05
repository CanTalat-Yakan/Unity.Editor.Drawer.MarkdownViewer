using System;
using System.Collections.Generic;
using System.IO;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    internal static class MarkdownRenderer
    {
        private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
            .UseAutoLinks()
            .UsePipeTables(new PipeTableOptions { RequireHeaderSeparator = false })
            .UseEmphasisExtras()
            .Build();
        
        public static VisualElement RenderFunction(string markdown, string assetPath)
        {
            var root = new VisualElement();
            root.AddToClassList("mdv-root");

            var styleSheet = AssetResolver.TryGet<StyleSheet>("UnityEssentials_USS_Markdown");
            if (styleSheet != null)
                root.styleSheets.Add(styleSheet);

            if (string.IsNullOrEmpty(markdown))
            {
                root.Add(new Label("(empty)") { pickingMode = PickingMode.Ignore });
                return root;
            }

            MarkdownDocument doc;
            try
            {
                doc = Markdown.Parse(markdown, Pipeline);
            }
            catch (Exception ex)
            {
                root.Add(MakeWarning($"Markdown parse error: {ex.Message}"));
                var raw = new TextField { value = markdown, multiline = true, isReadOnly = true };
                raw.style.height = 120;
                root.Add(raw);
                return root;
            }

            var visitor = new MarkdigUiVisitor(assetPath);
            foreach (var block in doc)
                root.Add(visitor.RenderBlock(block));

            return root;
        }

        private static VisualElement MakeWarning(string message)
        {
            var box = new VisualElement();
            box.AddToClassList("mdv-warning");
            var label = new Label(message) { pickingMode = PickingMode.Ignore };
            label.style.whiteSpace = WhiteSpace.Normal;
            box.Add(label);
            return box;
        }
    }
}
