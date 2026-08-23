// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Options.FileExplorer;
using OTCode.UI.Controls;
using OTCode.UI.ViewModels;

namespace OTCode.UI.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel viewModel,
        IFileExplorerItemActions itemActions,
        FileExplorerOptions options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        InitializeComponent();
        DataContext = viewModel;

        Content = new FileExplorerControl(options, itemActions, loggerFactory);
    }
}