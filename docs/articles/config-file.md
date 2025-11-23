# Configuration files

Configuration files contain basic information for the translation manager to be able to load the translations. If you use Generator and generated translation units, the configuration file must also include a couple of lines for the generator to use.

One configuration file is needed for one [translation set](glossary.md).

## Configuration File Formats

Configuration file can be specified in one of the following formats:

* JSON (.jsoncfg, .arbcfg extensions) - the format and the syntax of the configuration file is the same for simple JSON and Arb formats
* INI (.cfg extension) - the syntax follows the one of simple INI files
* TOML (.tomlcfg extension) - the syntax follows the one of TOML files
* XML (.resxcfg) - simple XML syntax

The format of a particular configuration file is determined by Tlumach from the file extension.

Configuration files do _not_ need to be of the same [format](files-formats.md) as the translation files. It is perfectly correct to have the configuration file in INI format and translations in the Arb format.

For CSV and TSV files, use a configuration file in any supported format (INI will be enough).

Configuration files in any format may and should contain the same information; there are no entries specific to a certain translation file format.

## Configuration File Content

A sample .cfg file in INI format:

```ini
defaultFile=sample.arb
defaultFileLocale=en-UK
generatedClass=Strings
generatedNamespace=Tlumach.Sample
textProcessingMode=Arb

[translations]
de-AT=sample_de-AT.toml
de=sample_de.toml
pl=sample.pl.resx
sk=sample_sk.ini
hr=sample_hr.json
uk=sample_uk.tsv
```

Configuration files may contain entries in the root key and optionally, in the "translation" key.

* **defaultFile** - the only required entry. It references the [default file](glossary.md). The reference may include a relative path, which may be useful when [translation files](glossary.md) are stored in a dedicated directory.
* **defaultFileLocale** - an optional specifier of the locale of the default file. It is useful with CSV and TSV formats to choose the right column in the file that contains more than one column with translation units. Also, specifying the locale helps the translation manager to speed up loading of translation files in some scenarios.
* **generatedClass** - tells [Generator](generator.md) which name it should give to the class it generates. This is a required entry if your project uses Generator.
* **generatedNamespace** - tells [Generator](generator.md) which namespace the generated class should belong to. This is a required entry if your project uses Generator.
* **textProcessingMode** - an optional <xref:Tlumach.Base.TextFormat> value if it is different from the one that the parser uses by default. It is possible to change the value in runtime.
* **translations** - an optional **section** which includes references to [locale-specific files](glossary.md). Like in `defaultFile`, the references may include a relative path. The keys of the references under the `translations` section may be locale names or language codes as described in the [Files and Formats](files-formats.md) topic.

## Inclusion in Translation Projects

### Use with Generator

When a configuration file is added into a project with translations, add it as an additional file:

```xml
<ItemGroup>
    <AdditionalFiles Include="Sample.cfg" />
</ItemGroup>
```

In the properties of this file in the Visual Studio IDE, set "Build Action" to "C# Analyzer additional file".

This will let Generator see this file as configuration.

The configuration file will not be included in resources, but this is not needed - Generator will include the necessary information to the generated source code. If you for whatever reason need a copy in resources, you can make a copy of this file and include the copy into the project as an Embedded Resource and set its name as a resource using the LogicalName parameter:

```xml
<ItemGroup>
    <EmbeddedResource Include="SampleForResource.cfg">
        <LogicalName>Sample.cfg</LogicalName>
    </EmbeddedResource>
</ItemGroup>
```

### Use with Translation Files (no Generator)

If you are not using Generator, you will need the config file to initialize <xref:Tlumach.TranslationManager>. For this, include the configuration file as an Embedded Resource to your project and address it when calling a <xref:Tlumach.TranslationManager> constructor. If you enabled loading of files from the disk, you can omit embedding the file into resources and just use the <xref:Tlumach.TranslationManager> constructor that accepts the file path.
