using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Litenbib.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Litenbib.Views
{
    public partial class MainWindow : StyledWindow
    {
        private MainWindowViewModel _viewModel = null!;
        private TabStripItem _draggedItem = null!;
        private Point _initialPoint;
        private double[] _tabWidths = [];
        private TabStripItem[] _tabItems = [];
        private int _draggedId = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected async override void OnInitialized()
        {
            base.OnInitialized();
            await Task.Delay(1); // 确保界面元素加载完成
            //MainTabControl.AddHandler(PointerMovedEvent, TabControl_PointerMoved, RoutingStrategies.Bubble);
            if (DataContext is MainWindowViewModel mwvm)
            {
                _viewModel = mwvm;
                await _viewModel.LoadLocalConfig();
            }
        }

        protected override async Task<bool> OnCloseButtonClicked()
        {
            // 保存本地配置文件
            await _viewModel.SaveLocalConfig();
            // 检查是否需要保存
            if (_viewModel.NeedSave)
            {
                var box = MessageBoxManager.GetMessageBoxStandard(
                    "Warning", "Some files have been edited, but not saved. Do you want to save them?",
                    ButtonEnum.YesNoCancel);
                var result = await box.ShowAsync();
                if (result == ButtonResult.Cancel)
                { return false; }
                else if (result == ButtonResult.Yes)
                { await Task.Run(() => _viewModel.SaveAllCommand); }
            }
            return true;
        }



        #region Title Bar Drag Move
        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            TitleBarGrid.IsHitTestVisible = false;
            this.BeginMoveDrag(e);
        }

        private void TitleBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
        { TitleBarGrid.IsHitTestVisible = true; }
        #endregion Title Bar Drag Move

        #region Tab Control Drag
        private void TabItem_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Control c || c.DataContext is not BibtexViewModel bvm) { return; }
            _draggedItem = c.FindAncestorOfType<TabStripItem>()!;
            _initialPoint = e.GetPosition(MainTabControl);
            // Capture the pointer to ensure we get events even when outside the control
            e.Pointer.Capture(c);

            CalculateTabItemWidths();
        }

        private void CalculateTabItemWidths()
        {
            var x = MainTabStrip.GetLogicalChildren().OfType<TabStripItem>();
            _tabWidths = new double[x.Count()];
            _tabItems = new TabStripItem[x.Count()];
            foreach (var item in x)
            {
                if (item.DataContext is not BibtexViewModel bvm) { continue; }
                int index = _viewModel.BibtexTabs.IndexOf(bvm);
                if (item == _draggedItem)
                {
                    _draggedId = index;
                    item.ZIndex = 1;
                }
                else
                {
                    item.ZIndex = 0;
                }
                _tabWidths[index] = item.Bounds.Width + item.Margin.Left + item.Margin.Right;
                _tabItems[index] = item;
            }
        }

        private void DragTabItem(double positionX)
        {
            if (_draggedItem.RenderTransform is not TranslateTransform tt)
            {
                tt = new TranslateTransform();
                _draggedItem.RenderTransform = tt;
            }
            tt.X = positionX - _initialPoint.X;
        }

        private void SwitchTabItems(int i)
        {
            // Move the dragged item
            _initialPoint = new Point(_initialPoint.X + i * _tabWidths[_draggedId + i], _initialPoint.Y);
            _viewModel.BibtexTabs.Move(_draggedId + i, _draggedId);
            // Animation
            foreach (var item in MainTabStrip.GetLogicalChildren().OfType<TabStripItem>())
            {
                if (_tabItems[_draggedId + i].DataContext == item.DataContext)
                {
                    Animation animation = new()
                    {
                        Duration = TimeSpan.FromMilliseconds(150),
                        Easing = new SineEaseOut(),
                        FillMode = FillMode.Forward,
                        Children =
                        {
                            new KeyFrame
                            {
                                Cue = new Cue(0d),
                                Setters = { new Setter(TranslateTransform.XProperty, i * _tabWidths[_draggedId]) }
                            },
                            new KeyFrame
                            {
                                Cue = new Cue(1d),
                                Setters = { new Setter(TranslateTransform.XProperty, 0d) }
                            }
                        }
                    };
                    animation.RunAsync(item);
                    _tabItems[_draggedId + i] = _tabItems[_draggedId];
                    _tabItems[_draggedId] = item;
                    break;
                }
            }
            (_tabWidths[_draggedId], _tabWidths[_draggedId + i]) = (_tabWidths[_draggedId + i], _tabWidths[_draggedId]);
            _draggedId += i;
        }

        private void TabControl_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_draggedItem == null) { return; }

            var position = e.GetPosition(MainTabStrip);

            if (_draggedId < _tabWidths.Length - 1 // not the last one
                && _initialPoint.X + _tabWidths[_draggedId + 1] / 2 + 1.0 < position.X)
            { SwitchTabItems(1); }
            else if (_draggedId > 0
                && _initialPoint.X - _tabWidths[_draggedId - 1] / 2 - 1.0 > position.X)
            { SwitchTabItems(-1); }
            DragTabItem(position.X);
        }

        private void TabItem_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            Animation animation = new()
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Easing = new SineEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        Setters = { new Setter(TranslateTransform.XProperty, 0d) }
                    }
                }
            };
            animation.RunAsync(_draggedItem);

            _draggedItem = null!;
            e.Pointer.Capture(null); // Release the pointer capture
        }
        #endregion Tab Control Drag
    }
}