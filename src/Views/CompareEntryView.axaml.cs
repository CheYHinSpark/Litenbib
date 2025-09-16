using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Litenbib.Models;
using Litenbib.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Litenbib.Views;

public partial class CompareEntryView : StyledWindow
{
    private List<List<RadioButton>> radioButtons = [];

    public CompareEntryView()
    {
        InitializeComponent();
        this.DataContext = new CompareEntryViewModel();
    }

    public CompareEntryView(List<BibtexEntry> list)
    {
        InitializeComponent();
        this.DataContext = new CompareEntryViewModel(list);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SetGrid();
    }

    private async void SetGrid()
    {
        await Task.Delay(1);

        if (DataContext is not CompareEntryViewModel vm) { return; }

        // EntryType行、CitationKey行
        MainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddTextBlock("Entry Type", 0);
        MainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        AddTextBlock("Citation Key", 1);
        // 字段名列
        HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = "field" });
        MainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto) { SharedSizeGroup = "field" });

        // 字段=>行号
        Dictionary<string, int> fieldDict = [];
        int row = 2;
        int column = 1;
        // 获取所有字段数量，顺便添加列
        foreach (var entry in vm.Entries)
        {
            HeaderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            radioButtons.Add([]);

            Button button = new()
            {
                Content = $"Entry {column}",
                Tag = "center",
            };
            button.SetValue(Grid.ColumnProperty, column);
            button.Click += ChoosingButton_Click;
            HeaderGrid.Children.Add(button);

            AddRadioButton(entry.EntryType, "Entry Type", 0, column, column == 1);
            AddRadioButton(entry.CitationKey, "Citation Key", 1, column, column == 1);

            foreach (var key in fieldDict.Keys)
            {
                if (!entry.Fields.ContainsKey(key))
                { AddRadioButton("", key, fieldDict[key], column); }
            }
            foreach (var key in entry.Fields.Keys)
            {
                if (!fieldDict.TryGetValue(key, out int value))
                {
                    value = row;
                    fieldDict[key] = value;
                    MainGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                    AddTextBlock(key, row);
                    for (int j = 1; j < column; ++j)
                    { AddRadioButton("", key, row, j); }
                    row++;
                    AddRadioButton(entry.Fields[key], key, value, column, true);
                }
                else
                { AddRadioButton(entry.Fields[key], key, value, column); }
            }
            column++;
        }
    }

    private void ChoosingButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button button) { return; }
        int col = (int)button.GetValue(Grid.ColumnProperty);
        foreach (var rb in radioButtons[col - 1])
        { rb.IsChecked = true; }
    }

    private void AddTextBlock(string s, int r, int c = 0)
    {
        TextBlock tb = new() { Text = s };
        tb.SetValue(Grid.RowProperty, r);
        tb.SetValue(Grid.ColumnProperty, c);
        MainGrid.Children.Add(tb);
    }

    private void AddRadioButton(string s, string g, int r, int c, bool isChecked = false)
    {
        RadioButton rb = new()
        {
            Content = s,
            GroupName = g,
            IsChecked = isChecked,
        };
        // 设置绑定
        Binding binding = new()
        {
            Path = "SetField",
            Mode = BindingMode.OneWayToSource,
            Converter = new DictionaryContentConverter(),
            ConverterParameter = (g, s)
        };
        rb.Bind(RadioButton.IsCheckedProperty, binding);
        // 设置RadioButton为MainGrid的子控件，并放置在指定行列
        rb.SetValue(Grid.RowProperty, r);
        rb.SetValue(Grid.ColumnProperty, c);
        MainGrid.Children.Add(rb);
        radioButtons[c - 1].Add(rb);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    { this.Close(false); }
}