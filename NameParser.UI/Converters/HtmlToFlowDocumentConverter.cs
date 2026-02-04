using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace NameParser.UI.Converters
{
    /// <summary>
    /// Converts HTML content to FlowDocument for display in RichTextBox
    /// Supports basic HTML tags: b, strong, i, em, u, br, p, h1-h6, ul, ol, li, a
    /// </summary>
    public static class HtmlToFlowDocumentConverter
    {
        public static FlowDocument Convert(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return new FlowDocument();

            var document = new FlowDocument();
            document.PagePadding = new Thickness(10);

            // Parse HTML and convert to paragraphs
            var paragraphs = SplitIntoParagraphs(html);

            foreach (var paragraphHtml in paragraphs)
            {
                var paragraph = ParseParagraph(paragraphHtml);
                if (paragraph != null)
                {
                    document.Blocks.Add(paragraph);
                }
            }

            return document;
        }

        private static string[] SplitIntoParagraphs(string html)
        {
            // Split by <p>, <br>, <h1>-<h6>, <div> tags
            var paragraphs = Regex.Split(html, @"</?(?:p|div|br\s*/?)>|<h[1-6]>|</h[1-6]>", RegexOptions.IgnoreCase);
            return paragraphs;
        }

        private static Paragraph ParseParagraph(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var paragraph = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            // Check if it's a heading
            var headingMatch = Regex.Match(html, @"^<h(\d)>(.*?)</h\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (headingMatch.Success)
            {
                var level = int.Parse(headingMatch.Groups[1].Value);
                var content = headingMatch.Groups[2].Value;
                
                var run = new Run(StripHtml(content))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 24 - (level * 2) // h1=22, h2=20, h3=18, etc.
                };
                paragraph.Inlines.Add(run);
                return paragraph;
            }

            // Parse inline elements
            ParseInlineElements(html, paragraph);

            return paragraph.Inlines.Count > 0 ? paragraph : null;
        }

        private static void ParseInlineElements(string html, Paragraph paragraph)
        {
            // Simple regex-based parser for inline elements
            var position = 0;
            var boldPattern = @"<(?:b|strong)>(.*?)</(?:b|strong)>";
            var italicPattern = @"<(?:i|em)>(.*?)</(?:i|em)>";
            var underlinePattern = @"<u>(.*?)</u>";
            var linkPattern = @"<a\s+href=[""']([^""']+)[""'][^>]*>(.*?)</a>";

            // Find all tags and their positions
            var matches = new System.Collections.Generic.List<(int start, int length, string type, string content, string url)>();

            foreach (Match match in Regex.Matches(html, boldPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                matches.Add((match.Index, match.Length, "bold", match.Groups[1].Value, null));
            }
            foreach (Match match in Regex.Matches(html, italicPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                matches.Add((match.Index, match.Length, "italic", match.Groups[1].Value, null));
            }
            foreach (Match match in Regex.Matches(html, underlinePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                matches.Add((match.Index, match.Length, "underline", match.Groups[1].Value, null));
            }
            foreach (Match match in Regex.Matches(html, linkPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                matches.Add((match.Index, match.Length, "link", match.Groups[2].Value, match.Groups[1].Value));
            }

            // Sort by position
            matches.Sort((a, b) => a.start.CompareTo(b.start));

            // Process text and tags
            if (matches.Count == 0)
            {
                // No formatting, just add plain text
                var text = StripHtml(html);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    paragraph.Inlines.Add(new Run(text));
                }
                return;
            }

            foreach (var match in matches)
            {
                // Add text before this tag
                if (match.start > position)
                {
                    var plainText = StripHtml(html.Substring(position, match.start - position));
                    if (!string.IsNullOrWhiteSpace(plainText))
                    {
                        paragraph.Inlines.Add(new Run(plainText));
                    }
                }

                // Add formatted text
                var content = StripHtml(match.content);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    switch (match.type)
                    {
                        case "bold":
                            paragraph.Inlines.Add(new Run(content) { FontWeight = FontWeights.Bold });
                            break;
                        case "italic":
                            paragraph.Inlines.Add(new Run(content) { FontStyle = FontStyles.Italic });
                            break;
                        case "underline":
                            paragraph.Inlines.Add(new Run(content) { TextDecorations = TextDecorations.Underline });
                            break;
                        case "link":
                            var hyperlink = new Hyperlink(new Run(content))
                            {
                                NavigateUri = new Uri(match.url, UriKind.RelativeOrAbsolute),
                                Foreground = Brushes.Blue,
                                TextDecorations = TextDecorations.Underline
                            };
                            hyperlink.RequestNavigate += (s, e) =>
                            {
                                try
                                {
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
                                }
                                catch { }
                            };
                            paragraph.Inlines.Add(hyperlink);
                            break;
                    }
                }

                position = match.start + match.length;
            }

            // Add remaining text
            if (position < html.Length)
            {
                var plainText = StripHtml(html.Substring(position));
                if (!string.IsNullOrWhiteSpace(plainText))
                {
                    paragraph.Inlines.Add(new Run(plainText));
                }
            }
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Remove HTML tags
            var text = Regex.Replace(html, @"<[^>]+>", "");

            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);

            return text.Trim();
        }

        public static string FlowDocumentToHtml(FlowDocument document)
        {
            if (document == null)
                return string.Empty;

            var html = new System.Text.StringBuilder();

            foreach (var block in document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    html.Append("<p>");

                    foreach (var inline in paragraph.Inlines)
                    {
                        if (inline is Run run)
                        {
                            var text = System.Net.WebUtility.HtmlEncode(run.Text);

                            if (run.FontWeight == FontWeights.Bold)
                                text = $"<b>{text}</b>";
                            if (run.FontStyle == FontStyles.Italic)
                                text = $"<i>{text}</i>";
                            if (run.TextDecorations == TextDecorations.Underline)
                                text = $"<u>{text}</u>";

                            html.Append(text);
                        }
                        else if (inline is Hyperlink hyperlink)
                        {
                            var text = System.Net.WebUtility.HtmlEncode(new TextRange(hyperlink.ContentStart, hyperlink.ContentEnd).Text);
                            html.Append($"<a href=\"{hyperlink.NavigateUri}\">{text}</a>");
                        }
                    }

                    html.Append("</p>");
                }
                else if (block is Table table)
                {
                    html.Append("<table style='width: 100%; border-collapse: collapse;'>");

                    foreach (var rowGroup in table.RowGroups)
                    {
                        foreach (var row in rowGroup.Rows)
                        {
                            var rowStyle = "";
                            if (row.Background is SolidColorBrush bgBrush)
                            {
                                rowStyle += $"background-color: {bgBrush.Color};";
                            }
                            if (row.Foreground is SolidColorBrush fgBrush)
                            {
                                rowStyle += $"color: {fgBrush.Color};";
                            }

                            html.Append($"<tr{(string.IsNullOrEmpty(rowStyle) ? "" : $" style='{rowStyle}'")}>");

                            foreach (var cell in row.Cells)
                            {
                                var cellStyle = "";
                                if (cell.Padding != default(Thickness))
                                {
                                    cellStyle += $"padding: {cell.Padding.Left}px;";
                                }
                                if (cell.Background is SolidColorBrush cellBgBrush)
                                {
                                    cellStyle += $"background-color: {cellBgBrush.Color};";
                                }

                                html.Append($"<td{(string.IsNullOrEmpty(cellStyle) ? "" : $" style='{cellStyle}'")}>");

                                foreach (var cellBlock in cell.Blocks)
                                {
                                    if (cellBlock is Paragraph cellParagraph)
                                    {
                                        foreach (var inline in cellParagraph.Inlines)
                                        {
                                            if (inline is Run run)
                                            {
                                                var text = System.Net.WebUtility.HtmlEncode(run.Text);

                                                if (run.FontWeight == FontWeights.Bold)
                                                    text = $"<b>{text}</b>";
                                                if (run.FontStyle == FontStyles.Italic)
                                                    text = $"<i>{text}</i>";
                                                if (run.TextDecorations == TextDecorations.Underline)
                                                    text = $"<u>{text}</u>";

                                                html.Append(text);
                                            }
                                        }
                                    }
                                }

                                html.Append("</td>");
                            }

                            html.Append("</tr>");
                        }
                    }

                    html.Append("</table>");
                }
            }

            return html.ToString();
        }
    }
}
