// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using MacroCanvas.Core.Models;
using MacroCanvas.Core.Options;

namespace MacroCanvas.UI.Controls;

public sealed partial class FileExplorerControl : UserControl
{
    public FileExplorerPanelOptions PanelOptions {get;}
    private TreeView? _tree;
    private bool _isCtrlHeld;

    public FileExplorerControl() : this(new FileExplorerPanelOptions()) { }

    public FileExplorerControl(FileExplorerPanelOptions panelOptions)
    {
        PanelOptions = panelOptions.Validated();

        InitializeComponent();

        _tree = this.FindControl<TreeView>("FileExplorerTree");
    }
}