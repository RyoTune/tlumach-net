// <copyright file="IniParser.cs" company="Allied Bits Ltd.">
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
    public class IniParser : KeyValueTextParser
    {
        protected override char LineCommentChar => ';';

        static IniParser()
        {
            FileFormats.RegisterConfigParser(".cfg", Factory);
            FileFormats.RegisterParser(".ini", Factory);
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return ".ini".Equals(fileExtension, StringComparison.OrdinalIgnoreCase);
        }

        protected override bool IsStartOfKey(string content, int pointer) => content[pointer] == '_' || char.IsLetter(content[pointer]);

        protected override bool IsEndOfKey(string content, int pointer, out int newPosition)
        {
            if (char.IsWhiteSpace(content[pointer]) || IsSeparatorChar(content[pointer]))
            {
                newPosition = pointer + 1;
                return true;
            }

            newPosition = pointer;
            return false;
        }

        protected override string UnwrapKey(string value) => value;

        protected override bool IsSeparatorChar(char candidate) => candidate == '=' || candidate == ':';

        protected override bool IsStartOfValue(string content, int pointer) => true; // In ini files, everything is a value (all non-values, such as EOL and space, are handled by TextParser)

        private static BaseFileParser Factory() => new IniParser();

        protected override bool? IsEndOfValue(string content, int pointer, out int newPosition)
        {
            newPosition = pointer;
            return content[pointer] == '\n';
        }

        protected override (string? escaped, string unescaped) UnwrapValue(string value)
        {
            if (value.Length > 2)
            {
                /*
                // Check if the value is quoted and remove quotes
                if (((value[0] == C_SINGLE_QUOTE) || (value[0] == C_DOUBLE_QUOTE)) && value.Length >= 2 && (value[value.Length - 1] == value[0]))
                {
                    if (GetTemplateEscapeMode() != TemplateStringEscaping.None)
                        return Utils.UnescapeString(value.Substring(1, value.Length - 2));
                    else
                        return value.Substring(1, value.Length - 2);
                }
                else
                */
                if (GetTemplateEscapeMode() != TemplateStringEscaping.None)
                    return (value, Utils.UnescapeString(value));
            }

            return (null, value);
        }
    }
}
