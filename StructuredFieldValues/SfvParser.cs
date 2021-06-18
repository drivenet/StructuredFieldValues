using System;

namespace StructuredFieldValues
{
    public static class SfvParser
    {
        public static ParseError? Parse(ReadOnlySpan<char> source, FieldType fieldType, ref int index, out object result)
            => Rfc8941Parser.ParseField(source, fieldType, ref index, out result);

        public static ParseError? Parse(string source, FieldType fieldType, ref int index, out object result)
            => Rfc8941Parser.ParseField(source, fieldType, ref index, out result);
    }
}
