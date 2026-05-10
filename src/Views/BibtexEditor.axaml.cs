using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
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

    private bool _updatingFromEditor;
    private bool _updatingFromProperty;

    static BibtexEditor()
    {
        TextProperty.Changed.AddClassHandler<BibtexEditor>((editor, e) =>
            editor.ApplyTextFromProperty(e.NewValue as string));
        PlaceholderTextProperty.Changed.AddClassHandler<BibtexEditor>((editor, e) =>
            editor.Editor.Watermark = e.NewValue as string);
    }

    public BibtexEditor()
    {
        InitializeComponent();
        ConfigureEditor(Editor);
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
            HighlightCurrentLine = true,
            EnableHyperlinks = false,
            EnableEmailHyperlinks = false,
        };
        editor.Document.UndoStack.SizeLimit = 0;
        editor.TextArea.TextView.LineTransformers.Add(new BibtexColorizer());
        editor.TextArea.SelectionChanged += (_, _) => UpdateSelectionProperties();
        editor.TextArea.Caret.PositionChanged += (_, _) => UpdateSelectionProperties();
        editor.TextChanged += (_, _) => ApplyTextFromEditor();
        editor.GotFocus += (_, _) => SetValue(FocusEx.BindToProperty, this);
        editor.LostFocus += (_, _) => SetValue(FocusEx.BindToProperty, null);
        editor.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        editor.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
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
