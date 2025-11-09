// <copyright file="KeyValueTextParser.cs" company="Allied Bits Ltd.">
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
    /// The base parser for key-value configuration and translation files (ini and toml).
    /// </summary>
    public abstract class KeyValueTextParser : BaseFileParser
    {
        private enum TextParserState
        {
            LookingForLineStart,
            SkippingTillEOL,
            CapturingKey,
            LookingForSeparator,
            LookingForValueStart,
            CapturingValue,
            CapturingSectionName,
        }

        private static readonly int _translationsPrefixLength = TranslationConfiguration.KEY_SECTION_TRANSLATIONS_DOT.Length;

        protected virtual char LineCommentChar { get; }

        /// <summary>
        /// Gets or sets the escape mode to use when recognizing template strings in translation entries.
        /// </summary>
        public static TemplateStringEscaping TemplateEscapeMode { get; set; }

        protected override TemplateStringEscaping GetTemplateEscapeMode()
        {
            return TemplateEscapeMode;
        }

        public override Translation? LoadTranslation(string translationText)
        {
            string currentGroup = string.Empty;
            string key;
            string? value, escapedValue, reference;

            Translation result = new (locale: null);
            TranslationEntry entry;

            if (string.IsNullOrEmpty(translationText))
                return null;

            Dictionary<string, (string? escaped, string unescaped)?> lines = LoadAsDictionary(translationText);

            foreach (var line in lines)
            {
                if (line.Value is null)
                {
                    currentGroup = line.Key.Trim();
                }
                else
                {
                    if (currentGroup.Length == 0)
                        key = line.Key.Trim();
                    else
                        key = currentGroup + "." + line.Key.Trim();

                    value = line.Value.Value.unescaped;
                    escapedValue = line.Value.Value.escaped;

                    if (IsReference(value))
                    {
                        reference = value.Substring(1);
                        value = null;
                    }
                    else
                    {
                        reference = null;
                    }

                    entry = new TranslationEntry(key, value, escapedValue, reference);

                    if (reference is not null)
                    {
                        if (escapedValue is not null)
                            entry.IsTemplated = IsTemplatedText(escapedValue);
                        else
                        if (value is not null)
                            entry.IsTemplated = IsTemplatedText(value);
                    }

                    result.Add(key.ToUpperInvariant(), entry);
                }
            }

            return result;
        }

        public override TranslationConfiguration? ParseConfiguration(string fileContent)
        {
            if (string.IsNullOrEmpty(fileContent))
                return null;

            Dictionary<string, (string? escaped, string unescaped)?> lines = LoadAsDictionary(fileContent);

            (string? escaped, string unescaped)? valueTuple;

            lines.TryGetValue(TranslationConfiguration.KEY_DEFAULT_FILE, out valueTuple);
            string? defaultFile = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_DEFAULT_LOCALE, out valueTuple);
            string? defaultLocale = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_GENERATED_NAMESPACE, out valueTuple);
            string? generatedNamespace = valueTuple?.unescaped?.Trim();

            lines.TryGetValue(TranslationConfiguration.KEY_GENERATED_CLASS, out valueTuple);
            string? generatedClassName = valueTuple?.unescaped?.Trim();

            TranslationConfiguration result = new TranslationConfiguration(defaultFile ?? string.Empty, generatedNamespace, generatedClassName, defaultLocale, GetTemplateEscapeMode());

            if (string.IsNullOrEmpty(defaultFile))
                return result;

            // If the configuration contains the Translations section, parse it
            foreach (var key in lines.Keys.Where(k => k.StartsWith(TranslationConfiguration.KEY_SECTION_TRANSLATIONS_DOT, StringComparison.OrdinalIgnoreCase)))
            {
                string? value = lines[key]?.unescaped.Trim();

                // The list contains sections too, but they have values set to null (empty keys have empty but non-null values)
                if (value is null)
                    continue;

                // skip the entry named "translations." if any such entry occurs
                if (key.Length == _translationsPrefixLength)
                    continue;

                var keyTrimmed = key.Substring(_translationsPrefixLength + 1).Trim();
                string? lang = keyTrimmed;

                if (lang.Equals(TranslationConfiguration.KEY_TRANSLATION_ASTERISK, StringComparison.Ordinal))
                    lang = TranslationConfiguration.KEY_TRANSLATION_DEFAULT;
                else
                    lang = lang.ToUpperInvariant();

                if (result.Translations.ContainsKey(lang))
                    throw new GenericParserException($"Duplicate translation reference '{keyTrimmed}' specified in the list of translations");
                result.Translations.Add(lang, value);
            }

            return result;
        }

        /// <summary>
        /// This method loads the file as a list of key-value pairs.
        /// If sections are detected, they are added as key with values set to null.
        /// </summary>
        /// <param name="content">the content to parse.</param>
        /// <returns>the list of key-value pairs.</returns>
        internal Dictionary<string, (string? escaped, string unescaped)?> LoadAsDictionary(string content)
        {
            Dictionary<string, (string? escaped, string unescaped)?> result = new Dictionary<string, (string? escaped, string unescaped)?>(StringComparer.OrdinalIgnoreCase);

            char lineCommentChar = LineCommentChar;

            var currentLineNumber = 1;
            var currentColumnNumber = 1;

            int offset = 0;

            int lineStartPos = 0;
            int keyStartPos = -1;
            int keyEndPos = -1;
            int valueStartPos = -1;

            string capturedKey = string.Empty;

            TextParserState state = TextParserState.LookingForLineStart;

            // walk through the lines one by one
            while (offset < content.Length)
            {
                switch (state)
                {
                    case TextParserState.LookingForLineStart:
                        if (char.IsWhiteSpace(content[offset]))
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (content[offset] == lineCommentChar) // skip lines that start with a comment char
                            state = TextParserState.SkippingTillEOL;
                        else
                        if (content[offset] == '[')
                        {
                            state = TextParserState.CapturingSectionName;
                            keyStartPos = offset;
                        }
                        else
                        if (IsStartOfKey(content, offset))
                        {
                            state = TextParserState.CapturingKey;
                            keyStartPos = offset;
                        }
                        else
                            throw new TextParseException($"Unexpected character '${content[offset]}' at {currentLineNumber}:{currentColumnNumber}", keyStartPos, keyStartPos, currentLineNumber, currentColumnNumber);

                        break;

                    case TextParserState.SkippingTillEOL:

                        if (content[offset] == '\n') // detect EOL
                        {
                            state = TextParserState.LookingForLineStart;
                            currentLineNumber++;
                            currentColumnNumber = 0; // it will be incremented after the switch and be set to the starting position 1
                            lineStartPos = offset + 1;
                        }

                        break;

                    case TextParserState.CapturingKey:

                        if (IsEndOfKey(content, offset, out int posAfterKey)) // found end of key
                        {
                            capturedKey = UnwrapKey(content.Substring(keyStartPos, posAfterKey - keyStartPos));

                            if (IsSeparatorChar(content[offset])) // now, we can look for a value
                                state = TextParserState.LookingForValueStart;
                            else
                                state = TextParserState.LookingForSeparator;

                            keyEndPos = offset;

                            break;
                        }
                        else
                        if (IsValidKeyChar(content[offset])) // acceptable characters
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (content[offset] == '\n') // detect EOL as it is not permitted
                            throw new TextParseException($"Line {currentLineNumber} does not contain a key/value pair", lineStartPos, offset, currentLineNumber, currentColumnNumber);
                        else
                            throw new TextParseException($"Unexpected character '${content[offset]}' at {currentLineNumber}:{currentColumnNumber}", lineStartPos, offset, currentLineNumber, currentColumnNumber);

                        break;

                    case TextParserState.LookingForSeparator:

                        if (char.IsWhiteSpace(content[offset]))
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (IsSeparatorChar(content[offset]))
                        {
                            state = TextParserState.LookingForValueStart;
                        }
                        else
                        if (content[offset] == '\n') // detect EOL as it is not permitted
                            throw new TextParseException($"Line {currentLineNumber} does not contain a key/value pair", lineStartPos, offset, currentLineNumber, currentColumnNumber);
                        else
                            throw new TextParseException($"Unexpected character '${content[offset]}' at {currentLineNumber}:{currentColumnNumber}", lineStartPos, offset, currentLineNumber, currentColumnNumber);

                        break;

                    case TextParserState.LookingForValueStart:

                        if (char.IsWhiteSpace(content[offset]))
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (content[offset] == '\n') // detect EOL - it is the end of an empty value
                        {
                            // Add an empty value to the resulting dictionary
                            if (!string.IsNullOrEmpty(capturedKey))
                            {
                                if (result.ContainsKey(capturedKey))
                                    throw new TextParseException($"Duplicate key `{capturedKey}`", keyStartPos, keyEndPos, currentLineNumber, keyEndPos - lineStartPos + 1);
                                result[capturedKey] = (null, string.Empty);
                            }

                            // ... and switch to the new line
                            state = TextParserState.LookingForLineStart;
                            currentLineNumber++;
                            currentColumnNumber = 0; // it will be incremented after the switch and be set to the starting position 1
                            lineStartPos = offset + 1;
                        }
                        else
                        if (IsStartOfValue(content, offset))
                        {
                            state = TextParserState.CapturingValue;
                            valueStartPos = offset;
                        }

                        break;

                    case TextParserState.CapturingValue:

                        bool? endCheck = IsEndOfValue(content, offset, out int posAfterValue);

                        if (endCheck is null)
                            throw new TextParseException($"Unexpected end of line at {currentLineNumber}:{currentColumnNumber}", valueStartPos, offset, currentLineNumber, currentColumnNumber);
                        else
                        if (endCheck == true)
                        {
                            // Add a value to the resulting dictionary
                            if (!string.IsNullOrEmpty(capturedKey))
                            {
                                if (result.ContainsKey(capturedKey))
                                    throw new TextParseException($"Duplicate key `{capturedKey}`", keyStartPos, keyEndPos, currentLineNumber, keyStartPos - lineStartPos + 1);
                                string value = content.Substring(valueStartPos, posAfterValue - valueStartPos);
                                result[capturedKey] = UnwrapValue(value);
                            }

                            currentColumnNumber += posAfterValue - offset;
                            offset = posAfterValue;

                            if (offset < content.Length)
                            {
                                if (content[offset] == '\n')
                                {
                                    // ... and switch to the new line
                                    state = TextParserState.LookingForLineStart;
                                    currentLineNumber++;
                                    currentColumnNumber = 0; // it will be incremented after the switch and be set to the starting position 1
                                    lineStartPos = offset + 1;
                                }
                                else
                                    state = TextParserState.SkippingTillEOL;
                            }
                            else
                                state = TextParserState.LookingForLineStart; // makes no sense at the end, but the end of the method checks the current state for consistency (no pending opened keys or values)

                            offset--; // we need to decrement it so that after the switch, it gets incremented back and points at the first character after the end of value
                        }

                        break;

                    case TextParserState.CapturingSectionName:

                        if (IsValidSectionNameChar(content[offset])) // acceptable characters
                        {
                            // do nothing - the pointer is shifted after the switch statement
                        }
                        else
                        if (content[offset] == ']') // found the end of a section name
                        {
                            // check for validity of the name
                            if (content[keyStartPos] == '_' || char.IsLetter(content[keyStartPos]))
                            {
                                capturedKey = content.Substring(keyStartPos, offset - keyStartPos);
                            }
                            else
                                throw new TextParseException($"A section name may not start with '${content[keyStartPos]}' at {currentLineNumber}:{keyStartPos - lineStartPos + 1}", keyStartPos, offset, currentLineNumber, offset - lineStartPos + 1);

                            // Add a null value to the resulting dictionary
                            if (!string.IsNullOrEmpty(capturedKey))
                            {
                                // We use '^' (caret) to distinguish between section names and keys - neither of them may start with a caret anyway
                                if (result.ContainsKey('^' + capturedKey))
                                    throw new TextParseException($"Duplicate section name `{capturedKey}`", keyStartPos, offset, currentLineNumber, keyStartPos - lineStartPos + 1);
                                result['^' + capturedKey] = null;
                            }
                            else
                                throw new TextParseException($"Empty section name", keyStartPos, offset, currentLineNumber, offset - lineStartPos + 1);

                            state = TextParserState.SkippingTillEOL;

                            break;
                        }
                        else
                        if (content[offset] == '\n') // detect EOL as it is not permitted
                            throw new TextParseException($"Unexpected end of line at {currentLineNumber}:{currentColumnNumber}", keyStartPos, offset, currentLineNumber, currentColumnNumber);
                        else
                            throw new TextParseException($"Unexpected character at {currentLineNumber}:{currentColumnNumber}", offset, offset, currentLineNumber, currentColumnNumber);

                        break;
                }

                offset++;
                currentColumnNumber++;
            }

            if (state != TextParserState.LookingForLineStart && state != TextParserState.SkippingTillEOL)
                throw new TextParseException($"Unexpected end of file at {currentLineNumber}:{currentColumnNumber}", offset, offset, currentLineNumber, currentColumnNumber);

            return result;
        }

        protected abstract bool IsStartOfKey(string content, int offset);

        protected abstract bool IsEndOfKey(string content, int offset, out int newPosition);

        /// <summary>
        /// Strips format-specific markers that denote the beginning and the end of a value.
        /// </summary>
        /// <param name="value">the value to strip.</param>
        /// <returns>the text inside the markers.</returns>
        protected abstract string UnwrapKey(string value);

        protected abstract bool IsSeparatorChar(char candidate);

        protected abstract bool IsStartOfValue(string content, int offset);

        /// <summary>
        /// Detects if the end of a value has been reached.
        /// </summary>
        /// <param name="content">content to check.</param>
        /// <param name="offset">current position within the content.</param>
        /// <param name="newPosition">upon return is set to a position next to the end-of-value marker (indicates to which value the pointer must be set).</param>
        /// <returns><see langword="true"/> if the end of the value has been reached, <see langword="false"/> if the end has not been reached, and <see langword="null"/> if the end of line was encountered and a single-line value was not closed.</returns>
        /// <exception cref="TextParseException">thrown when an error occurs.</exception>
        protected abstract bool? IsEndOfValue(string content, int offset, out int newPosition);

        /// <summary>
        /// Strips format-specific markers that denote the beginning and the end of a value.
        /// </summary>
        /// <param name="value">the value to strip.</param>
        /// <returns>the text inside the markers.</returns>
        protected abstract (string? escaped, string unescaped) UnwrapValue(string value);

        protected virtual bool IsValidKeyChar(char value)
        {
            return char.IsLetterOrDigit(value) ||
                   (value == '_') ||
                   (value == '-') ||
                   (value == '.');
        }

        protected virtual bool IsValidSectionNameChar(char value) => IsValidKeyChar(value);

        protected override TranslationTree? InternalLoadTranslationStructure(string content)
        {
            string currentGroup = string.Empty;

            string key, value;

            TranslationTree result = new();

            if (string.IsNullOrEmpty(content))
                return result;

            TranslationTreeNode? node = result.RootNode;
            TranslationTreeLeaf leaf;

            Dictionary<string, (string? escaped, string unescaped)?> lines = LoadAsDictionary(content);

            foreach (var line in lines)
            {
                if (line.Value is null)
                {
                    currentGroup = line.Key;
                    node = result.MakeNode(currentGroup);
                    if (node == null) // this should not normally happen - MakeNode returns null when the name is invalid, and we control names during parsing.
                        continue;
                }
                else
                {
                    key = line.Key;
                    if (line.Value.Value.escaped is not null)
                    {
                        value = line.Value.Value.escaped;
                        leaf = new TranslationTreeLeaf(key, !IsReference(value) && IsTemplatedText(value));
                    }
                    else
                    {
                        value = line.Value.Value.unescaped;
                        leaf = new TranslationTreeLeaf(key, !IsReference(value));
                    }

                    node!.Keys.Add(key, leaf);
                }
            }

            return result;
        }

        internal override bool IsTemplatedText(string text)
        {
            return StringHasParameters(text, TemplateEscapeMode);
        }
    }
}
