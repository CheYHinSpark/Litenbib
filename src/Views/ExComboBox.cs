using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Litenbib.Views
{
    /// <summary>
    /// A simplified drop-down list control.
    /// </summary>
    [TemplatePart("PART_Popup", typeof(ExPopup))]
    public class ExComboBox : SelectingItemsControl
    {
        private ExPopup? _popup;

        /// <summary>
        /// Defines the IsDropDownOpen property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDropDownOpenProperty =
            AvaloniaProperty.Register<ExComboBox, bool>(nameof(IsDropDownOpen));

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown is currently open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get => GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Defines the SelectionBoxItem property.
        /// </summary>
        public static readonly DirectProperty<ExComboBox, object?> SelectionBoxItemProperty =
            AvaloniaProperty.RegisterDirect<ExComboBox, object?>(nameof(SelectionBoxItem), o => o.SelectionBoxItem);

        private object? _selectionBoxItem;

        /// <summary>
        /// Gets or sets the item to display as the control's content.
        /// </summary>
        public object? SelectionBoxItem
        {
            get => _selectionBoxItem;
            protected set => SetAndRaise(SelectionBoxItemProperty, ref _selectionBoxItem, value);
        }

        /// <summary>
        /// Defines the MaxDropDownHeight property.
        /// </summary>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<ExComboBox, double>(nameof(MaxDropDownHeight), 200); // 200 is the default value

        /// <summary>
        /// Gets or sets the maximum height for the dropdown list.
        /// </summary>
        public double MaxDropDownHeight
        {
            get => GetValue(MaxDropDownHeightProperty);
            set => SetValue(MaxDropDownHeightProperty, value);
        }

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new VirtualizingStackPanel());

        private bool _isKeySelecting = false;

        static ExComboBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ExComboBox>(DefaultPanel);
            FocusableProperty.OverrideDefaultValue<ExComboBox>(true);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _popup = e.NameScope.Get<ExPopup>("PART_Popup");
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectedItemProperty)
            {
                UpdateSelectionBoxItem(change.NewValue);
                if (IsDropDownOpen && !_isKeySelecting)
                {
                    IsDropDownOpen = false;
                }
                _isKeySelecting = false;
            }
            else if (change.Property == IsDropDownOpenProperty)
            {
                PseudoClasses.Set(":dropdownopen", change.GetNewValue<bool>());
                _isKeySelecting = false;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            // Toggle the dropdown state on a click.
            if (!e.Handled && !IsDropDownOpen && _popup != null && !_popup.IsOpen)
            {
                SetValue(IsDropDownOpenProperty, true);
                e.Handled = true;
            }

            // If an item inside the popup was clicked, select it.
            if ( e.Source is Visual visual && _popup?.IsInsidePopup(visual) == true)
            {
                if (UpdateSelectionFromEventSource(visual))
                {
                    IsDropDownOpen = false;
                    e.Handled = true;
                }
            }
        }

        private void UpdateSelectionBoxItem(object? item)
        {
            // This is a simplified approach. In a real application, you might use a template
            // or a string converter to get the display value.
            SelectionBoxItem = item;
        }

        // Add these methods back to your SimpleComboBox class
        protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new ComboBoxItem();
        }

        protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<ComboBoxItem>(item, out recycleKey);
        }


        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;
            switch (e.Key)
            {
                case Key.Down:
                    e.Handled = SelectNext();
                    break;
                case Key.Up:
                    e.Handled = SelectPrevious();
                    break;
                case Key.Enter:
                case Key.Space:
                    // If the dropdown is open, and a keypress has not been handled by an internal control, close the dropdown
                    if (IsDropDownOpen)
                    {
                        IsDropDownOpen = false;
                        e.Handled = true;
                    }
                    break;
                case Key.F4:
                case Key.Escape:
                    // Toggle the dropdown or close it
                    SetValue(IsDropDownOpenProperty, !IsDropDownOpen);
                    e.Handled = true;
                    break;
            }
        }

        private bool SelectNext() => MoveSelection(SelectedIndex, 1, false);
        private bool SelectPrevious() => MoveSelection(SelectedIndex, -1, false);

        private bool MoveSelection(int startIndex, int step, bool wrap)
        {
            static bool IsSelectable(object? o) => (o as AvaloniaObject)?.GetValue(IsEnabledProperty) ?? true;

            var count = ItemCount;

            for (int i = startIndex + step; i != startIndex; i += step)
            {
                if (i < 0 || i >= count)
                {
                    if (wrap)
                    {
                        if (i < 0)
                            i += count;
                        else if (i >= count)
                            i %= count;
                    }
                    else
                    {
                        return false;
                    }
                }

                var item = ItemsView[i];
                var container = ContainerFromIndex(i);

                if (IsSelectable(item) && IsSelectable(container))
                {
                    _isKeySelecting = true;
                    SelectedIndex = i;
                    return true;
                }
            }

            return false;
        }

    }
}
