// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Options.FileExplorer;
using OTCode.UI.Controls;
using OTCode.UI.ViewModels.Services;

namespace OTCode.UI.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(
        FileExplorerViewModel viewModel,
        IFilePickerService filePicker)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(filePicker);

        InitializeComponent();
        DataContext = viewModel;

        var options = new FileExplorerOptions().SanitizeValidate();

        Content = new FileExplorerControl(viewModel, filePicker, options.Panel);
    }
}