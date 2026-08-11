// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

using OTCode.Core.Enums;
using OTCode.Core.Domains;

namespace OTCode.Core.Exceptions;

public sealed class OTCodeException : Exception
{
    public ErrorCode Code {get;}

    public OTCodeException(ErrorCode code, string? message, Exception? inner) : base(message, inner)
        => Code = code;
}