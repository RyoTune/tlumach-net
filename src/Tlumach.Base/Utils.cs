using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tlumach.Base
{
    /// <summary>
    /// Contains helper functions, usable across the library.
    /// </summary>
    public static class Utils
    {
        public static string? ReadFileFromDisk(string fileName)
        {
            try
            {
                using var reader = File.OpenText(fileName);
                if (reader is null)
                    return null;
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        public static DateTime ParseDate(string date, string format)
        {
            if (date == null)
                return System.DateTime.MinValue;

            try
            {
                if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime result))
                    return result;
                else
                    return DateTime.MinValue;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        public static DateTime? ParseDateISO8601(string? date)
        {
            const string DATE_FORMAT_ISO_8601 = "yyyy-MM-dd'T'HH:mm:ss'Z'";
            const string DATE_FORMAT_ISO_8601_WITH_MS = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

            if (string.IsNullOrEmpty(date))
                return null;

            DateTime result = ParseDate(date!, DATE_FORMAT_ISO_8601_WITH_MS);
            if (result == DateTime.MinValue)
                result = ParseDate(date!, DATE_FORMAT_ISO_8601);
            if (result == DateTime.MinValue)
                return null;
            return result;
        }

        /// <summary>
        /// Decodes escaping used in JSON and TOML strings.
        /// </summary>
        /// <param name="value">original string to decode.</param>
        /// <returns>a decoded string.</returns>
        public static string UnescapeString(string value)
        {
            StringBuilder builder = new(value.Length);
            int charCode;
            char nextChar;
            int i = 0;
            while (i < value.Length)
            {
                char c = value[i];
                if (c == '\\')
                {
                    i++;
                    if (i < value.Length)
                    {
                        nextChar = value[i];
                        switch (nextChar)
                        {
                            case '"':
                                builder.Append('"'); break;
                            case '\\':
                                builder.Append('\\'); break;
                            case '/':
                                builder.Append('/'); break;
                            case 'b':
                                builder.Append('\b'); break;
                            case 'f':
                                builder.Append('\f'); break;
                            case 'n':
                                builder.Append('\n'); break;
                            case 'r':
                                builder.Append('\r'); break;
                            case 't':
                                builder.Append('\t'); break;
                            case 'u':
                                if (i + 4 < value.Length)
                                {
                                    string hex = value.Substring(i + 1, 4);
                                    try
                                    {
                                        charCode = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                                        builder.Append((char)(charCode));
                                        i += 4;
                                    }
                                    catch (FormatException)
                                    {
                                        // Invalid sequence
                                        builder.Append('\\').Append('u').Append(hex);
                                    }
                                }
                                else
                                {
                                    // Incomplete sequence
                                    builder.Append('\\').Append('u');
                                }

                                break;

                            default:
                                builder.Append('\\').Append(nextChar);
                                break;
                        }
                    }
                }
                else
                {
                    builder.Append(c);
                }

                i++;
            }

            return builder.ToString();
        }
    }
}
