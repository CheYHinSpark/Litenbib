//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.Input;
//using Avalonia.Interactivity;
//using Avalonia.VisualTree;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Litenbib.Models
//{
//    public static class DataGridDragDropBehavior
//    {
//        // 启用拖放的附加属性
//        public static readonly AttachedProperty<bool> EnableDragDropProperty =
//            AvaloniaProperty.RegisterAttached<DataGrid, bool>("EnableDragDrop", typeof(DataGridDragDropBehavior), false, false);

//        public static bool GetEnableDragDrop(DataGrid obj) => obj.GetValue(EnableDragDropProperty);
//        public static void SetEnableDragDrop(DataGrid obj, bool value) => obj.SetValue(EnableDragDropProperty, value);

//        // 数据源的附加属性（用于操作集合）
//        public static readonly AttachedProperty<IList> ItemsSourceProperty =
//            AvaloniaProperty.RegisterAttached<DataGrid, IList>("ItemsSource", typeof(DataGridDragDropBehavior), null, false);

//        public static IList GetItemsSource(DataGrid obj) => obj.GetValue(ItemsSourceProperty);
//        public static void SetItemsSource(DataGrid obj, IList value) => obj.SetValue(ItemsSourceProperty, value);

//        private static int _dragStartIndex = -1; // 记录开始拖动的行索引
//        private static bool _isDragging = false; // 是否正在拖动
//        private static Point _startPoint; // 记录鼠标按下的起始位置

//        // 属性改变时的回调
//        static DataGridDragDropBehavior()
//        {
//            EnableDragDropProperty.Changed.AddClassHandler<DataGrid>((dataGrid, e) =>
//            {
//                if (e.NewValue is true)
//                {
//                    // 启用拖放相关事件监听
//                    dataGrid.AllowDrop = true;
//                    dataGrid.AddHandler(DataGrid.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
//                    dataGrid.AddHandler(DataGrid.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
//                    dataGrid.AddHandler(DataGrid.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
//                    dataGrid.AddHandler(DataGrid.DragDropEvent, OnDragDrop, RoutingStrategies.Tunnel);
//                }
//                else
//                {
//                    // 移除事件监听
//                    dataGrid.RemoveHandler(DataGrid.PointerPressedEvent, OnPointerPressed);
//                    dataGrid.RemoveHandler(DataGrid.PointerMovedEvent, OnPointerMoved);
//                    dataGrid.RemoveHandler(DataGrid.PointerReleasedEvent, OnPointerReleased);
//                    dataGrid.RemoveHandler(DataGrid.DragDropEvent, OnDragDrop);
//                }
//            });
//        }

//        private static void OnPointerPressed(object sender, PointerPressedEventArgs e)
//        {
//            var dataGrid = sender as DataGrid;
//            if (dataGrid == null) return;

//            var point = e.GetCurrentPoint(dataGrid);
//            if (point.Properties.IsLeftButtonPressed)
//            {
//                _startPoint = point.Position; // 记录按下的起始位置
//                _isDragging = false; // 重置拖动状态

//                // 查找点击的行
//                var hitTestResult = dataGrid.InputHitTest(point.Position);
//                if (hitTestResult is Control control)
//                {
//                    var dataGridRow = control.FindAncestorOfType<DataGridRow>();
//                    if (dataGridRow != null)
//                    {
//                        _dragStartIndex = dataGridRow.GetIndex(); // 记录起始行索引
//                        e.Handled = true;
//                    }
//                }
//            }
//        }

//        private static void OnPointerMoved(object sender, PointerEventArgs e)
//        {
//            var dataGrid = sender as DataGrid;
//            if (dataGrid == null || _dragStartIndex == -1) return;

//            var point = e.GetCurrentPoint(dataGrid);
//            if (!_isDragging && point.Properties.IsLeftButtonPressed)
//            {
//                // 计算移动距离，超过阈值才开始拖动（避免误操作）
//                var diff = point.Position - _startPoint;
//                if (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3) // 阈值可根据需要调整
//                {
//                    _isDragging = true;
//                    // 初始化拖放操作
//                    var data = new DataObject();
//                    data.Set("DragData", _dragStartIndex); // 可以传递需要的数据
//                    DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
//                }
//            }
//        }

//        private static void OnPointerReleased(object sender, PointerReleasedEventArgs e)
//        {
//            // 重置状态
//            if (!_isDragging)
//            {
//                _dragStartIndex = -1;
//                _isDragging = false;
//            }
//        }

//        private static void OnDragDrop(object sender, DragEventArgs e)
//        {
//            var dataGrid = sender as DataGrid;
//            if (dataGrid == null) return;

//            // 获取拖放的目标行
//            var point = e.GetPosition(dataGrid);
//            var hitTestResult = dataGrid.InputHitTest(point);
//            if (hitTestResult is Control control)
//            {
//                var targetRow = control.FindAncestorOfType<DataGridRow>();
//                if (targetRow != null)
//                {
//                    int targetIndex = targetRow.GetIndex();
//                    var itemsSource = GetItemsSource(dataGrid); // 获取绑定的数据源

//                    if (itemsSource != null && _dragStartIndex >= 0 && _dragStartIndex != targetIndex)
//                    {
//                        // 操作数据源：移动项目
//                        var itemToMove = itemsSource[_dragStartIndex];
//                        itemsSource.RemoveAt(_dragStartIndex);
//                        itemsSource.Insert(targetIndex, itemToMove);

//                        // 可选：刷新DataGrid的显示
//                        dataGrid.ItemsSource = null;
//                        dataGrid.ItemsSource = itemsSource;
//                    }
//                }
//            }

//            // 重置状态
//            _dragStartIndex = -1;
//            _isDragging = false;
//            e.Handled = true;
//        }
//    }
//}
