// <copyright file="ArbParserTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "BaseJson")]
    [Trait("Category", "Arb")]
    public class ArbParserTests
    {

        const string TestFilesPath = "..\\..\\..\\TestData\\Arb";

        static ArbParserTests()
        {
            ArbParser.Use();
        }

        [Fact]
        public void ShouldLoadSimpleValidConfig()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "SimpleValidConfig.arbcfg"));
            Assert.Equal("EmptyDefault.arb", manager.DefaultConfiguration?.DefaultFile);
        }

        [Fact]
        public void ShouldFailOnInvalidConfig()
        {
            TranslationManager manager;
            Assert.Throws<TextParseException>(() => manager = new TranslationManager(Path.Combine(TestFilesPath, "SimpleInvalidConfig.arbcfg")));
        }

        [Fact]
        public void ShouldLoadValidConfigWithTranslations()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            Assert.Equal("Strings.arb", manager.DefaultConfiguration?.DefaultFile);
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE-AT"), "de-AT translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("DE"), "de translation not found");
            Assert.True(manager.DefaultConfiguration?.Translations.ContainsKey("default"), "default translation not found");
        }

        [Fact]
        public void ShouldLoadValidConfig()
        {
            ArbParser? parser = FileFormats.GetParser(".arb") as ArbParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfig.arbcfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("Strings.arb", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("Hello"));
        }

        [Fact]
        public void ShouldLoadValidConfigWithGroups()
        {
            ArbParser? parser = FileFormats.GetParser(".arb") as ArbParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            TranslationTree? tree = parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigWithGroups.arbcfg"), TestFilesPath, out config);
            Assert.NotNull(tree);
            Assert.NotNull(config);
            Assert.Equal("StringsWithGroups.arb", config.DefaultFile);
            Assert.True(tree.RootNode.Keys.Count > 0);
            Assert.True(tree.RootNode.Keys.ContainsKey("hello"));
            Assert.NotNull(tree.FindNode("ui"));
            Assert.True(tree.RootNode.ChildNodes.ContainsKey("logs"));
            TranslationTreeNode? node = tree.FindNode("logs.server");
            Assert.NotNull(node);
            Assert.True(node.Keys.ContainsKey("started"));
        }

        [Fact]
        public void ShouldFailOnValidConfigWithUnknownExt()
        {
            ArbParser? parser = FileFormats.GetParser(".arb") as ArbParser;
            Assert.NotNull(parser);
            TranslationConfiguration? config;
            Assert.Throws<ParserLoadException>(() => parser.LoadTranslationStructure(Path.Combine(TestFilesPath, "ValidConfigUnknownExt.arbcfg"), TestFilesPath, out config));
        }

        [Fact]
        public void ShouldGetKeyDefaultLanguage()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFile()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToBasicLocale()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-CH");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToDefaultLocale()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithTranslations.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("StringsWithTranslations.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("cz");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hello", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyFromGroup()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigWithGroups.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("StringsWithGroups.arb", manager.DefaultConfiguration?.DefaultFile);

            TranslationEntry entry = manager.GetValue("logs.server.started", new CultureInfo("sk"));
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Started", entry.Text);
        }

        [Fact]
        public void ShouldGetKeyExistingLocaleFileWithSearch()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("Strings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("sk");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Ahoj", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToBasicLocaleWithSearch()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("Strings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-CH");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Hallo", entry.Text);
        }

        [Fact]
        public void ShouldFallbackToBasicLocaleForUnknownKey()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfig.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("Strings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Willkommen", entry.Text);
        }

        [Fact]
        public void ShouldFallbackWithExplicitLocale()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigExplicitLocale.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("DEDEStrings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Willkommen", entry.Text);
            entry = manager.GetValue("Bye");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.True(entry.Text?.StartsWith("Tsch", StringComparison.Ordinal));
        }

        [Fact]
        public void ShouldFallbackWithExplicitLocaleNoBasic()
        {
            var manager = new TranslationManager(Path.Combine(TestFilesPath, "ValidConfigExplicitLocaleNoBasic.arbcfg"));
            manager.LoadFromDisk = true;
            manager.FilesLocation = TestFilesPath;
            Assert.Equal("FRFRStrings.arb", manager.DefaultConfiguration?.DefaultFile);
            manager.CurrentCulture = new CultureInfo("de-AT");
            TranslationEntry entry = manager.GetValue("Hello");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Servus", entry.Text);
            entry = manager.GetValue("Welcome");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Willkommen", entry.Text);
            entry = manager.GetValue("Bye");
            Assert.False(string.IsNullOrEmpty(entry.Text));
            Assert.Equal("Au revoir", entry.Text);
        }
    }
}
