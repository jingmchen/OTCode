// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Reflection;
using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.UI.Constants;

namespace OTCode.UI.Services;

public sealed class UriPaths : IUriPaths
{
    public string ThemeTemplate {get;}
    public string AccentTemplate {get;}
    public string StyleTemplate {get;}
    public string TermsCondition {get;}
    
    public UriPaths(IAppInfo appInfo)
    {
        ArgumentNullException.ThrowIfNull(appInfo);
        
        var assemblyName = typeof(UriPaths).Assembly.GetName().Name ?? $"{appInfo.Product}.UI";

        ThemeTemplate =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Themes}/{{0}}Theme.axaml";
        
        AccentTemplate =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Accents}/{{0}}Accent.axaml";
        
        StyleTemplate =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Styles}/{{0}}.axaml";
        
        TermsCondition =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Markdowns}/" +
            $"{UIConstants.Bundled.FileName.TermsConditions}";
    }
}