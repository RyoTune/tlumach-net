// <copyright file="TranslationManager.cs" company="Allied Bits Ltd.">
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

using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using Tlumach.Base;

namespace Tlumach
{
    public class TranslationManager
    {
        /// <summary>
        /// A container for all translations managed by this class.
        /// </summary>
        private readonly Dictionary<string, Translation> _translations = new (StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The default translation that is used as a fallback.
        /// </summary>
        private Translation? _defaultTranslation;

        private CultureInfo _culture = CultureInfo.InvariantCulture;

        private TranslationConfiguration? _defaultConfig;

        public bool LoadFromDisk { get; set; }

        public string FilesLocation { get; set; } = string.Empty;

        public CultureInfo CurrentCulture => _culture;

        /// <summary>
        /// The event is fired when the content of the file is to be loaded. A handler can provide file content from another location.
        /// </summary>
        public event EventHandler<FileContentNeededEventArgs>? OnFileContentNeeded;

        /// <summary>
        /// The event is fired when the translation of a certain key is requested. A handler can provide a different text or even a reference which will be resolved.
        /// If the returned values are valid and accepted (e.g., a reference is properly resolved), the value is returned without firing <seealso cref="OnTranslationValueFound"/>.
        /// </summary>
        public event EventHandler<TranslationValueEventArgs>? OnTranslationValueNeeded;

        /// <summary>
        /// The event is fired after a translation of a certain key has been found in a file.
        /// Should a handler need to provide a different value, it may change the text in the <seealso cref="TranslationValueEventArgs.Text"/> property or replace the reference in the <seealso cref="TranslationValueEventArgs.Entry"/> property of the arguments.
        /// </summary>
        public event EventHandler<TranslationValueEventArgs>? OnTranslationValueFound;

        /// <summary>
        /// The event is fired after a translation of a certain key has not been found in a file.
        /// Should a handler decide to provide some value, it may set the text in the <seealso cref="TranslationValueEventArgs.Text"/> property or place a reference in the <seealso cref="TranslationValueEventArgs.Entry"/> property of the arguments.
        /// </summary>
        public event EventHandler<TranslationValueEventArgs>? OnTranslationValueNotFound;

        /// <summary>
        /// The event is fired when the CurrentCulture property is changed by the application.
        /// It is used primarily by the reactive TranslationUnit classes.
        /// </summary>
        public event EventHandler<CultureChangedEventArgs>? OnCultureChanged;

        public TranslationManager()
        {
            _defaultConfig = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationManager"/> class.
        /// <para>
        /// This constructor creates a translation manager based on the configuration that is to be loaded from the file.
        /// Such translation manager can be used to simplify access to translations when translation units are not used - an application simply calls the GetValue method and species the key and, optionally, the culture.
        /// </para>
        /// </summary>
        /// <param name="configFile">the file with the configuration that specifies where to load translations from.</param>
        public TranslationManager(string configFile)
        {
            if (configFile is null)
                throw new ArgumentNullException(nameof(configFile));

            string filename = configFile;

            // The config parser will parse configuration and will find the correct parser for the files referenced by the configuration
            BaseFileParser? parser = FileFormats.GetConfigParser(Path.GetExtension(filename));
            if (parser is null)
                throw new GenericParserException($"Failed to find a parser for the configuration file '{filename}'");

            TranslationConfiguration? configuration = parser.ParseConfiguration(filename);

            if (configuration is null)
                throw new ParserLoadException(filename, $"Failed to load the configuration from '{filename}'");

            _defaultConfig = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationManager"/> class.
        /// <para>
        /// This constructor creates a translation manager based on the specified configuration.
        /// Such translation manager can be used to simplify access to translations when translation units are not used - an application simply calls the GetValue method and species the key and, optionally, the culture.
        /// </para>
        /// </summary>
        /// <param name="translationConfiguration">the configuration that specifies where to load translations from.</param>
        public TranslationManager(TranslationConfiguration translationConfiguration)
        {
            _defaultConfig = translationConfiguration;
        }

        private static string? ReadFileFromResource(Assembly assembly, string resourceName)
        {
            try
            {
                // Resource names are fully qualified: "Namespace.Folder.Filename.txt"
                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream is null)
                    return null;

                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }

        private static string? FindBasicCulture(CultureInfo culture)
        {
            CultureInfo? neutral = null;

            if (culture.IsNeutralCulture)
                neutral = culture;
            else
            {
                string cultureName = culture.Name;
                if (cultureName.Length > 2 && cultureName[2] == '-')
                {
                    try
                    {
                        neutral = new CultureInfo(cultureName.Substring(0, 2));
                    }
                    catch (CultureNotFoundException)
                    {
                        // ignore the not found exception - that's ok for us
                    }
                }
            }

            if (neutral is null)
                return null;

            CultureInfo? basic = null;
            try
            {
                basic = CultureInfo.CreateSpecificCulture(neutral.Name);
                if (basic.Name == culture.Name)
                    return null;
            }
            catch (CultureNotFoundException)
            {
                // ignore the not found exception - that's ok for us
            }

            return basic?.Name;
        }

        /// <summary>
        /// From the list of available language files obtained using <see cref="ListTranslationFiles()"/>, retrieve culture information (needed for language names and to switch application language).
        /// </summary>
        /// <param name="fileNames">the list of names obtained from <see cref="ListTranslationFiles()"/>.</param>
        /// <returns>the list of<see cref="System.Globalization.CultureInfo"/>.</returns>
        public static IList<CultureInfo> ListCultures(IList<string> fileNames)
        {
            if (fileNames is null)
                throw new ArgumentNullException(nameof(fileNames));
            IList<CultureInfo> result = [];
            string cultureName;
            CultureInfo cultureInfo;
            int idx;
            string fileExt;
            foreach (var filename in fileNames)
            {
                idx = filename.LastIndexOf('_');
                if (idx >= 0 && idx < filename.Length - 1)
                {
                    fileExt = Path.GetExtension(filename);
                    cultureName = filename.Substring(idx + 1, filename.Length - (idx + 1) - fileExt.Length);
                    try
                    {
                        // We obtain the culture for the given code.
                        // If it is neutral (no region specified),
                        // we use CreateSpecificCulture method to obtain a culture for the default region.
                        // The mapping to default regions is hardcoded into .NET for all neutral cultures.
                        cultureInfo = new CultureInfo(cultureName);
                        if (cultureInfo.IsNeutralCulture)
                        {
                            cultureInfo = CultureInfo.CreateSpecificCulture(cultureName);
                        }

                        result.Add(cultureInfo);
                        continue;
                    }
                    catch (CultureNotFoundException)
                    {
                        // ignore
                    }
                }
            }

            return result;
        }

        public static Translation? LoadTranslation(string translationText, string fileExtension)
        {
            BaseFileParser? parser = FileFormats.GetParser(fileExtension);
            if (parser is null)
                return null;

            return parser.LoadTranslation(translationText);
        }

        public static Translation? LoadTranslation(string translationText, BaseFileParser parser)
        {
            if (parser is null)
                throw new ArgumentNullException(nameof(parser));

            return parser.LoadTranslation(translationText);
        }

        /// <summary>
        /// Updates current culture of the manager.
        /// </summary>
        /// <param name="culture">the new culture to use.</param>
        public void SetCulture(CultureInfo culture)
        {
            if (culture is null)
                throw new ArgumentNullException(nameof(culture));

            // Update the culture only if current culture is not the same as the one in the argument
            if (!culture.Name.Equals(_culture.Name, StringComparison.Ordinal))
            {
                _culture = culture;

                // Notify listeners about the change
                OnCultureChanged?.Invoke(this, new CultureChangedEventArgs(_culture));
            }
        }

        /// <summary>
        /// "Forgets" the translation for the given culture so that upon the next attempt to access it, the translation gets loaded again.
        /// </summary>
        /// <param name="culture">The culture, whose translation should be dropped.</param>
        public void DropTranslation(CultureInfo culture)
        {
            if (culture is null)
                throw new ArgumentNullException(nameof(culture));

            lock (_translations)
            {
                _translations.Remove(culture.Name.ToUpperInvariant());
            }
        }

        /// <summary>
        /// <para>"Forgets" all translation so that upon the next attempt to access any translation, they get loaded again.</para>
        /// <para>The method is useful when it is necessary to rescan translations updated on the disk or in another storage.</para>
        /// </summary>
        public void DropAllTranslations()
        {

            lock (_translations)
            {
                _translations.Clear();
            }
            _defaultTranslation = null;
        }

        /// <summary>
        /// Retrieves the value based on the default configuration and culture.
        /// </summary>
        /// <param name="key">the key of the translation entry to retrieve.</param>
        /// <returns>the translation entry or an empty entry if nothing was found.</returns>
        public TranslationEntry GetValue(string key)
        {
            if (_defaultConfig is null)
                return TranslationEntry.Empty;

            return GetValue(_defaultConfig, _culture, key);
        }

        /// <summary>
        /// Retrieves the value based on the default configuration and culture.
        /// </summary>
        /// <param name="culture">the culture, for which the entry is needed.</param>
        /// <param name="key">the key of the translation entry to retrieve.</param>
        /// <returns>the translation entry or an empty entry if nothing was found.</returns>
        public TranslationEntry GetValue(CultureInfo culture, string key)
        {
            if (_defaultConfig is null)
                return TranslationEntry.Empty;

            return GetValue(_defaultConfig, culture, key);
        }

#pragma warning disable MA0051 // Method is too long
        /// <summary>
        /// Retrieves the value based on the default configuration and culture.
        /// </summary>
        /// <param name="config">the configuration that specifies from where to load translations.</param>
        /// <param name="culture">the culture, for which the entry is needed.</param>
        /// <param name="key">the key of the translation entry to retrieve.</param>
        /// <returns>the translation entry or an empty entry if nothing was found.</returns>
        public TranslationEntry GetValue(TranslationConfiguration config, CultureInfo culture, string key)
#pragma warning restore MA0051 // Method is too long
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            if (config.DefaultFile is null)
                throw new ArgumentNullException(nameof(config.DefaultFile));

            if (culture is null)
                throw new ArgumentNullException(nameof(culture));

            if (key is null)
                throw new ArgumentNullException(nameof(key));

            TranslationEntry? result = null;
            string keyUpper = key.ToUpperInvariant();

            Translation? translation = null;
            Translation? cultureLocalTranslation = null;
            Translation? basicCultureLocalTranslation = null;

            // If the OnTranslationValueNeeded event is defined, fire it first and use its result if one is returned
            if (OnTranslationValueNeeded is not null)
            {
                TranslationValueEventArgs args = new(culture, key);
                OnTranslationValueNeeded.Invoke(this, args);
                if (args.Entry is not null && TranslationEntryAcceptable(args.Entry, originalAssembly: null, originalFile: null))
                    return args.Entry;

                if (args.Text is not null || args.EscapedText is not null)
                    return EntryFromEventArgs(args, config.TemplateEscapeMode);
            }

            // If requesting text for a non-default culture, deal with the culture-specific translation
            if (culture != _culture)
            {
                string? cultureNameUpper = culture.Name.ToUpperInvariant();

                // first, we try to obtain the translation entry from the culture-specific translation
                result = TryGetEntryFromCulture(keyUpper, key, cultureNameUpper, config, culture, false, ref cultureLocalTranslation);

                if (result is null && (cultureLocalTranslation is null || !cultureLocalTranslation.IsBasicCulture))
                {
                    // try to find the basic culture, e.g., for de-AT, it would be "de", and from there, "de-DE", in which we are interested
                    cultureNameUpper = FindBasicCulture(culture)?.ToUpperInvariant();
                    if (cultureNameUpper is not null)
                    {
                        // next, try to obtain the translation entry from the basic-culture translation
                        result = TryGetEntryFromCulture(keyUpper, key, cultureNameUpper, config, culture, true, ref basicCultureLocalTranslation);
                        if (result is not null)
                        {
                            // if a locale-specific translation exists, cache the value from the basic-culture translation in the culture-local one so that in the future, no attempt to load or go to the basic-culture translation is needed
                            cultureLocalTranslation?.Add(keyUpper, result);
                            translation = basicCultureLocalTranslation;
                        }
                    }
                }
                else
                    translation = cultureLocalTranslation;

                if (result is not null)
                {
                    return FireTranslationValueFound(culture, key, result, translation?.OriginalAssembly, translation?.OriginalFile, config.TemplateEscapeMode);
                }
            }

            // At this point, we need a default translation
            if (_defaultTranslation is null)
            {
                translation = InternalLoadTranslation(config, _culture);
                _defaultTranslation = translation;
                if (translation is not null)
                {
                    string cultureNameUpper;
                    if (!string.IsNullOrEmpty(translation.Locale))
                    {
                        cultureNameUpper = translation.Locale!.ToUpperInvariant();
                    }
                    else
                    {
                        cultureNameUpper = culture.Name.ToUpperInvariant();
                    }

                    lock (_translations)
                    {
                        if (!_translations.ContainsKey(cultureNameUpper))
                        {
                            _translations.Add(cultureNameUpper, translation);
                        }
                    }
                }
            }

            // Try loading from the default translation
            if (_defaultTranslation is not null
                && _defaultTranslation.TryGetValue(keyUpper, out result)
                && result is not null
                && TranslationEntryAcceptable(result, _defaultTranslation.OriginalAssembly, _defaultTranslation.OriginalFile))
            {
                // if a locale-specific translation exists, cache the value from the default translation in the culture-local one so that in the future, no attempt to load or go to the default translation is needed
                cultureLocalTranslation?.Add(keyUpper, result);

                // if a basic-locale translation exists, cache the value from the default translation in the basic-culture one so that in the future, no attempt to load or go to the default translation is needed
                basicCultureLocalTranslation?.Add(keyUpper, result);

                return FireTranslationValueFound(culture, key, result, _defaultTranslation.OriginalAssembly, _defaultTranslation.OriginalFile, config.TemplateEscapeMode);
            }

            return FireTranslationValueNotFound(culture, key, config.TemplateEscapeMode);
        }

        private TranslationEntry? TryGetEntryFromCulture(string keyUpper, string key, string cultureNameUpper, TranslationConfiguration config, CultureInfo culture, bool isBasicCulture, ref Translation? cultureLocalTranslation)
        {
            TranslationEntry? result = null;
            Translation? translation = null;

            // Locate the translation set for the specified locale
            lock (_translations)
            {
                bool notInList = true; // we use it to speed up access a bit

                if (!_translations.TryGetValue(cultureNameUpper, out translation))
                    translation = null;
                else
                    notInList = false;

                if (translation is null)
                {
                    translation = InternalLoadTranslation(config, culture);
                    translation ??= new Translation(culture.Name); // we use an empty translation here, but we cannot use a static instance because this particular instance will be filled with entries from the default translations one by one once they are accessed.
                    if (notInList)
                        _translations.Add(cultureNameUpper, translation);
                }
            }

            if (isBasicCulture)
                translation.IsBasicCulture = true;

            // pass the translation up so that if the value is not found, the one from the basic-locale or default translation will be written to this saved one
            cultureLocalTranslation = translation;

            // If the translation contains what we need, try using it
            if (translation.TryGetValue(keyUpper, out result)
                && result is not null
                && TranslationEntryAcceptable(result, translation.OriginalAssembly, translation.OriginalFile))
            {
                return FireTranslationValueFound(culture, key, result, translation.OriginalAssembly, translation.OriginalFile, config.TemplateEscapeMode);
            }

            return null;
        }

        /// <summary>
        /// <para>Scans the assembly and optionally a disk directory for translation files.</para>
        /// <para>This method can recognize only the files with names that have the {base_name}_{locale-name}[.{supported_extension}] format, where 'locale-name' may be either language name (e.g., "en") or locale name (e.g., "en-US").</para>
        /// </summary>
        /// <param name="assembly">an optional assembly to look for translations.</param>
        /// <param name="fileName">the base name of the file to look for. If it contains a recognized extension, the extension is stripped.</param>
        /// <returns>the list of filenames of files found in the corresponding directory.</returns>
        public IList<string> ListTranslationFiles(Assembly? assembly, string fileName)
        {
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));

            foreach (var extension in FileFormats.GetSupportedExtensions().Where((x) => fileName.EndsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                fileName = fileName.Substring(0, fileName.Length - extension.Length);
            }

            List<string> fileNames = [];

            if (LoadFromDisk)
            {
                string? filePath;
                if (string.IsNullOrEmpty(FilesLocation))
                    filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                else
                    filePath = FilesLocation;

                if (!string.IsNullOrEmpty(filePath))
                {
                    // Enumerate all supported files, strip the extension, check if the file's base name matches "fileName", and add all matching filenames to the list
                    string fileNameMatch = fileName + "_";
                    string name;
                    foreach (var extension in FileFormats.GetSupportedExtensions())
                    {
                        var diskFiles = Directory.EnumerateFiles(
                            filePath,
                            "*" + extension
#if NET9_0_OR_GREATER
                            , new EnumerationOptions() { RecurseSubdirectories = false, IgnoreInaccessible = true, MatchType = MatchType.Simple }
#endif
                        );
                        foreach (var diskFile in diskFiles)
                        {
                            name = Path.GetFileName(diskFile);
                            if (name.StartsWith(fileNameMatch, StringComparison.OrdinalIgnoreCase))
                                fileNames.Add(name);
                        }
                    }
                }
            }

            if (assembly is not null)
            {
#pragma warning disable CA1307 // Specify StringComparison for clarity
                string resourceName = fileName.Replace("/", ".").Replace(@"\", ".").ToUpperInvariant();
#pragma warning restore CA1307 // Specify StringComparison for clarity

                string baseName;
                int idx;

                string fileNameMatch = "." + fileName;

                var resourceNames = assembly.GetManifestResourceNames();
                var resourcePaths = resourceNames.Where(str => str.ToUpperInvariant().Contains(resourceName));

                foreach (var resourcePath in resourcePaths)
                {
                    // we enumerate language-specific files like "strings_de-AT",
                    // and we have to match the generic name, such as "strings", with the name of the resource.
                    // But then, we add a specific name to the list so that we can turn it to the CultureInfo object
#pragma warning disable CA1310 // Specify StringComparison for correctness
#if NET9_0_OR_GREATER
                    idx = resourcePath.LastIndexOf('_');
#else
                    idx = resourcePath.LastIndexOf("_");
#endif
#pragma warning restore CA1310 // Specify StringComparison for correctness
                    if (idx > 0)
                    {
                        baseName = resourcePath.Substring(0, idx);
                        if (baseName.EndsWith(fileNameMatch, StringComparison.OrdinalIgnoreCase))
                        {
                            idx = baseName.LastIndexOf(fileNameMatch, StringComparison.OrdinalIgnoreCase);
                            if (idx < resourcePath.Length - 1)
                                fileNames.Add(resourcePath.Substring(idx + 1));
                        }
                    }
                }
            }

            return fileNames;
        }

        private static TranslationEntry EntryFromEventArgs(TranslationValueEventArgs args, TemplateStringEscaping templateEscapeMode)
        {
            string? text = args.Text;
            string? escapedText = args.EscapedText;

            if (text is null && escapedText is not null)
                text = Utils.UnescapeString(escapedText);

            TranslationEntry entry = new(text, escapedText);
            if (escapedText is not null)
                entry.IsTemplated = IsTemplatedText(escapedText, templateEscapeMode);
            else
            if (text is not null)
                entry.IsTemplated = IsTemplatedText(text, templateEscapeMode);

            return entry;
        }

        private static bool IsTemplatedText(string text, TemplateStringEscaping templateEscapeMode)
        {
            return BaseFileParser.StringHasParameters(text, templateEscapeMode);
        }

        /// <summary>
        /// Loads a translation from a file.
        /// </summary>
        /// <param name="config">Configuration information to use (contains an optional reference to the assembly and the filename(s).</param>
        /// <param name="culture">The desired locale for which the file is needed.</param>
        /// <returns>A translation if one was found and loaded and <see langword="null"/> otherwise.</returns>
        private Translation? InternalLoadTranslation(TranslationConfiguration config, CultureInfo culture)
        {
            string? translationContent = null;
            string? usedFileName = null;

            // Fire an event if a handler is assigned - maybe, it provides the file content
            if (OnFileContentNeeded != null)
            {
                FileContentNeededEventArgs args = new(config.Assembly, config.DefaultFile, culture);
                OnFileContentNeeded.Invoke(this, args);
                translationContent = args.Content;
            }

            // Look for translations in the config - maybe, one is present there
            if (string.IsNullOrEmpty(translationContent))
            {
                string? configRef = null;

                // Try the locale name
                if (config.Translations.TryGetValue(culture.Name.ToUpperInvariant(), out configRef) && !string.IsNullOrEmpty(configRef))
                {
                    translationContent = InternalLoadFileContent(config.Assembly, configRef, ref usedFileName);
                }

                // Try the language name
                if (string.IsNullOrEmpty(translationContent))
                {
                    if (config.Translations.TryGetValue(culture.TwoLetterISOLanguageName.ToUpperInvariant(), out configRef) && !string.IsNullOrEmpty(configRef))
                    {
                        translationContent = InternalLoadFileContent(config.Assembly, configRef, ref usedFileName);
                    }
                }

                // See maybe the default value is defined
                if (string.IsNullOrEmpty(translationContent))
                {
                    if (config.Translations.TryGetValue(TranslationConfiguration.KEY_TRANSLATION_DEFAULT, out configRef) && !string.IsNullOrEmpty(configRef))
                    {
                        translationContent = InternalLoadFileContent(config.Assembly, configRef, ref usedFileName);
                    }
                }
            }

            string? fileExtension = Path.GetExtension(config.DefaultFile);

            // If the content has not been loaded, try some heuristics
            if (string.IsNullOrEmpty(translationContent))
            {
                string filename = config.DefaultFile;

                string fileBase = Path.GetFileNameWithoutExtension(filename);

                // Here, we attempt to guess the filename and load data from there.

                // Try the full culture name first
                filename = string.Concat(fileBase, ".", culture.Name, fileExtension);
                translationContent = InternalLoadFileContent(config.Assembly, filename, ref usedFileName);

                // If not loaded, try just the language name
                if (string.IsNullOrEmpty(translationContent))
                {
                    filename = string.Concat(fileBase, ".", culture.TwoLetterISOLanguageName, fileExtension);
                    translationContent = InternalLoadFileContent(config.Assembly, filename, ref usedFileName);
                }

                // We try loading the data from the default file only for a default culture
                if (string.IsNullOrEmpty(translationContent) && (culture == _culture))
                {
                    translationContent = InternalLoadFileContent(config.Assembly, config.DefaultFile, ref usedFileName);
                }
            }

            if (string.IsNullOrEmpty(translationContent))
                return null;

            // File extension is used to create an appropriate parser
            return LoadTranslation(translationContent!, fileExtension)?.SetOrigin(config.Assembly, usedFileName);
        }

        /// <summary>
        /// Loads content of the file.
        /// </summary>
        /// <param name="assembly">An optional assembly where the file should be looked for.</param>
        /// <param name="filename">The filename to load the data from.</param>
        /// <param name="usedFileName">Becomes set to the filename actually used if loading was successful. This filename may contain a path if loading was performed from the disk.</param>
        /// <param name="originalFile">An optional reference to the file, from which the translation was loaded.</param>
        /// <returns>File content if the file was found and loaded and <see langword="null"/> otherwise.</returns>
        private string? InternalLoadFileContent(Assembly? assembly, string filename, ref string? usedFileName, string? originalFile = null)
        {
            string? fileContent = null;

            // Try to load the file from the disk.
            // The disk is checked first so that a translation provided on the disk can override the translation from resources (useful for translators to test their work).
            if (LoadFromDisk)
            {
                // If the path is absolute, we only try this file and return
                if (Path.IsPathRooted(filename))
                {
                    usedFileName = filename;
                    return Utils.ReadFileFromDisk(filename);
                }

                string tryFileName;

                // Try the file in the FilesLocation directory if one is specified
                if (!string.IsNullOrEmpty(FilesLocation))
                {
                    tryFileName = Path.Combine(FilesLocation, filename);

                    fileContent = Utils.ReadFileFromDisk(tryFileName);
                    if (fileContent is not null)
                    {
                        usedFileName = tryFileName;
                        return fileContent;
                    }
                }
                else
                {
                    string? baseDir;

                    // Try the directory of the original file, if it was specified.
                    if (!string.IsNullOrEmpty(originalFile))
                    {
                        baseDir = Path.GetDirectoryName(originalFile);
                        if (!string.IsNullOrEmpty(baseDir))
                        {
                            tryFileName = Path.Combine(baseDir, filename);

                            fileContent = Utils.ReadFileFromDisk(tryFileName);
                            if (fileContent is not null)
                            {
                                usedFileName = tryFileName;
                                return fileContent;
                            }
                        }
                    }

                    // Try the directory of the main EXE file
                    baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    if (!string.IsNullOrEmpty(baseDir))
                    {
                        tryFileName = Path.Combine(baseDir, filename);

                        fileContent = Utils.ReadFileFromDisk(tryFileName);
                        if (fileContent is not null)
                        {
                            usedFileName = tryFileName;
                            return fileContent;
                        }

                    }

                    // The last resort - try to load "as is" (from the current directory)
                    fileContent = Utils.ReadFileFromDisk(filename);
                    if (fileContent is not null)
                    {
                        usedFileName = filename;
                        return fileContent;
                    }
                }
            }

            // Try to load the file from the assembly
            if (assembly is not null)
            {
                string asmFileName = filename.Replace('/', '.').Replace('\\', '.');

                fileContent = ReadFileFromResource(assembly, asmFileName);

                if (fileContent is not null)
                {
                    usedFileName = asmFileName;
                    return fileContent;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks whether the entry is usable.
        /// </summary>
        /// <param name="entry">The entry to check.</param>
        /// <param name="originalAssembly">An optional reference to the assembly, from which the translation was loaded.</param>
        /// <param name="originalFile">An optional reference to the file, from which the translation was loaded.</param>
        /// <returns><see langword="true"/> if the entry is usable and <see langword="false"/> otherwise.</returns>
        private bool TranslationEntryAcceptable(TranslationEntry entry, Assembly? originalAssembly, string? originalFile)
        {
            // We can use a translation entry when either it has the text specified or when there exists a reference that can be used
            if (entry.Text is not null)
                return true;
            if (entry.Reference is not null)
            {
                string? usedFile = null;
                string? referencedText = InternalLoadFileContent(originalAssembly, entry.Reference, ref usedFile, originalFile);
                if (!string.IsNullOrEmpty(referencedText))
                {
                    entry.Text = referencedText;
                    return true;
                }
            }

            return false;
        }

        private TranslationEntry FireTranslationValueFound(CultureInfo culture, string key, TranslationEntry entry, Assembly? originalAssembly, string? originalFile, TemplateStringEscaping templateEscapeMode)
        {
            if (OnTranslationValueFound is not null)
            {
                TranslationValueEventArgs args = new(culture, key);

                try
                {
                    entry.Lock();
                    OnTranslationValueFound.Invoke(this, args);
                }
                finally
                {
                    entry.Unlock();
                }
                // If the handler has provided the entry, validate and return it.
                if (args.Entry == entry)
                    return entry;

                if (args.Entry is not null && TranslationEntryAcceptable(args.Entry, originalAssembly, originalFile))
                    return args.Entry;

                // If just a text was provided - great, we create an entry based on this text.
                if (args.Text is not null || args.EscapedText is not null)
                    return EntryFromEventArgs(args, templateEscapeMode);
            }

            return entry;
        }

        private TranslationEntry FireTranslationValueNotFound(CultureInfo culture, string key, TemplateStringEscaping templateEscapeMode)
        {
            if (OnTranslationValueNotFound is not null)
            {
                TranslationValueEventArgs args = new(culture, key);
                OnTranslationValueNotFound.Invoke(this, args);

                // If the handler has provided the entry, validate and return it.
                // When a new entry is provided, we accept entries only with the text but not with a reference (a handler should resolve references itself).
                if (args.Entry is not null && (args.Entry.Text is not null) && args.Entry.Reference is null)
                    return args.Entry;

                // If just a text was provided - great, we create an entry based on this text.
                if (args.Text is not null || args.EscapedText is not null)
                    return EntryFromEventArgs(args, templateEscapeMode);
            }

            // Nothing was found, return an empty entry.
            return TranslationEntry.Empty;
        }
    }
}
