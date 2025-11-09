// <copyright file="TomlParser.cs" company="Allied Bits Ltd.">
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
using System.Text;

namespace Tlumach.Base
{
    /// <summary>
    /// The parser for TOML translation files.
    /// </summary>
    public class TomlParser : KeyValueTextParser
    {
        private enum StringMarker
        {
            Unknown,
            Basic,
            MultilineBasic,
            Literal,
            MultilineLiteral,
        }

        private bool _keyIsQuoted;

        private StringMarker _lastStartOfValue = StringMarker.Unknown;

        static TomlParser()
        {
            FileFormats.RegisterConfigParser(".tomlcfg", Factory);
            FileFormats.RegisterParser(".toml", Factory);
        }

        /// <summary>
        /// Initializes the parser class, making it available for use.
        /// </summary>
        public static void Use() { }

        public override bool CanHandleExtension(string fileExtension)
        {
            return ".toml".Equals(fileExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static BaseFileParser Factory() => new IniParser();

        protected override bool IsValidKeyChar(char value)
        {
            if (!_keyIsQuoted)
                return base.IsValidKeyChar(value);
            else
                return value != Utils.C_DOUBLE_QUOTE && value != '\n' && value != '\r';
        }

#pragma warning disable CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.
        protected override bool IsStartOfKey(string content, int offset)
        {
            if (content[offset] == Utils.C_DOUBLE_QUOTE)
            {
                _keyIsQuoted = true;
                return true;
            }
            else
            if (content[offset] == '_' || content[offset] == '-' || content[offset] == '.' || char.IsLetter(content[offset]))
            {
                _keyIsQuoted = false;
                return true;
            }

            return false;
        }

        protected override bool IsEndOfKey(string content, int offset, out int newPosition)
        {
            if (_keyIsQuoted)
            {
                if (content[offset] == Utils.C_DOUBLE_QUOTE)
                {
                    newPosition = offset + 1;
                    return true;
                }

                newPosition = offset;
                return false;
            }
            else
            {
                newPosition = offset;
                return char.IsWhiteSpace(content[offset]) || IsSeparatorChar(content[offset]);
            }
        }

        protected override string UnwrapKey(string value)
        {
            return (!_keyIsQuoted || value.Length < 2)
                ? value
                : value.Substring(1, value.Length - 2);
        }
#pragma warning restore CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.

        protected override bool IsSeparatorChar(char candidate) => candidate == '=';

        protected override bool IsStartOfValue(string content, int offset)
        {
            if (content is null)
                return false;

            if (content[offset] == Utils.C_SINGLE_QUOTE)
            {
                if ((offset <= content.Length - 3) && content[offset + 1] == Utils.C_SINGLE_QUOTE && content[offset + 2] == Utils.C_SINGLE_QUOTE)
                {
                    _lastStartOfValue = StringMarker.MultilineLiteral;
                    return true;
                }

                _lastStartOfValue = StringMarker.Literal;
                return true;
            }

            if (content[offset] == Utils.C_DOUBLE_QUOTE)
            {
                if ((offset <= content.Length - 3) && content[offset + 1] == Utils.C_DOUBLE_QUOTE && content[offset + 2] == Utils.C_DOUBLE_QUOTE)
                {
                    _lastStartOfValue = StringMarker.MultilineBasic;
                    return true;
                }

                _lastStartOfValue = StringMarker.Basic;
                return true;
            }

            return false;
        }

        protected override bool? IsEndOfValue(string content, int offset, out int newPosition)
        {
            newPosition = offset;
            if (content is null)
                return false;

            if (_lastStartOfValue == StringMarker.MultilineLiteral)
            {
                if (content[offset] == Utils.C_SINGLE_QUOTE && (offset <= content.Length - 3) && content[offset + 1] == Utils.C_SINGLE_QUOTE && content[offset + 2] == Utils.C_SINGLE_QUOTE)
                {
                    newPosition = offset + 3;
                    return true;
                }

                return false;
            }

            if (_lastStartOfValue == StringMarker.Literal)
            {
                if (content[offset] == Utils.C_SINGLE_QUOTE)
                {
                    newPosition = offset + 1;
                    return true;
                }

                if (content[offset] == '\n')
                {
                    return null;
                }

                return false;
            }

            if (_lastStartOfValue == StringMarker.MultilineBasic)
            {
                if (content[offset] == Utils.C_DOUBLE_QUOTE)
                {
                    if (offset > 0 && content[offset - 1] == Utils.C_BACKSLASH) // the quote is escaped, we skip it
                        return false;

                    if (content[offset] == Utils.C_DOUBLE_QUOTE && (offset <= content.Length - 3) && content[offset + 1] == Utils.C_DOUBLE_QUOTE && content[offset + 2] == Utils.C_DOUBLE_QUOTE)
                    {
                        newPosition = offset + 3;
                        return true;
                    }
                }

                return false;
            }

            if (_lastStartOfValue == StringMarker.Basic)
            {
                if (content[offset] == Utils.C_DOUBLE_QUOTE)
                {
                    if (offset > 0 && content[offset - 1] == Utils.C_BACKSLASH) // the quote is escaped, we skip it
                        return false;

                    // we have found a closing quote
                    newPosition = offset + 1;
                    return true;
                }

                if (content[offset] == '\n')
                {
                    return null;
                }

                return false;
            }

            return false;
        }

#pragma warning disable CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.
        protected override (string? escaped, string unescaped) UnwrapValue(string value)
        {
            switch (_lastStartOfValue)
            {
                case StringMarker.Basic:
                case StringMarker.MultilineBasic:

                    string basicToReturn = (_lastStartOfValue == StringMarker.Basic) ? value.Substring(1, value.Length - 2) : value.Substring(3, value.Length - 6);

                    if (GetTemplateEscapeMode() != TemplateStringEscaping.None)
                        return (basicToReturn, Utils.UnescapeString(basicToReturn));
                    else
                        return (null, basicToReturn);

                case StringMarker.Literal:
                case StringMarker.MultilineLiteral:
                    string literalToReturn = (_lastStartOfValue == StringMarker.Literal) ? value.Substring(1, value.Length - 2) : value.Substring(3, value.Length - 6);
                    return (null, literalToReturn);

                default:
                    return (null, value);
            }
        }
#pragma warning restore CA1062 // In externally visible method, validate parameter is non-null before using it. If appropriate, throw an 'ArgumentNullException' when the argument is 'null'.

    }
}
