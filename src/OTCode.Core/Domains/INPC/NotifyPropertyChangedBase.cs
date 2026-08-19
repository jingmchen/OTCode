// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OTCode.Core.Domains.INPC;

/// <summary>
/// Using hand-rolled INPC here to keep Core dependency-free.
/// Theres a cleaner alternative but it requires more code and abstractions to be implemented
/// If the scale of this application expands, I will consider it
/// </summary>
public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        OnPropertyChanged(propertyName);
        
        return true;
    }
}