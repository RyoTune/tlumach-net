// <copyright file="IcuFragment.cs" company="Allied Bits Ltd.">
//
// Copyright 2025 Allied Bits Ltd.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tlumach.Base
{
    internal static class IcuFragment
    {
        /// <summary>
        /// Tries to parse the ICU-compatible placeholder and produce a text result.
        /// </summary>
        /// <param name="content">the content to parse.</param>
        /// <param name="value">the value of the variable for the given placeholder name.</param>
        /// <param name="getParamValueFunc">the function that returns the value of the placeholder by its name or index. It is used for inner placeholders with no index, so the index passed to it is always -1.</param>
        /// <param name="culture">the culture to use in conversions.</param>
        /// <param name="pluralCategory">an optional function that determines to what numeric category (zero, one, few, many, other) the value falls.</param>
        /// <returns>the resulting string or <see langword="null"/> on failure. If <see langword="null"/> is returned, the caller uses other means of formatting.</returns>
        /// <exception cref="TemplateParserException">thrown if an error or an unsupported ICU feature is detected.</exception>
        public static string? Evaluate(string content, object value, Func<string, int, object?> getParamValueFunc, CultureInfo? culture = null, Func<decimal, CultureInfo?, string>? pluralCategory = null)
        {
            content = content.Trim();

            var reader = new Reader(content);
            var name = reader.ReadIdentifier(out bool simpleIdentifier);

            if (simpleIdentifier)
                return value.ToString() ?? string.Empty;

            return EvaluateNoName(content.Substring(name.Length), value, getParamValueFunc, culture, pluralCategory);
        }

        /// <summary>
        /// Tries to parse the ICU-compatible tail of a placeholder and produce a text result.
        /// </summary>
        /// <param name="content">the tail to parse. The tail is everything after the placeholder name, which the caller deals with.</param>
        /// <param name="value">the value of the variable for the given placeholder name.</param>
        /// <param name="getParamValueFunc">the function that returns the value of the placeholder by its name or index. It is used for inner placeholders with no index, so the index passed to it is always -1.</param>
        /// <param name="culture">the culture to use in conversions.</param>
        /// <param name="pluralCategory">an optional function that determines to what numeric category (zero, one, few, many, other) the value falls.</param>
        /// <returns>the resulting string or <see langword="null"/> on failure. If <see langword="null"/> is returned, the caller uses other means of formatting.</returns>
        /// <exception cref="TemplateParserException">thrown if an error or an unsupported ICU feature is detected.</exception>
        internal static string? EvaluateNoName(string content, object value, Func<string, int, object?> getParamValueFunc, CultureInfo? culture = null, Func<decimal, CultureInfo?, string>? pluralCategory = null)
        {
            string? result; // null will indicate failure

            culture ??= CultureInfo.InvariantCulture;
            pluralCategory ??= SimplePluralCategory; // swap with a CLDR-aware resolver later

            content = content.Trim();

            // Try to parse: " , <kind> , <body>"
            // skip the name as we have one
            var reader = new Reader(content);

            reader.SkipWs();
            if (!reader.TryReadChar(',')) // Not ICU pattern; treat as simple`
                return null;

            reader.SkipWs();
            var kind = reader.ReadIdentifier(out _).ToLowerInvariant();
            reader.SkipWs();

            if (!reader.TryReadChar(','))
                throw new TemplateParserException("ICU fragment: expected second comma");

            reader.SkipWs();

            if (kind == "select")
            {
                var options = ReadOptions(reader);
                var key = value != null ? value.ToString()! : "other";

                if (!options.TryGetValue(key, out var chosen) && !options.TryGetValue("other", out chosen))
                    throw new TemplateParserException("ICU select: missing 'other' branch in 'select'");

                return RenderText(chosen, getParamValueFunc);
            }
            else
            if (kind == "plural")
            {
                // plural header can contain things like: offset:1
                int offset = 0;

                if (reader.Peek() != '{')
                {
                    // read tokens until '{'
                    while (true)
                    {
                        reader.SkipWs();
                        if (reader.Peek() == '{')
                            break;

                        var tok = reader.ReadIdentifier(out _);
                        reader.SkipWs();

                        if (tok.Equals("offset", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!reader.TryReadChar(':'))
                                throw new TemplateParserException("plural: expected ':' after offset");

                            var offNum = reader.ReadNumberToken(); // integer
                            offset = int.Parse(offNum, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            throw new TemplateParserException($"plural: unexpected token '{tok}'");
                        }

                        reader.SkipWs();
                    }
                }

                var options = ReadOptions(reader);

                if (!TryGetNumeric(value, out var n))
                    n = 0m;

                // exact match first (=n)
                if (options.TryGetValue("=" + n.ToString(CultureInfo.InvariantCulture), out var exact))
                    return RenderPluralText(exact, n, offset, value, getParamValueFunc, culture);

                var cat = pluralCategory(n, culture); // "one", "few", etc. (here: zero/one/other)

                if (!options.TryGetValue(cat, out var chosen) && !options.TryGetValue("other", out chosen))
                    throw new TemplateParserException("ICU plural: missing 'other' branch");

                return RenderPluralText(chosen, n, offset, value, getParamValueFunc, culture);
            }
            else
            {
                throw new TemplateParserException($"ICU kind '{kind}' not supported (supported: select, plural)");
            }
        }

        private static string RenderPluralText(string template, decimal n, int offset,
            object? value, Func<string, int, object?> getParamValueFunc, CultureInfo culture)
        {
            // Replace '#' with (n - offset) using culture
            var number = n - offset;
            var replaced = template.Replace("#", number.ToString(culture));
            return RenderText(replaced, getParamValueFunc);
        }

        // Very small {name} expander for nested simple placeholders inside branch text.
        // Escaping/advanced ICU nesting is intentionally out of scope for this subset.
        private static string RenderText(string s, Func<string, int, object?> getParamValueFunc)
        {
            var sb = new StringBuilder();
            int i = 0;

            while (i < s.Length)
            {
                if (s[i] == '{')
                {
                    int start = i + 1;
                    int openBraceCount = 1;

                    i++;

                    while (i < s.Length && openBraceCount > 0)
                    {
                        if (s[i] == '{')
                            openBraceCount++;
                        else
                        if (s[i] == '}')
                            openBraceCount--;
                        i++;
                    }

                    if (openBraceCount != 0)
                        throw new TemplateParserException("Unbalanced braces in branch");

                    var inner = s.Substring(start, (i - 1) - start).Trim();

                    object? val = null;

                    if (IsSimpleIdentifier(inner))
                        val = getParamValueFunc(inner, -1);

                    // Only support simple identifiers inside branch text
                    if (val is not null)
                        sb.Append(val.ToString());
                    else
                        sb.Append(string.Empty);

                    i--; // i points at '}' right now; loop will ++
                }
                else
                {
                    sb.Append(s[i]);
                }

                i++;
            }

            return sb.ToString();
        }

        private static Dictionary<string, string> ReadOptions(Reader reader)
        {
            // Grammar: '{' (key '{' text '}' )+ '}'
            reader.SkipWs();
            if (!reader.TryReadChar('{'))
                throw new TemplateParserException("Expected '{' to start options");

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            while (true)
            {
                reader.SkipWs();
                if (reader.Peek() == '}')
                {
                    reader.ReadChar();
                    break;
                }

                var key = reader.ReadOptionKey(); // e.g., "one", "other", "=2"
                reader.SkipWs();

                var text = reader.ReadBracedText(); // reads '{...}' and returns inner
                result[key] = text;
                reader.SkipWs();
            }

            return result;
        }

        private static bool TryGetNumeric(object value, out decimal n)
        {
            n = 0;

            switch (value)
            {
                case byte b: n = b; return true;
                case sbyte sb: n = sb; return true;
                case short s: n = s; return true;
                case ushort us: n = us; return true;
                case int i: n = i; return true;
                case uint ui: n = ui; return true;
                case long l: n = l; return true;
                case ulong ul: n = ul; return true;
                case float f: n = (decimal)f; return true;
                case double d: n = (decimal)d; return true;
                case decimal dec: n = dec; return true;
                case string str when decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out var dv):
                    n = dv; return true;
                default: return false;
            }
        }

        private static string SimplePluralCategory(decimal num, CultureInfo? _)
        {
            return num switch
            {
                0 => "zero",
                1 => "one",
                _ => "other",
            };
        }

        private static bool IsSimpleIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (!(char.IsLetter(value[0]) || value[0] == '_'))
                return false;

            for (int i = 1; i < value.Length; i++)
            {
                if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
                    return false;
            }

            return true;
        }

        private sealed class Reader
        {
            private readonly string _sourceText;

            private int _pointer;

            public Reader(string sourceText)
            {
                _sourceText = sourceText;
                _pointer = 0;
            }

            public char Peek() => _pointer < _sourceText.Length ? _sourceText[_pointer] : '\0';

            public char ReadChar() => _pointer < _sourceText.Length ? _sourceText[_pointer++] : '\0';

            public void SkipWs()
            {
                while (_pointer < _sourceText.Length && char.IsWhiteSpace(_sourceText[_pointer]))
                    _pointer++;
            }

            public bool TryReadChar(char c)
            {
                if (Peek() == c)
                {
                    _pointer++;
                    return true;
                }

                return false;
            }

            public string ReadIdentifier(out bool simpleIdentifier)
            {
                SkipWs();
                int start = _pointer;

                if (_pointer >= _sourceText.Length || !(char.IsLetter(_sourceText[_pointer]) || _sourceText[_pointer] == '_'))
                    throw new TemplateParserException("Expected identifier");

                _pointer++;

                while (_pointer < _sourceText.Length && (char.IsLetterOrDigit(_sourceText[_pointer]) || _sourceText[_pointer] == '_'))
                    _pointer++;
                simpleIdentifier = _pointer == _sourceText.Length;

                return _sourceText.Substring(start, _pointer - start);
            }

            public string ReadNumberToken()
            {
                SkipWs();
                int start = _pointer;

                if (_pointer < _sourceText.Length && (_sourceText[_pointer] == '+' || _sourceText[_pointer] == '-'))
                    _pointer++;

                bool any = false;

                while (_pointer < _sourceText.Length && char.IsDigit(_sourceText[_pointer]))
                {
                    _pointer++;
                    any = true;
                }

                if (!any)
                    throw new TemplateParserException("Expected number");

                return _sourceText.Substring(start, _pointer - start);
            }

            public string ReadOptionKey()
            {
                SkipWs();
                if (Peek() == '=')
                {
                    ReadChar();

                    var num = ReadNumberToken();
                    return "=" + num;
                }

                return ReadIdentifier(out _).ToLowerInvariant();
            }

            public string ReadBracedText()
            {
                SkipWs();
                if (!TryReadChar('{'))
                    throw new TemplateParserException("Expected '{' starting branch text");

                int depth = 1;
                int start = _pointer;

                while (_pointer < _sourceText.Length && depth > 0)
                {
                    if (_sourceText[_pointer] == '{')
                        depth++;
                    else
                    if (_sourceText[_pointer] == '}')
                        depth--;
                    _pointer++;
                }

                if (depth != 0)
                    throw new TemplateParserException("Unbalanced braces in branch text");

                // remove outer braces
                return _sourceText.Substring(start, (_pointer - 1) - start);
            }
        }
    }
}
