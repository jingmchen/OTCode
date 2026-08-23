// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Domains.FileExplorer;
using OTCode.Core.Options.FileExplorer;
using OTCode.UI.Services;
using OTCode.UI.ViewModels.Services;

namespace OTCode.UI.Controls;

public sealed partial class FileExplorerControl : UserControl, IDisposable
{
    private readonly IFileExplorerService _service;
    private readonly FileExplorerViewModel _viewModel;
    private FileExplorerItem? _anchor;
    private FileExplorerItem? _pendingCollapseItem;
    private Point _pressPoint;
    private bool _disposed;
    private bool _teardownHooked;
    public FileExplorerOptions Options {get;}
    private FileExplorerViewModel? ViewModel => DataContext as FileExplorerViewModel;

    public FileExplorerControl(
        FileExplorerOptions options,
        IFileExplorerItemActions? itemActions = null,
        ILoggerFactory? loggerFactory = null)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));

        options.SanitizeValidate();

        var effectiveLoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;

        _viewModel = new FileExplorerViewModel(
            effectiveLoggerFactory.CreateLogger<FileExplorerViewModel>(),
            _service,
            itemActions ?? new FileExplorerItemActions());

        InitializeComponent();

        ApplyPanelOptions();

        DataContext = _viewModel;
        Loaded += OnLoaded;

        if (!string.IsNullOrWhiteSpace(options.Service.RootPath))
            _viewModel.LoadDirectory(options.Service.RootPath);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        Loaded -= OnLoaded;
        _viewModel.Dispose();
    }

    private void ApplyPanelOptions()
    {
        var options = Options;

        Width = options.Panel.Width;
        Height = options.Panel.Height;
        MinWidth = options.Panel.MinWidth;
        MaxWidth = options.Panel.MaxWidth;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_teardownHooked)
            return;

        _teardownHooked = true;

        if (Window.GetWindow(this) is { } window)
            window.Closed += (_, _) => Dispose();
        else
            Dispatcher.ShutdownStarted += (_, _) => Dispose();
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select root folder",
                Multiselect = false
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) == true &&
                !string.IsNullOrEmpty(dialog.FolderName))
            {
                ViewModel?.LoadDirectory(dialog.FolderName);
            }
        }
        catch (Exception exception)
        {
            // A native-picker failure must not terminate the application.
            System.Diagnostics.Debug.WriteLine(
                $"[FileExplorer] Folder picker failed: {exception}");
        }
    }

    private void OnPathBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return || sender is not TextBox textBox)
            return;

        string? path = textBox.Text?.Trim();

        if (!string.IsNullOrEmpty(path))
            ViewModel?.LoadDirectory(path);

        // Prevent the UserControl-level ConfirmOrOpenCommand from also handling Enter.
        e.Handled = true;
    }

    private void OnTreePreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel is null)
            return;
        
        if (IsWithin<TextBox>(e.OriginalSource) || IsWithin<ScrollBar>(e.OriginalSource))
            return;

        FileExplorerItem? item = ItemFrom(e.OriginalSource);

        if (e.ChangedButton == MouseButton.Right)
        {
            // Preserve an existing multi-selection when right-clicking one of its rows.
            if (item is {IsSelected: false})
            {
                ViewModel.SelectItem(item, multiSelect: false);
                _anchor = item;
            }

            return;
        }

        if (e.ChangedButton != MouseButton.Left)
            return;

        // Drag/drop may consume mouse-up, so discard stale before each new selection gesture
        _pendingCollapseItem = null;

        if (item is null)
        {
            ViewModel.ClearSelection();
            _anchor = null;
            return;
        }

        if (e.ClickCount == 2)
        {
            if (ViewModel.OpenCommand.CanExecute(item))
                ViewModel.OpenCommand.Execute(item);

            // Suppress TreeViewItem's built-in double-click expansion to avoid two toggles.
            e.Handled = true;
            return;
        }

        bool multiSelect = Options.Panel.AllowMultiSelect;
        bool controlPressed = multiSelect && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        bool shiftPressed = multiSelect && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        if (shiftPressed && _anchor is not null)
        {
            SelectRange(_anchor, item);
            return;
        }

        if (controlPressed)
        {
            if (item.IsSelected)
                ViewModel.DeselectItem(item);
            else
                ViewModel.SelectItem(item, multiSelect: true);

            _anchor = item;
            return;
        }

        if (item.IsSelected && ViewModel.SelectedItems.Count > 1)
        {
            _pendingCollapseItem = item;
            _pressPoint = e.GetPosition(ExplorerTree);
            return;
        }

        ViewModel.SelectItem(item, multiSelect: false);
        _anchor = item;
    }

    private void OnTreePreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        FileExplorerItem? item = _pendingCollapseItem;
        _pendingCollapseItem = null;

        if (ViewModel is null || item is null)
            return;

        Point releasePoint = e.GetPosition(ExplorerTree);

        bool remainedWithinDragThreshold =
            Math.Abs(releasePoint.X - _pressPoint.X) <=
                SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(releasePoint.Y - _pressPoint.Y) <=
                SystemParameters.MinimumVerticalDragDistance;

        if (remainedWithinDragThreshold &&
            ReferenceEquals(ItemFrom(e.OriginalSource), item))
        {
            ViewModel.SelectItem(item, multiSelect: false);
            _anchor = item;
        }
    }

    private void SelectRange(FileExplorerItem from, FileExplorerItem to)
    {
        if (ViewModel is null)
            return;

        List<FileExplorerItem> visibleItems = FlattenVisible();
        int firstIndex = visibleItems.IndexOf(from);
        int lastIndex = visibleItems.IndexOf(to);

        if (firstIndex < 0 || lastIndex < 0)
            return;

        if (firstIndex > lastIndex)
            (firstIndex, lastIndex) = (lastIndex, firstIndex);

        ViewModel.ClearSelection();

        for (int index = firstIndex; index <= lastIndex; index++)
            ViewModel.SelectItem(visibleItems[index], multiSelect: true);
    }

    private List<FileExplorerItem> FlattenVisible()
    {
        var visibleItems = new List<FileExplorerItem>();

        if (ViewModel is not null)
            Walk(ViewModel.RootItems);

        return visibleItems;

        void Walk(IEnumerable<FileExplorerItem> items)
        {
            foreach (FileExplorerItem item in items)
            {
                visibleItems.Add(item);

                if (item.IsDirectory &&
                    item.IsExpanded &&
                    item.Children.Count > 0)
                {
                    Walk(item.Children);
                }
            }
        }
    }

    private static FileExplorerItem? ItemFrom(object? original)
    {
        var current = original as DependencyObject;

        while (current is not null)
        {
            if (current is FrameworkElement {DataContext: FileExplorerItem item})
                return item;

            current = ParentOf(current);
        }

        return null;
    }

    private static bool IsWithin<T>(object? original) where T : DependencyObject
    {
        var current = original as DependencyObject;

        while (current is not null)
        {
            if (current is T)
                return true;

            current = ParentOf(current);
        }

        return false;
    }

    private static DependencyObject? ParentOf(DependencyObject child) =>
        child is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(child)
            : LogicalTreeHelper.GetParent(child);
}