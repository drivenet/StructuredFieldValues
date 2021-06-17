namespace StructuredFieldValues.Tests
{
    internal static class StringEncoder
    {
        private const string Base32EncodeMap = "QAZ2WSX3EDC4RFV5TGB6YHN7UJM8K9LP";

        public static string ToBase32String(byte[] buffer)
        {
            var length = buffer.Length;
            var resultLength = ((length * 8) + 4) / 5;
            var result = new char[resultLength];
            var hi = 5;
            for (int index = 0, lastIndex = length, resultIndex = 0; index < lastIndex;)
            {
                int code;
                if (hi > 8)
                {
                    code = buffer[index++] >> (hi - 5);
                    if (index != buffer.Length)
                    {
                        code |= unchecked((byte)(buffer[index] << (16 - hi)) >> 3);
                    }

                    hi -= 3;
                }
                else
                {
                    if (hi == 8)
                    {
                        code = buffer[index++] >> 3;
                        hi -= 3;
                    }
                    else
                    {
                        code = unchecked((byte)(buffer[index] << (8 - hi)) >> 3);
                        hi += 5;
                    }
                }

                result[resultIndex++] = Base32EncodeMap[code];
            }

            return new(result);
        }
    }
}
