// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using System.Globalization;
using Serilog.Core;
using Serilog.Events;
using OTCode.Core.Abstractions.UI;

namespace OTCode.UI.Services;

public sealed class ConsoleLog : IConsoleLog, ILogEventSink
{
    private const int Cap = 400;
    private readonly IUIDispatcher _dispatcher;
    public ObservableCollection<string> Entries {get;} = [];

    public ConsoleLog(IUIDispatcher dispatcher)
        => _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));

    // ─── INTERFACE METHODS ─────────────────────
    public void Clear()
        => _dispatcher.Post(Entries.Clear);
    
    // ─── SERILOG SINK (ILogEventSink) ──────────
    public void Emit(LogEvent logEvent)
    {
        string message = logEvent.RenderMessage(CultureInfo.InvariantCulture);

        if (logEvent.Exception != null)
            message += " — " + logEvent.Exception.Message;
        
        if (string.IsNullOrWhiteSpace(message))
            return;
        
        _dispatcher.Post(() =>
        {
            Entries.Add($"{logEvent.Timestamp:HH:mm:ss}  {message}");
            while (Entries.Count > Cap)
                Entries.RemoveAt(0);
        });
    }
}