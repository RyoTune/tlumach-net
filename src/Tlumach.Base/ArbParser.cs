// <copyright file="ArbParser.cs" company="Allied Bits Ltd.">
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
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Tlumach.Base
{
    public class ArbPlaceholder : Placeholder
    {
        public ArbPlaceholder(string name)
            : base(name)
        {
        }

        internal void SetType(string type)
        {
            Type = type;
        }

        internal void SetFormat(string format)
        {
            Format = format;
        }

        internal void SetExample(string example)
        {
            Example = example;
        }
    }

    public partial class ArbParser : BaseFileParser
    {
        private const string ARB_KEY_LOCALE = "@@locale";
        private const string ARB_KEY_GLOBAL_CONTEXT = "@@context";
        private const string ARB_KEY_LAST_MODIFIED = "@@last_modified";
        private const string ARB_KEY_AUTHOR = "@@author";
        private const string ARB_KEY_DESCRIPTION = "description";
        private const string ARB_KEY_TYPE = "type";
        private const string ARB_KEY_CONTEXT = "context";
        private const string ARB_KEY_SOURCE_TEXT = "source_text";
        private const string ARB_KEY_SCREEN = "screen";
        private const string ARB_KEY_VIDEO = "video";
        private const string ARB_KEY_PLACEHOLDERS = "placeholders";
        private const string ARB_KEY_FORMAT = "format";
        private const string ARB_KEY_EXAMPLE = "example";
        private const string ARB_KEY_OPTIONAL_PARAMETERS = "optionalParameters";

        /// <summary>
        /// Gets or sets the escape mode to use when recognizing template strings in translation entries.
        /// </summary>
        public static TemplateStringEscaping TemplateEscapeMode { get; set; }

        static ArbParser()
        {
            TemplateEscapeMode = TemplateStringEscaping.Arb;

            // We register the parser for both configuration files and translation files.
            // This approach enables us to use configuration and translations in different formats.
            FileFormats.RegisterConfigParser(".arbcfg", Factory);
            FileFormats.RegisterParser(".arb", Factory);
        }

        protected override TemplateStringEscaping GetTemplateEscapeMode()
        {
            return TemplateEscapeMode;
        }

        public override bool CanHandleExtension(string fileExtension)
        {
            return fileExtension.Equals(".arb", StringComparison.OrdinalIgnoreCase);
        }

        public override TranslationConfiguration? ParseConfiguration(string fileContent)
        {
            try
            {
                JObject? configObj = JObject.Parse(fileContent);

                string? defaultFile = configObj.Value<string>(TranslationConfiguration.KEY_DEFAULT_FILE);
                string? defaultLocale = configObj.Value<string>(TranslationConfiguration.KEY_DEFAULT_LOCALE);
                string? generatedNamespace = configObj.Value<string>(TranslationConfiguration.KEY_GENERATED_NAMESPACE);
                string? generatedClassName = configObj.Value<string>(TranslationConfiguration.KEY_GENERATED_CLASS);

                TranslationConfiguration result = new TranslationConfiguration(defaultFile ?? string.Empty, generatedNamespace, generatedClassName, defaultLocale, GetTemplateEscapeMode());

                if (string.IsNullOrEmpty(defaultFile))
                    return result;

                // If the configuration contains the Translations section, parse it
                if (configObj.TryGetValue(TranslationConfiguration.KEY_SECTION_TRANSLATIONS, StringComparison.OrdinalIgnoreCase, out JToken? translationsToken) && translationsToken is JObject translationsObject)
                {
                    // Enumerate properties
                    foreach (JProperty prop in translationsObject.Properties())
                    {
                        string lang = prop.Name.Trim();
                        if (lang.Equals(TranslationConfiguration.KEY_TRANSLATION_ASTERISK, StringComparison.Ordinal))
                            lang = TranslationConfiguration.KEY_TRANSLATION_DEFAULT;
                        else
                            lang = lang.ToUpperInvariant();
                        if (prop.Value.Type == JTokenType.String)
                        {
                            string value = prop.Value.ToString().Trim();
                            if (result.Translations.ContainsKey(lang))
                                throw new GenericParserException($"Duplicate translation reference '{prop.Name}' specified in the list of translations");
                            result.Translations.Add(lang, value);
                        }
                        else
                        {
                            throw new GenericParserException($"Translation reference '{prop.Name}' is not a string");
                        }
                    }
                }

                return result;
            }
            catch (JsonReaderException ex)
            {
                int pos = GetAbsolutePosition(fileContent, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of configuration has failed", ex);
            }
        }

        /*public override bool IsValidConfigFile(string fileContent, out TranslationConfiguration? configuration)
        {
            configuration = InternalLoadConfig(fileContent);

            if ((configuration is not null) && !string.IsNullOrEmpty(configuration.DefaultFile) && File.Exists(configuration.DefaultFile))
            {
                return true;
            }

            return false;
        }*/

        public override Translation? LoadTranslation(string translationText)
        {
            try
            {
                JObject? jsonObj = JObject.Parse(translationText);

                return InternalLoadTranslationEntryFromJSON(jsonObj, null, string.Empty);
            }
            catch (JsonReaderException ex)
            {
                int pos = GetAbsolutePosition(translationText, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of the translation has failed", ex);
            }
        }

        protected override TranslationTree? InternalLoadTranslationStructure(string content)
        {
            try
            {
                JObject? jsonObj = JObject.Parse(content);

                TranslationTree result = new();

                InternalLoadTreeNodeFromJSON(jsonObj, result, result.RootNode);

                return result;
            }
            catch (JsonReaderException ex)
            {
                int pos = GetAbsolutePosition(content, ex.LineNumber, ex.LinePosition);
                throw new TextParseException(ex.Message, pos, pos, ex.LineNumber, ex.LinePosition);
            }
            catch (Exception ex)
            {
                throw new GenericParserException("Parsing of configuration has failed", ex);
            }
        }

        private static BaseFileParser Factory() => new ArbParser();

        /// <summary>
        /// Extracts placeholder definitions from a JSON object and stores them in the translation entry.
        /// </summary>
        /// <param name="entry">the entry to put the definitions to.</param>
        /// <param name="jsonObj">the object to extract definitions from.</param>
        private static void InternalProcessEntryPlaceholderDefinitions(TranslationEntry entry, JObject jsonObj)
        {
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.Object))
            {
                string name = prop.Name.Trim();
                InternalAddSingplePlaceholderDefinition(entry, name, (JObject)prop.Value);
            }
        }

        private static void InternalAddSingplePlaceholderDefinition(TranslationEntry entry, string placeholderName, JObject jsonObj)
        {
            ArbPlaceholder placeholder = new(placeholderName);

            // Collect main and unrecognized parameters
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.String))
            {
                string key = prop.Name.Trim();
                string? value = prop.Value.ToString();

                if (key.Equals(ARB_KEY_TYPE, StringComparison.OrdinalIgnoreCase))
                    placeholder.SetType(value);
                else
                if (key.Equals(ARB_KEY_FORMAT, StringComparison.OrdinalIgnoreCase))
                    placeholder.SetFormat(value);
                else
                if (key.Equals(ARB_KEY_EXAMPLE, StringComparison.OrdinalIgnoreCase))
                    placeholder.SetExample(value);
                else
                    placeholder.Properties[key] = value;
            }

            // Collect optional parameters
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.Object && p.Name.Trim().Equals(ARB_KEY_OPTIONAL_PARAMETERS, StringComparison.OrdinalIgnoreCase)))
            {
                JObject? childObj = prop.Value as JObject;
                if (childObj != null)
                {
                    foreach (var childProp in childObj.Properties().Where(static p => p.Value.Type == JTokenType.String))
                    {
                        string key = childProp.Name.Trim();
                        string? value = childProp.Value.ToString();
                        placeholder.OptionalParameters[key] = value;
                    }
                }
            }

            // Add new placeholder to the entry
            entry.AddPlaceholder(placeholder);
        }

        private void InternalEnumerateStringPropertiesOfJSONObject(JObject jsonObj, Translation translation, string groupName)
        {
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.String))
            {
                TranslationEntry? entry;
                string key;

                string? value;
                string? target = null;
                string? reference = null;

                key = prop.Name.Trim();

                // pick custom attributes of the translation file
                if (key.StartsWith("@@x-", StringComparison.Ordinal) && key.Length > 4)
                {
                    value = prop.Value.Value<string>() ?? string.Empty;
                    translation.CustomProperties.Add(key.Substring(4), value);
                    continue;
                }

                int atIdx = key.IndexOf('@');
                if (atIdx == 0)
                    continue;
                else
                if (atIdx > 0 && atIdx < key.Length - 1) // the key contains a target for HTML
                {
                    target = key.Substring(atIdx + 1);
                    key = key.Substring(0, atIdx);
                }

                if (!string.IsNullOrEmpty(groupName))
                    key = groupName + "." + key;

                value = prop.Value.Value<string>();

                if (value is not null && IsReference(value))
                {
                    reference = value.Substring(1);
                    value = null;
                }

                // Pick an existing entry ...
                if (translation.TryGetValue(key, out entry))
                {
                    // Report duplicate entries but not always:
                    // we might have pre-allocated the entry if its properties object came before it for some reason.
                    if (!(entry.Text is null && entry.Reference is null))
                        throw new GenericParserException($"Duplicate key '{key}' specified in the translation file");
                    else
                        entry.Text = value;
                }
                else
                {
                    // ... or add a new one
                    entry = new(value);
                    translation.Add(key, entry);
                }

                if (value is not null)
                    entry.IsTemplated = IsTemplatedText(value);
                entry.Reference = reference;
                entry.Target = target;
            }
        }

        private void InternalEnumerateObjectPropertiesOfJSONObject(JObject jsonObj, Translation translation, string groupName)
        {
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.Object))
            {
                TranslationEntry? entry;
                string key;

                string name = prop.Name.Trim();

                var jsonChild = (JObject)prop.Value;

                // Process objects that contain properties of the entries
                if (name.StartsWith("@", StringComparison.Ordinal))
                {
                    if (name.Length == 1)
                        continue;
                    name = name.Substring(1); // Strip leading @

                    // Determine the key of the entry
                    key = (!string.IsNullOrEmpty(groupName)) ? groupName + "." + name : name;

                    // Locate an entry, to which the properties belong. If the entry is not found, add one.
                    if (!translation.TryGetValue(key, out entry))
                        entry = new TranslationEntry(text: null, reference: null);

                    foreach (var childProp in jsonChild.Properties())
                    {
                        string childPropName = childProp.Name.Trim();

                        if (childProp.Value.Type == JTokenType.String)
                        {
                            if (childPropName.Equals(ARB_KEY_DESCRIPTION, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick description
                                entry.Description = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_TYPE, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick type
                                entry.Type = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_CONTEXT, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick context
                                entry.Context = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_SOURCE_TEXT, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick source text
                                entry.SourceText = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_SCREEN, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick screen[shot]
                                entry.Screen = (string?)childProp.Value;
                            }
                            else
                            if (childPropName.Equals(ARB_KEY_VIDEO, StringComparison.OrdinalIgnoreCase))
                            {
                                // Pick video
                                entry.Video = (string?)childProp.Value;
                            }
                        }
                        else
                        if (childPropName.Equals(ARB_KEY_PLACEHOLDERS, StringComparison.OrdinalIgnoreCase) && childProp.Value.Type == JTokenType.Object)
                        {
                            // Pick placeholders
                            InternalProcessEntryPlaceholderDefinitions(entry, (JObject)childProp.Value);
                        }
                    }
                }
                else
                {
                    // We have a group - use recursive handling
                    InternalLoadTranslationEntryFromJSON(jsonChild, translation, (!string.IsNullOrEmpty(groupName)) ? groupName + "." + name : name);
                }
            }
        }

        private Translation InternalLoadTranslationEntryFromJSON(JObject jsonObj, Translation? translation, string groupName)
        {
            // When processing the top level, pick the metadata (locale, context, author, last modified) values if they are present
            if (translation is null)
            {
                string? locale = jsonObj.Value<string>(ARB_KEY_LOCALE);
                string? context = jsonObj.Value<string>(ARB_KEY_GLOBAL_CONTEXT);
                translation = new Translation(locale, context);

                string? value = jsonObj.Value<string>(ARB_KEY_AUTHOR);
                translation.Author = value;

                value = jsonObj.Value<string>(ARB_KEY_LAST_MODIFIED);

                translation.LastModified = Utils.ParseDateISO8601(value);
            }

            // Enumerate string properties
            InternalEnumerateStringPropertiesOfJSONObject(jsonObj, translation, groupName);

            // Enumerate JSON properties that are objects - they either contain extra information about entries or they are child groups
            InternalEnumerateObjectPropertiesOfJSONObject(jsonObj, translation, groupName);

            return translation;
        }

        private void InternalLoadTreeNodeFromJSON(JObject jsonObj, TranslationTree tree, TranslationTreeNode parentNode)
        {
            // Enumerate string properties, which will be keys
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.String))
            {
                string key = prop.Name.Trim();
                // This should not happen but someone may misunderstand the format and use @ with string properties instead of objects
                if (key.StartsWith("@", StringComparison.Ordinal))
                    continue;

                string? value = prop.Value<string>();

                if (value is null)
                    throw new GenericParserException($"The value of the key '{key}' is not a string");

                if (parentNode.Keys.Keys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    throw new GenericParserException($"Duplicate key '{key}' specified");
                parentNode.Keys.Add(key, new TranslationTreeLeaf(key, IsTemplatedText(value)));
            }

            // Enumerate object properties, which will be groups
            foreach (var prop in jsonObj.Properties().Where(static p => p.Value.Type == JTokenType.Object))
            {
                string name = prop.Name.Trim();

                // Skip child JSON nodes which, in ARB format, are supplementary information about an entry.
                // This information is used when loading phrases, but not when building a tree.
                if (name.StartsWith("@", StringComparison.Ordinal))
                    continue;

                if (parentNode.ChildNodes.Keys.Contains(name, StringComparer.OrdinalIgnoreCase))
                    throw new GenericParserException($"Duplicate group name '{name}' specified");

                var jsonChild = (JObject)prop.Value;

                var childNode = parentNode.MakeNode(name);
                if (childNode is null)
                    throw new GenericParserException($"Group '{name}' could not be used to build a tree of translation entries");

                InternalLoadTreeNodeFromJSON(jsonChild, tree, childNode);
            }
        }

        internal override bool IsTemplatedText(string text)
        {
            return ArbParser.StringHasParameters(text, TemplateEscapeMode);
        }
    }
}
