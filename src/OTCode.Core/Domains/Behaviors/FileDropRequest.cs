// Copyright (c) Tan Jing Ming. Use of this software is governed by LICENSE.md.

namespace OTCode.Core.Domains.Behaviors;

public sealed record FileDropRequest(
    string Payload,
    object? Target
);