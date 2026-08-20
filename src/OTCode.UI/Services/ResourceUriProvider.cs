// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Abstractions.Infrastructure;
using OTCode.Core.Abstractions.UI;
using OTCode.UI.Constants;

namespace OTCode.UI.Services;

public sealed class ResourceUriProvider : IResourceUriProvider
{
    public string AccentTemplate {get;}
    public string IconTemplate {get;}
    public string MarkdownTemplate {get;}
    public string StyleTemplate {get;}
    public string ThemeTemplate {get;}
    public string TermsConditionsMarkdown {get;}
    
    public ResourceUriProvider(IAppInfo appInfo)
    {
        ArgumentNullException.ThrowIfNull(appInfo);
        
        var assemblyName = typeof(ResourceUriProvider).Assembly.GetName().Name;

        AccentTemplate =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Accents}/" +
            $"{UIConstants.Bundled.FileName.AccentTemplate}";
        
        IconTemplate =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Icons}/" +
            $"{UIConstants.Bundled.FileName.IconTemplate}";
        
        MarkdownTemplate =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Markdowns}/" +
            $"{UIConstants.Bundled.FileName.MarkdownTemplate}";
        
        StyleTemplate =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Styles}/" +
            $"{UIConstants.Bundled.FileName.StyleTemplate}";
        
        ThemeTemplate =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Themes}/" +
            $"{UIConstants.Bundled.FileName.ThemeTemplate}";
        
        TermsConditionsMarkdown =
            $"pack://application:,,,/{assemblyName};component/" +
            $"{UIConstants.Bundled.FolderName.Assets}/" +
            $"{UIConstants.Bundled.FolderName.Markdowns}/" +
            $"{UIConstants.Bundled.FileName.TermsConditionsMarkdown}";
    }
}