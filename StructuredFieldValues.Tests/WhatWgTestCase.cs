using System;

using Newtonsoft.Json.Linq;

namespace StructuredFieldValues.Tests;

internal sealed class WhatWgTestCase
{
    private readonly string _groupName;
    private readonly string _name;

    public WhatWgTestCase(string groupName, string name, FieldType headerType, string header, JToken? expected, bool mustFail, bool canFail)
    {
        _groupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
        _name = name ?? throw new ArgumentNullException(nameof(name));
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

    public override string ToString() => $"\"{_name}\" in \"{_groupName}\"";
}
