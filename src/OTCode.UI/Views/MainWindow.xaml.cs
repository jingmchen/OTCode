// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Windows;
using Microsoft.Extensions.Logging;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.Core.Options.FileExplorer;
using OTCode.UI.Controls;
using OTCode.UI.ViewModels;

namespace OTCode.UI.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow(
        MainWindowViewModel viewModel,
        IFileExplorerService service,
        IFileExplorerItemActions itemActions,
        FileExplorerOptions options,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(options);

        InitializeComponent();
        DataContext = viewModel;

        Content = new FileExplorerControl(service, options, itemActions, loggerFactory);
    }
}