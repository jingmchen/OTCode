// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Options.FileExplorer;

public sealed class FileExplorerOptions
{
    public ServiceOptions Service {get; set;} = new();
    public PanelOptions Panel {get; set;} = new();

    public FileExplorerOptions SanitizeValidate()
    {
        Service ??= new();
        Panel ??= new();

        var service = Service;
        var panel = Panel;

        if (string.IsNullOrWhiteSpace(service.NewFileName))
            service.NewFileName = "NewFile";
        
        if (string.IsNullOrWhiteSpace(service.NewFolderName))
            service.NewFolderName = "NewFolder";
        
        if (!service.NewFileExt.StartsWith('.'))
            throw new ArgumentException(
                $"{nameof(service.NewFileExt)} must include a leading dot.");

        if (service.NewFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            service.NewFolderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("New file/folder names contain invalid path characters.");

        if (service.RootPath is not null && string.IsNullOrWhiteSpace(service.RootPath))
            throw new ArgumentException($"{nameof(service.RootPath)} must be null (no auto-load) or a non-blank path.");
        
        if (panel.MinWidth > panel.MaxWidth)
            throw new ArgumentException(
                $"{nameof(panel.MinWidth)} cannot be greater than {nameof(panel.MaxWidth)}.");
        
        if (panel.Width < panel.MinWidth || panel.Width > panel.MaxWidth)
            panel.Width = Math.Clamp(panel.Width, panel.MinWidth, panel.MaxWidth);
        
        return this;
    }
}