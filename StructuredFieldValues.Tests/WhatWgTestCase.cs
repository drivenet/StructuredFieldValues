using System;

using Newtonsoft.Json.Linq;

namespace StructuredFieldValues.Tests;

internal sealed class WhatWgTestCase
{
    public WhatWgTestCase(string fileName, string name, FieldType headerType, string header, JToken? expected, bool mustFail, bool canFail)
    {
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        HeaderType = headerType;
        Header = header ?? throw new ArgumentNullException(nameof(header));
        Expected = expected;
        MustFail = mustFail;
        CanFail = canFail;
    }

    public FieldType HeaderType { get; }

    public string Header { get; }

    public JToken? Expected { get; }

    public bool MustFail { get; }

    public bool CanFail { get; }

    private string FileName { get; }

    private string Name { get; }

    public override string ToString() => $"\"{Name}\" in \"{FileName}\"";
}
