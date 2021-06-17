using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xunit.Sdk;

namespace StructuredFieldValues.Tests
{
    public sealed class WhatWgTestsDataAttribute : DataAttribute
    {
        public WhatWgTestsDataAttribute(string fileName)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        }

        public string FileName { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            JArray items;
            using (var file = File.OpenText("../../../../httpwg/" + FileName))
            {
                using var reader = new JsonTextReader(file);
                items = (JArray)JToken.ReadFrom(reader);
            }

            foreach (var item in items)
            {
                if (item is null)
                {
                    continue;
                }

                var name = item.Value<string>("name") ?? throw new InvalidDataException($"Missing name for test in \"{FileName}\".");
                var raw = (item.Value<JArray>("raw") ?? throw new InvalidDataException($"Missing raw value for test \"{name}\" in \"{FileName}\"."))
                    .Select(t => t.Value<string>() ?? throw new InvalidDataException($"Null raw value for test \"{name}\" in \"{FileName}\"."));
                var header = string.Join(",", raw);
                var headerTypeString = item.Value<string>("header_type") ?? throw new InvalidDataException($"Missing header type for test in \"{FileName}\".");
                if (!Enum.TryParse<HeaderType>(headerTypeString, true, out var headerType))
                {
                    throw new InvalidDataException($"Invalid header type \"{headerTypeString}\" for test \"{name}\" in \"{FileName}\".");
                }

                var mustFail = item.Value<bool>("must_fail");
                var expected = item["expected"];
                var canFail = item.Value<bool>("can_fail");

                var testCase = new WhatWgTestCase(FileName, name, headerType, header, expected, mustFail, canFail);
                yield return new object[] { testCase };
            }
        }
    }
}
