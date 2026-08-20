// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Abstractions.UI;

public interface IResourceUriProvider
{
    string AccentTemplate {get;}
    string IconTemplate {get;}
    string MarkdownTemplate {get;}
    string StyleTemplate {get;}
    string ThemeTemplate {get;}
    string TermsConditionsMarkdown {get;}
}