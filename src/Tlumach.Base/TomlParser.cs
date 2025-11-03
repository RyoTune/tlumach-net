using System;
using System.Collections.Generic;
using System.Text;

namespace Tlumach.Base
{
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

        private bool _keyIsQuoted = false;

        private StringMarker _lastStartOfValue = StringMarker.Unknown;

        static TomlParser()
        {
            FileFormats.RegisterConfigParser(".tomlcfg", Factory);
            FileFormats.RegisterParser(".toml", Factory);
        }

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
                return value != C_DOUBLE_QUOTE && value != '\n' && value != '\r';
        }

        protected override bool IsStartOfKey(string content, int pointer)
        {
            if (content[pointer] == C_DOUBLE_QUOTE)
            {
                _keyIsQuoted = true;
                return true;
            }
            else
            if (content[pointer] == '_' || content[pointer] == '-' || content[pointer] == '.' || char.IsLetter(content[pointer]))
            {
                _keyIsQuoted = false;
                return true;
            }

            return false;
        }

        protected override bool IsEndOfKey(string content, int pointer, out int newPosition)
        {
            if (_keyIsQuoted)
            {
                if (content[pointer] == C_DOUBLE_QUOTE)
                {
                    newPosition = pointer + 1;
                    return true;
                }

                newPosition = pointer;
                return false;
            }
            else
            {
                newPosition = pointer;
                return char.IsWhiteSpace(content[pointer]) || IsSeparatorChar(content[pointer]);
            }
        }

        protected override string UnwrapKey(string value)
        {
            return (!_keyIsQuoted || value.Length < 2)
                ? value
                : value.Substring(1, value.Length - 2);
        }

        protected override bool IsSeparatorChar(char candidate) => candidate == '=';

        protected override bool IsStartOfValue(string content, int pointer)
        {
            if (content[pointer] == C_SINGLE_QUOTE)
            {
                if ((pointer <= content.Length - 3) && content[pointer + 1] == C_SINGLE_QUOTE && content[pointer + 2] == C_SINGLE_QUOTE)
                {
                    _lastStartOfValue = StringMarker.MultilineLiteral;
                    return true;
                }

                _lastStartOfValue = StringMarker.Literal;
                return true;
            }

            if (content[pointer] == C_DOUBLE_QUOTE)
            {
                if ((pointer <= content.Length - 3) && content[pointer + 1] == C_DOUBLE_QUOTE && content[pointer + 2] == C_DOUBLE_QUOTE)
                {
                    _lastStartOfValue = StringMarker.MultilineBasic;
                    return true;
                }

                _lastStartOfValue = StringMarker.Basic;
                return true;
            }

            return false;
        }

        protected override bool? IsEndOfValue(string content, int pointer, out int newPosition)
        {
            newPosition = pointer;
            if (_lastStartOfValue == StringMarker.MultilineLiteral)
            {
                if (content[pointer] == C_SINGLE_QUOTE && (pointer <= content.Length - 3) && content[pointer + 1] == C_SINGLE_QUOTE && content[pointer + 2] == C_SINGLE_QUOTE)
                {
                    newPosition = pointer + 3;
                    return true;
                }

                return false;
            }

            if (_lastStartOfValue == StringMarker.Literal)
            {
                if (content[pointer] == C_SINGLE_QUOTE)
                {
                    newPosition = pointer + 1;
                    return true;
                }

                if (content[pointer] == '\n')
                {
                    return null;
                }

                return false;
            }

            if (_lastStartOfValue == StringMarker.MultilineBasic)
            {
                if (content[pointer] == C_DOUBLE_QUOTE)
                {
                    if (pointer > 0 && content[pointer - 1] == C_BACKSLASH) // the quote is escaped, we skip it
                        return false;

                    if (content[pointer] == C_DOUBLE_QUOTE && (pointer <= content.Length - 3) && content[pointer + 1] == C_DOUBLE_QUOTE && content[pointer + 2] == C_DOUBLE_QUOTE)
                    {
                        newPosition = pointer + 3;
                        return true;
                    }
                }

                return false;
            }

            if (_lastStartOfValue == StringMarker.Basic)
            {
                if (content[pointer] == C_DOUBLE_QUOTE)
                {
                    if (pointer > 0 && content[pointer - 1] == C_BACKSLASH) // the quote is escaped, we skip it
                        return false;

                    // we have found a closing quote
                    newPosition = pointer + 1;
                    return true;
                }

                if (content[pointer] == '\n')
                {
                    return null;
                }

                return false;
            }

            return false;
        }

        protected override string UnwrapValue(string value)
        {
            switch (_lastStartOfValue)
            {
                case StringMarker.Basic:
                    if (GetTemplateEscapeMode() != TemplateStringEscaping.None)
                        return Utils.UnescapeString(value.Substring(1, value.Length - 2));
                    else
                        return value.Substring(1, value.Length - 2);

                case StringMarker.Literal:
                    return value.Substring(1, value.Length - 2);

                case StringMarker.MultilineBasic:

                    if (GetTemplateEscapeMode() != TemplateStringEscaping.None)
                        return Utils.UnescapeString(value.Substring(3, value.Length - 6));
                    else
                        return value.Substring(3, value.Length - 6);

                case StringMarker.MultilineLiteral:
                    return value.Substring(3, value.Length - 6);

                default:
                    return value;
            }
        }
    }
}
