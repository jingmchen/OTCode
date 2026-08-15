// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.Collections.ObjectModel;
using System.Globalization;
using OTCode.Core.Abstractions.UI;
using OTCode.UI.Utils;
using Serilog.Core;
using Serilog.Events;

namespace OTCode.UI.Services;

public sealed class ConsoleLog : IConsoleLog, ILogEventSink
{
    private const int Cap = 400;
    public ObservableCollection<string> Entries {get;} = [];

    public ConsoleLog() { }

    // ─── INTERFACE METHODS ─────────────────────
    public void Clear()
        => DispatcherHelper.PostOnUIThread(Entries.Clear);
    
    // ─── SERILOG SINK (ILogEventSink) ──────────
    public void Emit(LogEvent logEvent)
    {
        string message = logEvent.RenderMessage(CultureInfo.InvariantCulture);

        if (logEvent.Exception != null)
            message += " — " + logEvent.Exception.Message;
        
        if (string.IsNullOrWhiteSpace(message))
            return;
        
        DispatcherHelper.PostOnUIThread(() =>
        {
            Entries.Add($"{logEvent.Timestamp:HH:mm:ss}  {message}");
            while (Entries.Count > Cap)
                Entries.RemoveAt(0);
        });
    }
}