using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Text.RegularExpressions;

namespace Litenbib.Views;

public sealed partial class BibtexColorizer : DocumentColorizingTransformer
{
    private static readonly IBrush EntryTypeBrush = Brush.Parse("#7DD3FC");
    private static readonly IBrush CitationKeyBrush = Brush.Parse("#FBBF24");
    private static readonly IBrush FieldBrush = Brush.Parse("#C084FC");
    private static readonly IBrush ValueBrush = Brush.Parse("#86EFAC");
    private static readonly IBrush PunctuationBrush = Brush.Parse("#94A3B8");
    private static readonly IBrush CommentBrush = Brush.Parse("#64748B");

    protected override void ColorizeLine(DocumentLine line)
    {
        string text = CurrentContext.Document.GetText(line);
        int lineStart = line.Offset;
        int commentIndex = FindCommentStart(text);
        int codeLength = commentIndex >= 0 ? commentIndex : text.Length;

        ColorizePunctuation(text, lineStart, codeLength);
        ColorizeEntryHeader(text, lineStart, codeLength);
        ColorizeField(text, lineStart, codeLength);
        ColorizeValue(text, lineStart, codeLength);

        if (commentIndex >= 0)
        {
            ChangePart(lineStart + commentIndex, lineStart + text.Length, CommentBrush, FontStyle.Italic);
        }
    }

    private void ColorizePunctuation(string text, int lineStart, int codeLength)
    {
        for (int i = 0; i < codeLength; i++)
        {
            char c = text[i];
            if (c is '@' or '{' or '}' or '=' or ',')
            {
                ChangePart(lineStart + i, lineStart + i + 1, PunctuationBrush);
            }
        }
    }

    private void ColorizeEntryHeader(string text, int lineStart, int codeLength)
    {
        string code = text[..codeLength];
        Match match = EntryHeaderRegex().Match(code);
        if (!match.Success)
        {
            return;
        }

        ChangeGroup(lineStart, match.Groups["type"], EntryTypeBrush, FontWeight.SemiBold);
        ChangeGroup(lineStart, match.Groups["key"], CitationKeyBrush);
    }

    private void ColorizeField(string text, int lineStart, int codeLength)
    {
        string code = text[..codeLength];
        Match match = FieldRegex().Match(code);
        if (match.Success)
        {
            ChangeGroup(lineStart, match.Groups["field"], FieldBrush, FontWeight.SemiBold);
        }
    }

    private void ColorizeValue(string text, int lineStart, int codeLength)
    {
        int equalsIndex = text.AsSpan(0, codeLength).IndexOf('=');
        if (equalsIndex < 0)
        {
            return;
        }

        int valueStart = equalsIndex + 1;
        while (valueStart < codeLength && char.IsWhiteSpace(text[valueStart]))
        {
            valueStart++;
        }

        if (valueStart >= codeLength)
        {
            return;
        }

        int valueEnd = codeLength;
        while (valueEnd > valueStart && char.IsWhiteSpace(text[valueEnd - 1]))
        {
            valueEnd--;
        }

        if (valueEnd > valueStart && text[valueEnd - 1] == ',')
        {
            valueEnd--;
        }

        while (valueEnd > valueStart && char.IsWhiteSpace(text[valueEnd - 1]))
        {
            valueEnd--;
        }

        char opener = text[valueStart];
        if (opener is '{' or '"')
        {
            char closer = opener == '{' ? '}' : '"';
            int closingIndex = text.LastIndexOf(closer, valueEnd - 1, valueEnd - valueStart);
            valueStart++;
            if (closingIndex >= valueStart)
            {
                valueEnd = closingIndex;
            }
        }

        if (valueStart < valueEnd)
        {
            ChangePart(lineStart + valueStart, lineStart + valueEnd, ValueBrush);
        }
    }

    private static int FindCommentStart(string text)
    {
        bool escaped = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '%' && !escaped)
            {
                return i;
            }

            escaped = c == '\\' && !escaped;
            if (c != '\\')
            {
                escaped = false;
            }
        }

        return -1;
    }

    private void ChangeGroup(
        int lineStart,
        Group group,
        IBrush brush,
        FontWeight? fontWeight = null,
        FontStyle? fontStyle = null)
    {
        if (group.Success && group.Length > 0)
        {
            ChangePart(lineStart + group.Index, lineStart + group.Index + group.Length, brush, fontStyle, fontWeight);
        }
    }

    private void ChangePart(
        int startOffset,
        int endOffset,
        IBrush brush,
        FontStyle? fontStyle = null,
        FontWeight? fontWeight = null)
    {
        ChangeLinePart(startOffset, endOffset, element =>
        {
            element.TextRunProperties.SetForegroundBrush(brush);
            if (fontStyle.HasValue || fontWeight.HasValue)
            {
                element.TextRunProperties.SetTypeface(new Typeface(
                    "Consolas",
                    fontStyle ?? FontStyle.Normal,
                    fontWeight ?? FontWeight.Normal,
                    FontStretch.Normal));
            }
        });
    }

    [GeneratedRegex(@"^\s*@(?<type>[A-Za-z][A-Za-z0-9_-]*)\s*\{\s*(?<key>[^,\s]+)?", RegexOptions.Compiled)]
    private static partial Regex EntryHeaderRegex();

    [GeneratedRegex(@"^\s*(?<field>[A-Za-z][A-Za-z0-9_-]*)(?=\s*=)", RegexOptions.Compiled)]
    private static partial Regex FieldRegex();
}
