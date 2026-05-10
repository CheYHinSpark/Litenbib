using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using AvaloniaEdit;
using Litenbib.Models;

namespace Litenbib.Views;

public partial class BibtexEditor : UserControl, IUndoRedoTextHost
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<BibtexEditor, string?>(
            nameof(Text),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectionStartProperty =
        AvaloniaProperty.Register<BibtexEditor, int>(
            nameof(SelectionStart),
            defaultBindingMode: BindingMode.OneWayToSource);

    public static readonly StyledProperty<int> SelectionEndProperty =
        AvaloniaProperty.Register<BibtexEditor, int>(
            nameof(SelectionEnd),
            defaultBindingMode: BindingMode.OneWayToSource);

    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<BibtexEditor, string?>(
            nameof(PlaceholderText),
            string.Empty);

    public static readonly StyledProperty<IBrush?> EntryTypeBrushProperty =
        AvaloniaProperty.Register<BibtexEditor, IBrush?>(nameof(EntryTypeBrush));

    public static readonly StyledProperty<IBrush?> CitationKeyBrushProperty =
        AvaloniaProperty.Register<BibtexEditor, IBrush?>(nameof(CitationKeyBrush));

    public static readonly StyledProperty<IBrush?> FieldBrushProperty =
        AvaloniaProperty.Register<BibtexEditor, IBrush?>(nameof(FieldBrush));

    public static readonly StyledProperty<IBrush?> ValueBrushProperty =
        AvaloniaProperty.Register<BibtexEditor, IBrush?>(nameof(ValueBrush));

    public static readonly StyledProperty<IBrush?> PunctuationBrushProperty =
        AvaloniaProperty.Register<BibtexEditor, IBrush?>(nameof(PunctuationBrush));

    public static readonly StyledProperty<IBrush?> CommentBrushProperty =
        AvaloniaProperty.Register<BibtexEditor, IBrush?>(nameof(CommentBrush));

    private readonly BibtexColorizer _colorizer = new();
    private bool _updatingFromEditor;
    private bool _updatingFromProperty;

    static BibtexEditor()
    {
        TextProperty.Changed.AddClassHandler<BibtexEditor>((editor, e) =>
            editor.ApplyTextFromProperty(e.NewValue as string));
        PlaceholderTextProperty.Changed.AddClassHandler<BibtexEditor>((editor, e) =>
            editor.Editor.Watermark = e.NewValue as string);
        EntryTypeBrushProperty.Changed.AddClassHandler<BibtexEditor>((editor, _) => editor.ApplyPalette());
        CitationKeyBrushProperty.Changed.AddClassHandler<BibtexEditor>((editor, _) => editor.ApplyPalette());
        FieldBrushProperty.Changed.AddClassHandler<BibtexEditor>((editor, _) => editor.ApplyPalette());
        ValueBrushProperty.Changed.AddClassHandler<BibtexEditor>((editor, _) => editor.ApplyPalette());
        PunctuationBrushProperty.Changed.AddClassHandler<BibtexEditor>((editor, _) => editor.ApplyPalette());
        CommentBrushProperty.Changed.AddClassHandler<BibtexEditor>((editor, _) => editor.ApplyPalette());
    }

    public BibtexEditor()
    {
        InitializeComponent();
        BindPaletteResources();
        ConfigureEditor(Editor);
        ApplyTextAreaChrome();
        ActualThemeVariantChanged += (_, _) =>
        {
            ApplyPalette();
        };
        ResourcesChanged += (_, _) =>
        {
            ApplyPalette();
        };
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }

    public int SelectionEnd
    {
        get => GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, value);
    }

    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public IBrush? EntryTypeBrush
    {
        get => GetValue(EntryTypeBrushProperty);
        set => SetValue(EntryTypeBrushProperty, value);
    }

    public IBrush? CitationKeyBrush
    {
        get => GetValue(CitationKeyBrushProperty);
        set => SetValue(CitationKeyBrushProperty, value);
    }

    public IBrush? FieldBrush
    {
        get => GetValue(FieldBrushProperty);
        set => SetValue(FieldBrushProperty, value);
    }

    public IBrush? ValueBrush
    {
        get => GetValue(ValueBrushProperty);
        set => SetValue(ValueBrushProperty, value);
    }

    public IBrush? PunctuationBrush
    {
        get => GetValue(PunctuationBrushProperty);
        set => SetValue(PunctuationBrushProperty, value);
    }

    public IBrush? CommentBrush
    {
        get => GetValue(CommentBrushProperty);
        set => SetValue(CommentBrushProperty, value);
    }

    public int TextLength => Editor.Text?.Length ?? 0;

    public void SetCaretOffset(int offset)
    {
        int safeOffset = int.Clamp(offset, 0, TextLength);
        Editor.Select(safeOffset, 0);
        Editor.CaretOffset = safeOffset;
        Editor.TextArea.Caret.BringCaretToView();
        UpdateSelectionProperties();
    }

    private void ConfigureEditor(TextEditor editor)
    {
        editor.Options = new TextEditorOptions
        {
            AcceptsTab = true,
            ConvertTabsToSpaces = false,
            IndentationSize = 4,
            HighlightCurrentLine = false,
            EnableHyperlinks = false,
            EnableEmailHyperlinks = false,
        };
        editor.Document.UndoStack.SizeLimit = 0;
        ApplyPalette(redraw: false);
        editor.TextArea.TextView.LineTransformers.Add(_colorizer);
        editor.TextArea.SelectionChanged += (_, _) => UpdateSelectionProperties();
        editor.TextArea.Caret.PositionChanged += (_, _) => UpdateSelectionProperties();
        editor.TextChanged += (_, _) => ApplyTextFromEditor();
        editor.GotFocus += (_, _) =>
        {
            SetValue(FocusEx.BindToProperty, this);
        };
        editor.LostFocus += (_, _) =>
        {
            SetValue(FocusEx.BindToProperty, null);
        };
        editor.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        editor.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private void BindPaletteResources()
    {
        Bind(EntryTypeBrushProperty, new DynamicResourceExtension("BibtexEntryTypeBrush"));
        Bind(CitationKeyBrushProperty, new DynamicResourceExtension("BibtexCitationKeyBrush"));
        Bind(FieldBrushProperty, new DynamicResourceExtension("BibtexFieldBrush"));
        Bind(ValueBrushProperty, new DynamicResourceExtension("BibtexValueBrush"));
        Bind(PunctuationBrushProperty, new DynamicResourceExtension("BibtexPunctuationBrush"));
        Bind(CommentBrushProperty, new DynamicResourceExtension("BibtexCommentBrush"));
    }

    private void ApplyPalette(bool redraw = true)
    {
        if (EntryTypeBrush != null) { _colorizer.EntryTypeBrush = EntryTypeBrush; }
        if (CitationKeyBrush != null) { _colorizer.CitationKeyBrush = CitationKeyBrush; }
        if (FieldBrush != null) { _colorizer.FieldBrush = FieldBrush; }
        if (ValueBrush != null) { _colorizer.ValueBrush = ValueBrush; }
        if (PunctuationBrush != null) { _colorizer.PunctuationBrush = PunctuationBrush; }
        if (CommentBrush != null) { _colorizer.CommentBrush = CommentBrush; }

        if (redraw && Editor != null)
        {
            ApplyTextAreaChrome();
            Editor.TextArea.TextView.Redraw();
        }
    }

    private void ApplyTextAreaChrome()
    {
        if (TryGetBrush("TextSelectionHighlight", out IBrush? selectionBrush))
        {
            Editor.TextArea.SelectionBrush = selectionBrush;
        }

        if (TryGetBrush("MainForeground", out IBrush? caretBrush))
        {
            Editor.TextArea.CaretBrush = caretBrush;
        }

        Editor.TextArea.SelectionForeground = null;
    }

    private bool TryGetBrush(string resourceKey, out IBrush? brush)
    {
        if (TryGetResource(resourceKey, ActualThemeVariant, out object? value) && value is IBrush resourceBrush)
        {
            brush = resourceBrush;
            return true;
        }

        brush = null;
        return false;
    }

    private void ApplyTextFromProperty(string? value)
    {
        if (_updatingFromEditor)
        {
            return;
        }

        string text = value ?? string.Empty;
        if (Editor.Text == text)
        {
            return;
        }

        _updatingFromProperty = true;
        int caretOffset = int.Clamp(Editor.CaretOffset, 0, text.Length);
        Editor.Text = text;
        SetCaretOffset(caretOffset);
        _updatingFromProperty = false;
    }

    private void ApplyTextFromEditor()
    {
        if (_updatingFromProperty)
        {
            return;
        }

        _updatingFromEditor = true;
        SetCurrentValue(TextProperty, Editor.Text);
        _updatingFromEditor = false;
        UpdateSelectionProperties();
    }

    private void UpdateSelectionProperties()
    {
        int start = int.Clamp(Editor.SelectionStart, 0, TextLength);
        int end = int.Clamp(Editor.SelectionStart + Editor.SelectionLength, 0, TextLength);
        SetCurrentValue(SelectionStartProperty, start);
        SetCurrentValue(SelectionEndProperty, end);
    }
}
