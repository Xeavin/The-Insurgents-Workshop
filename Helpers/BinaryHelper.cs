using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Helpers
{
    public static class BinaryHelper
    {
        public static void Align(BinaryWriter bw, int alignment, bool isFilled = false)
        {
            if (bw.BaseStream.Position % alignment == 0)
            {
                return;
            }

            var size = alignment - bw.BaseStream.Position % alignment;
            if (isFilled)
            {
                var array = new byte[size];
                for (var i = 0; i < array.Length; i++)
                {
                    array[i] = 0xFF;
                }
                bw.Write(array);
            }
            else
            {
                bw.Write(new byte[size]);
            }
        }

        public static string GetEncodedStringByBytes(byte[] bytes)
        {
            return Encoding.GetEncoding("shift_jis").GetString(bytes);
        }

        public static byte[] GetBytesByEncodedString(string label)
        {
            return Encoding.GetEncoding("shift_jis").GetBytes(label);
        }
    }

    public class ByteArrayJsonConverter : JsonConverter<byte[]>
    {
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var shortArray = JsonSerializer.Deserialize<short[]>(ref reader);
            return shortArray?.Select(i => (byte)i).ToArray();
        }

        public override void Write(Utf8JsonWriter writer, byte[] array, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var value in array)
            {
                writer.WriteNumberValue(value);
            }
            writer.WriteEndArray();
        }
    }
}
