// <copyright file="TranslationEntry.cs" company="Allied Bits Ltd.">
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

namespace Tlumach.Base
{
    /// <summary>
    /// <para>
    /// Represents an entry in the translation file.
    /// An entry may have a value (some text in a specific language) or be a reference to an external file with a translation.
    /// </para>
    /// <para>
    /// Instances of this class are always owned by a dictionary which keeps the keys,
    /// and that dictionary is transferred in a way that specifies the locale.
    /// For this reason, TranslationEntry does not hold a key or locale ID.
    /// </para>
    /// </summary>
    public class TranslationEntry
    {
        private bool _locked = false;
        private string? _text = null;
        private string? _reference = null;

        public static TranslationEntry Empty { get; }

        /// <summary>
        /// Gets or sets a localized text.
        /// </summary>
        public string? Text
        {
            get => _text;
            set
            {
                CheckNotLocked();
                _text = value;
            }
        }

        /// <summary>
        /// Indicates that the text is a template. When it is, use the <see cref="ProcessTemplatedValue"/> method to format the template.
        /// </summary>
        public bool IsTemplated { get; set; } = false;

        /// <summary>
        /// Gets or sets an optional reference to an external file with the translation value.
        /// <para>A reference is set by the parser when the text starts with '@' (at) and the <see cref="ArbParser.RecognizeFileRefs"/> property is <see langword="true"/>.</para>
        /// </summary>
        public string? Reference
        {
            get => _reference;
            set
            {
                CheckNotLocked();
                _reference = value;
            }
        }

        /// <summary>
        /// Gets or sets an optional target of the entry.
        /// <para>Targets are defined in the ARB specification as attributes of HTML elements to which the content should be assigned.</para>
        /// </summary>
        public string? Target { get; set; }

        /// <summary>
        /// Gets or sets an optional type of the entry.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets or sets an optional description of the context.
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        ///  Gets or sets an optional original text that was translated.
        /// </summary>
        public string? SourceText { get; set; }

        /// <summary>
        /// Gets or sets an optional description of the entry.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets an optional reference to a screenshot of the entry.
        /// </summary>
        public string? Screen { get; set; }

        /// <summary>
        /// Gets or sets an optional reference to a video of the entry.
        /// </summary>
        public string? Video { get; set; }

        /// <summary>
        /// Gets or sets an optional collection of placeholder descriptions.
        /// </summary>
        public List<Placeholder>? Placeholders { get; set; }

        static TranslationEntry()
        {
            Empty = new TranslationEntry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationEntry"/> class.
        /// </summary>
        public TranslationEntry()
        {
            // Default constructor does nothing
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationEntry"/> class.
        /// </summary>
        /// <param name="text">an optional localized text of the translation entry.</param>
        /// <param name="reference">an optional reference to an external file with the text.</param>
        public TranslationEntry(string? text, string? reference = null)
        {
            Text = text;
            Reference = reference;
        }

        public void AddPlaceholder(Placeholder placeholder)
        {
            Placeholders ??= [];
            Placeholders.Add(placeholder);
        }

        public string ProcessTemplatedValue(Func<string, object?> getParamValueFunc, TemplateStringEscaping templateEscapeMode = TemplateStringEscaping.None)
        {
            string inputText = Text ?? string.Empty;
            if (templateEscapeMode == TemplateStringEscaping.None)
                return inputText;


            // todo: implement
            return string.Empty;
        }

        #region Internal use

        /// <summary>
        /// For internal use only.
        /// </summary>
        public void Lock()
        {
            _locked = true;
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public void Unlock()
        {
            _locked = false;
        }

        private void CheckNotLocked()
        {
            if (_locked)
            {
                throw new InvalidOperationException("A translation entry should not be modified by event handlers. When handling an event, create a new entry instead.");
            }
        }
        #endregion
    }
}
