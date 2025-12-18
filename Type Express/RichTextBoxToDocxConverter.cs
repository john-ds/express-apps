using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Type_Express;

public class RichTextBoxToDocxConverter
{
    public static void ConvertToDocx(RichTextBox richTextBox, string outputPath)
    {
        using var doc = DocX.Create(outputPath);
        SaveDocument(doc, richTextBox);
    }

    public static void ConvertToDocxStream(RichTextBox richTextBox, Stream outputStream)
    {
        using var doc = DocX.Create(outputStream);
        SaveDocument(doc, richTextBox);
    }

    private static void SaveDocument(DocX doc, RichTextBox richTextBox)
    {
        FlowDocument flowDoc = richTextBox.Document;

        foreach (Block block in flowDoc.Blocks)
            ProcessBlock(doc, block);

        doc.Save();
    }

    private static void ProcessBlock(DocX doc, Block block)
    {
        if (block is System.Windows.Documents.Paragraph para)
        {
            ProcessParagraph(doc, para);
        }
        else if (block is System.Windows.Documents.List list)
        {
            ProcessList(doc, list);
        }
        else if (block is System.Windows.Documents.Table table)
        {
            ProcessTable(doc, table);
        }
        else if (block is System.Windows.Documents.Section section)
        {
            foreach (Block sectionBlock in section.Blocks)
            {
                ProcessBlock(doc, sectionBlock);
            }
        }
        else if (block is BlockUIContainer blockContainer)
        {
            if (blockContainer.Child is System.Windows.Controls.Image image)
            {
                ProcessImage(doc, image);
            }
        }
    }

    private static void ProcessParagraph(DocX doc, System.Windows.Documents.Paragraph paragraph)
    {
        var docPara = doc.InsertParagraph();

        foreach (Inline inline in paragraph.Inlines)
            ProcessInline(doc, docPara, inline);

        if (paragraph.TextAlignment == TextAlignment.Center)
            docPara.Alignment = Alignment.center;
        else if (paragraph.TextAlignment == TextAlignment.Right)
            docPara.Alignment = Alignment.right;
        else if (paragraph.TextAlignment == TextAlignment.Justify)
            docPara.Alignment = Alignment.both;
        else
            docPara.Alignment = Alignment.left;

        if (paragraph.TextIndent != 0)
            docPara.IndentationFirstLine = (float)(paragraph.TextIndent * 0.75);

        if (paragraph.Margin.Left != 0)
            docPara.IndentationBefore = (float)(paragraph.Margin.Left * 0.75);

        if (paragraph.Margin.Right != 0)
            docPara.IndentationAfter = (float)(paragraph.Margin.Right * 0.75);

        if (paragraph.Margin.Top != 0)
            docPara.SpacingBefore((float)(paragraph.Margin.Top * 0.75));

        if (paragraph.Margin.Bottom != 0)
            docPara.SpacingAfter((float)(paragraph.Margin.Bottom * 0.75));

        if (paragraph.LineHeight > 0 && !double.IsNaN(paragraph.LineHeight))
            docPara.LineSpacing = (float)paragraph.LineHeight;
    }

    private static void ProcessInline(DocX doc, Xceed.Document.NET.Paragraph docPara, Inline inline)
    {
        if (inline is System.Windows.Documents.Run run)
        {
            string text = run.Text;
            if (string.IsNullOrEmpty(text))
                return;

            var formatting = new Formatting();
            if (
                run.FontWeight == FontWeights.Bold
                || run.FontWeight == FontWeights.ExtraBold
                || run.FontWeight == FontWeights.Black
                || run.FontWeight == FontWeights.SemiBold
            )
                formatting.Bold = true;

            if (run.FontStyle == FontStyles.Italic || run.FontStyle == FontStyles.Oblique)
                formatting.Italic = true;

            if (run.TextDecorations.Count > 0)
            {
                foreach (var decoration in run.TextDecorations)
                {
                    if (decoration.Location == TextDecorationLocation.Underline)
                    {
                        formatting.UnderlineStyle = UnderlineStyle.singleLine;
                    }
                    else if (decoration.Location == TextDecorationLocation.Strikethrough)
                    {
                        formatting.StrikeThrough = StrikeThrough.strike;
                    }
                }
            }

            if (run.FontSize > 0 && !double.IsNaN(run.FontSize))
                formatting.Size = run.FontSize * 0.75;

            if (run.FontFamily != null)
                formatting.FontFamily = new Font(run.FontFamily.Source);

            if (run.Foreground is SolidColorBrush fgBrush)
            {
                formatting.FontColor = new Xceed.Drawing.Color(fgBrush.Color.ToSKColor());
            }

            if (run.Background is SolidColorBrush bgBrush && bgBrush.Color.A > 0)
            {
                var color = bgBrush.Color;
                formatting.Highlight = ConvertToHighlight(color);
            }

            if (run.BaselineAlignment == BaselineAlignment.Subscript)
                formatting.Script = Script.subscript;
            else if (run.BaselineAlignment == BaselineAlignment.Superscript)
                formatting.Script = Script.superscript;

            docPara.Append(text, formatting);
        }
        else if (inline is Span span)
        {
            foreach (Inline childInline in span.Inlines)
                ProcessInline(doc, docPara, childInline);
        }
        else if (inline is LineBreak)
        {
            docPara.AppendLine();
        }
        else if (inline is System.Windows.Documents.Hyperlink hyperlink)
        {
            string text = new TextRange(hyperlink.ContentStart, hyperlink.ContentEnd).Text;
            if (hyperlink.NavigateUri != null)
            {
                var formatting = new Formatting();
                if (hyperlink.Foreground is SolidColorBrush fgBrush)
                {
                    formatting.FontColor = new Xceed.Drawing.Color(fgBrush.Color.ToSKColor());
                }

                if (hyperlink.TextDecorations.Count > 0)
                    formatting.UnderlineStyle = UnderlineStyle.singleLine;

                Xceed.Document.NET.Hyperlink link = doc.AddHyperlink(text, hyperlink.NavigateUri);
                docPara.AppendHyperlink(link);
            }
            else
            {
                docPara.Append(text);
            }
        }
        else if (inline is InlineUIContainer inlineContainer)
        {
            if (inlineContainer.Child is System.Windows.Controls.Image image)
            {
                ProcessInlineImage(doc, docPara, image);
            }
        }
    }

    private static void ProcessList(DocX doc, System.Windows.Documents.List list)
    {
        bool isNumbered =
            list.MarkerStyle == TextMarkerStyle.Decimal
            || list.MarkerStyle == TextMarkerStyle.LowerLatin
            || list.MarkerStyle == TextMarkerStyle.UpperLatin
            || list.MarkerStyle == TextMarkerStyle.LowerRoman
            || list.MarkerStyle == TextMarkerStyle.UpperRoman;

        Xceed.Document.NET.List? docList = null;
        int startIndex = list.StartIndex;

        foreach (ListItem listItem in list.ListItems)
        {
            string itemText = "";
            var tempDoc = DocX.Create(new MemoryStream());
            var tempPara = tempDoc.InsertParagraph();

            foreach (Block block in listItem.Blocks)
            {
                if (block is System.Windows.Documents.Paragraph para)
                {
                    foreach (Inline inline in para.Inlines)
                        ProcessInline(doc, tempPara, inline);
                }
            }

            itemText = tempPara.Text;
            if (docList == null)
            {
                if (isNumbered)
                    docList = doc.AddList(itemText, 0, ListItemType.Numbered, startIndex);
                else
                    docList = doc.AddList(itemText, 0, ListItemType.Bulleted);
            }
            else
            {
                doc.AddListItem(docList, itemText);
            }
        }

        if (docList != null)
            doc.InsertList(docList);
    }

    private static void ProcessTable(DocX doc, System.Windows.Documents.Table table)
    {
        if (table.RowGroups.Count == 0)
            return;

        int rowCount = 0;
        int colCount = 0;

        foreach (TableRowGroup rowGroup in table.RowGroups)
        {
            rowCount += rowGroup.Rows.Count;
            if (rowGroup.Rows.Count > 0)
            {
                colCount = Math.Max(colCount, rowGroup.Rows[0].Cells.Count);
            }
        }

        if (rowCount == 0 || colCount == 0)
            return;

        var docTable = doc.AddTable(rowCount, colCount);
        if (
            table.BorderThickness.Left > 0
            || table.BorderThickness.Right > 0
            || table.BorderThickness.Top > 0
            || table.BorderThickness.Bottom > 0
        )
        {
            var borderColor = new Xceed.Drawing.Color(System.Drawing.Color.Black.ToSKColor());
            docTable.SetBorder(
                TableBorderType.InsideH,
                new Xceed.Document.NET.Border(
                    BorderStyle.Tcbs_single,
                    BorderSize.one,
                    0,
                    borderColor
                )
            );
            docTable.SetBorder(
                TableBorderType.InsideV,
                new Xceed.Document.NET.Border(
                    BorderStyle.Tcbs_single,
                    BorderSize.one,
                    0,
                    borderColor
                )
            );
            docTable.SetBorder(
                TableBorderType.Top,
                new Xceed.Document.NET.Border(
                    BorderStyle.Tcbs_single,
                    BorderSize.one,
                    0,
                    borderColor
                )
            );
            docTable.SetBorder(
                TableBorderType.Bottom,
                new Xceed.Document.NET.Border(
                    BorderStyle.Tcbs_single,
                    BorderSize.one,
                    0,
                    borderColor
                )
            );
            docTable.SetBorder(
                TableBorderType.Left,
                new Xceed.Document.NET.Border(
                    BorderStyle.Tcbs_single,
                    BorderSize.one,
                    0,
                    borderColor
                )
            );
            docTable.SetBorder(
                TableBorderType.Right,
                new Xceed.Document.NET.Border(
                    BorderStyle.Tcbs_single,
                    BorderSize.one,
                    0,
                    borderColor
                )
            );
        }

        int currentRow = 0;
        foreach (TableRowGroup rowGroup in table.RowGroups)
        {
            foreach (TableRow row in rowGroup.Rows)
            {
                for (
                    int cellIndex = 0;
                    cellIndex < row.Cells.Count && cellIndex < colCount;
                    cellIndex++
                )
                {
                    TableCell cell = row.Cells[cellIndex];
                    var docCell = docTable.Rows[currentRow].Cells[cellIndex];

                    if (cell.Background is SolidColorBrush bgBrush && bgBrush.Color.A > 0)
                        docCell.FillColor = new Xceed.Drawing.Color(bgBrush.Color.ToSKColor());

                    if (
                        cell.Padding.Left > 0
                        || cell.Padding.Right > 0
                        || cell.Padding.Top > 0
                        || cell.Padding.Bottom > 0
                    )
                    {
                        docCell.MarginLeft = cell.Padding.Left * 0.75;
                        docCell.MarginRight = cell.Padding.Right * 0.75;
                        docCell.MarginTop = cell.Padding.Top * 0.75;
                        docCell.MarginBottom = cell.Padding.Bottom * 0.75;
                    }

                    bool firstBlock = true;
                    foreach (Block block in cell.Blocks)
                    {
                        if (block is System.Windows.Documents.Paragraph para)
                        {
                            var cellPara = firstBlock
                                ? docCell.Paragraphs[0]
                                : docCell.InsertParagraph();
                            firstBlock = false;

                            foreach (Inline inline in para.Inlines)
                            {
                                ProcessInline(doc, cellPara, inline);
                            }

                            // Apply paragraph alignment in cell
                            if (para.TextAlignment == TextAlignment.Center)
                                cellPara.Alignment = Alignment.center;
                            else if (para.TextAlignment == TextAlignment.Right)
                                cellPara.Alignment = Alignment.right;
                        }
                    }
                }
                currentRow++;
            }
        }

        doc.InsertTable(docTable);
    }

    private static Highlight ConvertToHighlight(Color color)
    {
        if (color.A < 128)
            return Highlight.none;

        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        if (r > 0.9 && g > 0.9 && b < 0.3)
            return Highlight.yellow;
        if (r < 0.3 && g > 0.7 && b < 0.3)
            return Highlight.green;
        if (r < 0.3 && g > 0.7 && b > 0.7)
            return Highlight.cyan;
        if (r > 0.7 && g < 0.3 && b > 0.7)
            return Highlight.magenta;
        if (r < 0.3 && g < 0.3 && b > 0.7)
            return Highlight.blue;
        if (r > 0.7 && g < 0.3 && b < 0.3)
            return Highlight.red;
        if (r < 0.3 && g < 0.3 && b < 0.3)
            return Highlight.black;
        if (r > 0.8 && g > 0.8 && b > 0.8)
            return Highlight.lightGray;
        if (r > 0.5 && g > 0.5 && b > 0.5)
            return Highlight.lightGray;
        if (r < 0.5 && g < 0.5 && b < 0.5)
            return Highlight.darkGray;

        return Highlight.yellow;
    }

    private static void ProcessImage(DocX doc, System.Windows.Controls.Image image)
    {
        try
        {
            var bitmapSource = image.Source as BitmapSource;
            if (bitmapSource == null)
                return;

            byte[]? imageBytes = ConvertBitmapSourceToBytes(bitmapSource);
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            string tempImagePath = Path.Combine(
                Path.GetTempPath(),
                $"TE-temp_image_{Guid.NewGuid()}.png"
            );
            File.WriteAllBytes(tempImagePath, imageBytes);

            try
            {
                var pic = doc.AddImage(tempImagePath);
                var paragraph = doc.InsertParagraph();

                double width = image.Width;
                double height = image.Height;

                if (double.IsNaN(width) || width <= 0)
                    width = image.ActualWidth > 0 ? image.ActualWidth : bitmapSource.PixelWidth;

                if (double.IsNaN(height) || height <= 0)
                    height = image.ActualHeight > 0 ? image.ActualHeight : bitmapSource.PixelHeight;

                var picture = pic.CreatePicture((int)height, (int)width);
                paragraph.AppendPicture(picture);

                if (image.HorizontalAlignment == HorizontalAlignment.Center)
                    paragraph.Alignment = Alignment.center;
                else if (image.HorizontalAlignment == HorizontalAlignment.Right)
                    paragraph.Alignment = Alignment.right;
            }
            finally
            {
                if (File.Exists(tempImagePath))
                    File.Delete(tempImagePath);
            }
        }
        catch { }
    }

    private static void ProcessInlineImage(
        DocX doc,
        Xceed.Document.NET.Paragraph docPara,
        System.Windows.Controls.Image image
    )
    {
        try
        {
            var bitmapSource = image.Source as BitmapSource;
            if (bitmapSource == null)
                return;

            byte[]? imageBytes = ConvertBitmapSourceToBytes(bitmapSource);
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            string tempImagePath = Path.Combine(
                Path.GetTempPath(),
                $"TE-temp_image_{Guid.NewGuid()}.png"
            );
            File.WriteAllBytes(tempImagePath, imageBytes);

            try
            {
                var pic = doc.AddImage(tempImagePath);

                double width = image.Width;
                double height = image.Height;

                if (double.IsNaN(width) || width <= 0)
                    width = image.ActualWidth > 0 ? image.ActualWidth : bitmapSource.PixelWidth;

                if (double.IsNaN(height) || height <= 0)
                    height = image.ActualHeight > 0 ? image.ActualHeight : bitmapSource.PixelHeight;

                var picture = pic.CreatePicture((int)height, (int)width);
                docPara.AppendPicture(picture);
            }
            finally
            {
                if (File.Exists(tempImagePath))
                    File.Delete(tempImagePath);
            }
        }
        catch { }
    }

    private static byte[]? ConvertBitmapSourceToBytes(BitmapSource bitmapSource)
    {
        try
        {
            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            using MemoryStream stream = new();
            encoder.Save(stream);
            return stream.ToArray();
        }
        catch
        {
            try
            {
                JpegBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

                using MemoryStream stream = new();
                encoder.Save(stream);
                return stream.ToArray();
            }
            catch
            {
                return null;
            }
        }
    }
}
