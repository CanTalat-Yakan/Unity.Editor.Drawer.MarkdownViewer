using System.Collections.Generic;
using System.IO;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEssentials
{
    internal sealed class MarkdigUiVisitor
        {
            private string _assetPath;
            
            private const int MaxImageWidth = 600;
            private const int MaxImageHeight = 400;
            
            public MarkdigUiVisitor(string assetPath) =>
                _assetPath = assetPath;
            
            public VisualElement RenderBlock(Block block)
            {
                switch (block)
                {
                    case HeadingBlock h:
                        return RenderHeading(h);
                    case ParagraphBlock p:
                        return RenderParagraph(p);
                    case ListBlock l:
                        return RenderList(l);
                    case QuoteBlock q:
                        return RenderQuote(q);
                    case ThematicBreakBlock:
                        return RenderHr();
                    case FencedCodeBlock f:
                        return RenderCodeBlock(f);
                    case CodeBlock c:
                        return RenderCodeBlock(c);
                    case Table t:
                        return RenderTable(t);
                    default:
                        var fallback = new Label(block.ToString()) { pickingMode = PickingMode.Ignore };
                        fallback.style.whiteSpace = WhiteSpace.Normal;
                        fallback.AddToClassList("mdv-block");
                        return fallback;
                }
            }

            private VisualElement RenderHeading(HeadingBlock h)
            {
                var firstInline = h.Inline != null ? h.Inline.FirstChild : null;
                var el = new Label(FlattenInlineText(firstInline)) { pickingMode = PickingMode.Ignore };
                el.style.whiteSpace = WhiteSpace.Normal;
                el.AddToClassList("mdv-block");
                el.AddToClassList($"mdv-h{Mathf.Clamp(h.Level, 1, 6)}");
                return el;
            }

            private VisualElement RenderParagraph(ParagraphBlock p)
            {
                var first = p.Inline != null ? p.Inline.FirstChild : null;

                // paragraph consisting of a single image
                var linkInline = first as LinkInline;
                if (linkInline != null && linkInline.IsImage)
                {
                    var img = RenderImage(linkInline);
                    if (img != null)
                    {
                        img.AddToClassList("mdv-block");
                        return img;
                    }
                }

                var row = RenderInlineContainer(p.Inline);
                row.AddToClassList("mdv-block");
                row.AddToClassList("mdv-p");
                return row;
            }

            private VisualElement RenderList(ListBlock list)
            {
                var container = new VisualElement();
                container.AddToClassList("mdv-block");
                container.AddToClassList("mdv-list");

                foreach (var item in list)
                {
                    if (item is not ListItemBlock li)
                        continue;

                    var row = new VisualElement();
                    row.AddToClassList("mdv-li");

                    var bullet = new Label(list.IsOrdered ? $"{list.OrderedStart}." : "•") { pickingMode = PickingMode.Ignore };
                    bullet.AddToClassList("mdv-bullet");
                    bullet.style.unityTextAlign = TextAnchor.UpperRight;

                    var content = new VisualElement();
                    content.AddToClassList("mdv-liContent");

                    foreach (var sub in li)
                        content.Add(RenderBlock(sub));

                    row.Add(bullet);
                    row.Add(content);
                    container.Add(row);
                }

                return container;
            }

            private VisualElement RenderQuote(QuoteBlock q)
            {
                var box = new VisualElement();
                box.AddToClassList("mdv-block");
                box.AddToClassList("mdv-quote");

                foreach (var sub in q)
                    box.Add(RenderBlock(sub));

                return box;
            }

            private static VisualElement RenderHr()
            {
                var hr = new VisualElement { pickingMode = PickingMode.Ignore };
                hr.AddToClassList("mdv-hr");
                return hr;
            }

            private static VisualElement RenderCodeBlock(CodeBlock code)
            {
                var wrapper = new VisualElement();
                wrapper.AddToClassList("mdv-block");
                wrapper.AddToClassList("mdv-codeBlock");

                var text = code.Lines.ToString() ?? string.Empty;

                var linesContainer = new VisualElement();
                linesContainer.AddToClassList("mdv-code");

                foreach (var line in SplitLines(text))
                {
                    var l = new Label(line) { pickingMode = PickingMode.Ignore };
                    l.AddToClassList("mdv-codeLine");
                    l.style.whiteSpace = WhiteSpace.NoWrap;
                    linesContainer.Add(l);
                }

                var sv = new ScrollView(ScrollViewMode.Horizontal);
                sv.horizontalScrollerVisibility = ScrollerVisibility.Auto;
                sv.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                sv.Add(linesContainer);
                wrapper.Add(sv);

                return wrapper;
            }

            private static VisualElement RenderTable(Table table)
            {
                var scroll = new ScrollView(ScrollViewMode.Horizontal);
                scroll.AddToClassList("mdv-block");
                scroll.AddToClassList("mdv-tableScroll");
                scroll.horizontalScrollerVisibility = ScrollerVisibility.Auto;
                scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;

                var grid = new VisualElement();
                grid.AddToClassList("mdv-table");

                foreach (var rowObj in table)
                {
                    if (rowObj is not TableRow row)
                        continue;

                    var rowEl = new VisualElement();
                    rowEl.AddToClassList("mdv-tr");

                    foreach (var cellObj in row)
                    {
                        if (cellObj is not TableCell cell)
                            continue;

                        var text = ExtractTableCellText(cell);
                        var cellEl = new Label(text) { pickingMode = PickingMode.Ignore };
                        cellEl.style.whiteSpace = WhiteSpace.Normal;
                        cellEl.AddToClassList(row.IsHeader ? "mdv-th" : "mdv-td");
                        rowEl.Add(cellEl);
                    }

                    grid.Add(rowEl);
                }

                scroll.Add(grid);
                return scroll;
            }

            private static string ExtractTableCellText(TableCell cell)
            {
                if (cell == null)
                    return string.Empty;

                // Markdig tables store cell content as nested blocks (typically ParagraphBlock with inlines).
                var sb = new System.Text.StringBuilder();
                foreach (var obj in cell)
                {
                    if (obj is ParagraphBlock p)
                    {
                        var first = p.Inline != null ? p.Inline.FirstChild : null;
                        sb.Append(FlattenInlineText(first));
                    }
                    else if (obj is Block b)
                    {
                        sb.Append(b.ToString());
                    }
                }

                return sb.ToString();
            }

            private VisualElement RenderImage(LinkInline img)
            {
                var url = img.Url ?? string.Empty;
                if (string.IsNullOrEmpty(url))
                    return MakeText(string.Empty);

                if (MarkdownResourceResolver.IsGif(url))
                {
                    var warn = new Label($"GIF images aren’t supported in the Inspector: {url}") { pickingMode = PickingMode.Ignore };
                    warn.AddToClassList("mdv-warning");
                    warn.style.whiteSpace = WhiteSpace.Normal;
                    return warn;
                }

                var tex = MarkdownResourceResolver.TryResolveImageTexture(url, _assetPath);
                if (tex == null)
                {
                    var warn = new Label($"Image not found: {url}") { pickingMode = PickingMode.Ignore };
                    warn.AddToClassList("mdv-warning");
                    warn.style.whiteSpace = WhiteSpace.Normal;
                    return warn;
                }

                var container = new VisualElement();
                container.AddToClassList("mdv-image");

                var image = new Image { image = tex, scaleMode = ScaleMode.ScaleToFit };
                image.style.maxWidth = MaxImageWidth;
                image.style.maxHeight = MaxImageHeight;
                container.Add(image);

                var captionText = FlattenInlineText(img.FirstChild);
                if (!string.IsNullOrWhiteSpace(captionText))
                {
                    var caption = new Label(captionText) { pickingMode = PickingMode.Ignore };
                    caption.AddToClassList("mdv-imageCaption");
                    caption.style.whiteSpace = WhiteSpace.Normal;
                    container.Add(caption);
                }

                return container;
            }

            private VisualElement RenderInlineContainer(ContainerInline inline)
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.style.flexWrap = Wrap.Wrap;

                for (var current = inline != null ? inline.FirstChild : null; current != null; current = current.NextSibling)
                {
                    foreach (var ve in RenderInline(current))
                        container.Add(ve);
                }

                return container;
            }

            private IEnumerable<VisualElement> RenderInline(Inline inline)
            {
                switch (inline)
                {
                    case LiteralInline lit:
                        yield return MakeText(GetLiteral(lit));
                        yield break;

                    case LineBreakInline:
                        var br = new Label("\n") { pickingMode = PickingMode.Ignore };
                        br.style.whiteSpace = WhiteSpace.Pre;
                        yield return br;
                        yield break;

                    case CodeInline code:
                        var codeEl = new Label(code.Content) { pickingMode = PickingMode.Ignore };
                        codeEl.AddToClassList("mdv-inlineCode");
                        yield return codeEl;
                        yield break;

                    case EmphasisInline em:
                        foreach (var child in RenderInlineChildren(em))
                            yield return child;
                        yield break;

                    case LinkInline link when link.IsImage:
                        var img = RenderImage(link);
                        if (img != null)
                            yield return img;
                        yield break;

                    case LinkInline link:
                        yield return MakeLink(link);
                        yield break;

                    default:
                        yield return MakeText(inline.ToString());
                        yield break;
                }
            }

            private IEnumerable<VisualElement> RenderInlineChildren(ContainerInline container)
            {
                for (var c = container.FirstChild; c != null; c = c.NextSibling)
                {
                    foreach (var ve in RenderInline(c))
                        yield return ve;
                }
            }

            private VisualElement MakeLink(LinkInline link)
            {
                var url = link.Url ?? string.Empty;
                var text = FlattenInlineText(link.FirstChild);
                if (string.IsNullOrWhiteSpace(text))
                    text = url;

                var btn = new Button(() => MarkdownResourceResolver.HandleLinkClick(url, _assetPath))
                {
                    text = text
                };
                btn.AddToClassList("mdv-link");
                btn.style.paddingLeft = 0;
                btn.style.paddingRight = 0;
                btn.style.paddingTop = 0;
                btn.style.paddingBottom = 0;
                btn.style.borderTopWidth = 0;
                btn.style.borderBottomWidth = 0;
                btn.style.borderLeftWidth = 0;
                btn.style.borderRightWidth = 0;
                btn.style.backgroundColor = Color.clear;
                btn.style.unityTextAlign = TextAnchor.UpperLeft;
                return btn;
            }

            private static Label MakeText(string text)
            {
                var l = new Label(text) { pickingMode = PickingMode.Ignore };
                l.style.whiteSpace = WhiteSpace.Normal;
                return l;
            }

            private static string FlattenInlineText(Inline first)
            {
                if (first == null)
                    return string.Empty;

                var sb = new System.Text.StringBuilder();
                for (var current = first; current != null; current = current.NextSibling)
                {
                    switch (current)
                    {
                        case LiteralInline lit:
                            sb.Append(GetLiteral(lit));
                            break;
                        case LineBreakInline:
                            sb.Append("\n");
                            break;
                        case CodeInline c:
                            sb.Append(c.Content);
                            break;
                        case EmphasisInline em:
                            sb.Append(FlattenInlineText(em.FirstChild));
                            break;
                        case LinkInline link when link.IsImage:
                            break;
                        case LinkInline link:
                            sb.Append(FlattenInlineText(link.FirstChild));
                            break;
                        default:
                            sb.Append(current.ToString());
                            break;
                    }
                }

                return sb.ToString();
            }

            private static string GetLiteral(LiteralInline lit)
            {
                var slice = lit.Content;
                var t = slice.Text;
                return t != null ? t.Substring(slice.Start, slice.Length) : string.Empty;
            }

            private static IEnumerable<string> SplitLines(string text)
            {
                using (var reader = new StringReader(text ?? string.Empty))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                        yield return line;
                }

                if (!string.IsNullOrEmpty(text) && (text.EndsWith("\n") || text.EndsWith("\r")))
                    yield return string.Empty;
            }
        }
    
}