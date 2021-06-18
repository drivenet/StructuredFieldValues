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
        public WhatWgTestsDataAttribute(string fileNameOrMask)
        {
            FileNameOrMask = fileNameOrMask ?? throw new ArgumentNullException(nameof(fileNameOrMask));
        }

        public string FileNameOrMask { get; }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
            => Directory.EnumerateFiles("../../../../httpwg", FileNameOrMask).SelectMany(GetData);

        private static IEnumerable<object[]> GetData(string fileName)
        {
            JArray items;
            using (var file = File.OpenText(fileName))
            {
                using var reader = new JsonTextReader(file);
                items = (JArray)JToken.ReadFrom(reader);
            }

            fileName = Path.GetFileNameWithoutExtension(fileName);
            foreach (var item in items)
            {
                if (item is null)
                {
                    continue;
                }

                var name = item.Value<string>("name") ?? throw new InvalidDataException($"Missing name for test in \"{fileName}\".");
                var raw = (item.Value<JArray>("raw") ?? throw new InvalidDataException($"Missing raw value for test \"{name}\" in \"{fileName}\"."))
                    .Select(t => t.Value<string>() ?? throw new InvalidDataException($"Null raw value for test \"{name}\" in \"{fileName}\"."));
                var header = string.Join(",", raw);
                var headerTypeString = item.Value<string>("header_type") ?? throw new InvalidDataException($"Missing header type for test in \"{fileName}\".");
                if (!Enum.TryParse<FieldType>(headerTypeString, true, out var headerType))
                {
                    throw new InvalidDataException($"Invalid header type \"{headerTypeString}\" for test \"{name}\" in \"{fileName}\".");
                }

                var mustFail = item.Value<bool>("must_fail");
                var expected = item["expected"];
                var canFail = item.Value<bool>("can_fail");

                var testCase = new WhatWgTestCase(fileName, name, headerType, header, expected, mustFail, canFail);
                yield return new object[] { testCase };
            }
        }
    }
}
