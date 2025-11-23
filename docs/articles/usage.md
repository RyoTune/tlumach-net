# Usage of Tlumach

## The Ways to Use Tlumach

Tlumach can be used in different ways depending on your application type and the way the localized text is to be used:

1. XAML-based desktop and mobile .NET applications, use in XAML UIs. You can bind XAML attributes to translation units as shown below. This way, when the language is switched in the translation manager, UI elements get updated automatically.
2. Websites and Web, console, and server applications which output the text from code or web files. There, you can access generated <xref:Tlumach.TranslationUnit> instances in code to pick the text for current or specific locale. The use of generated <xref:Tlumach.TranslationUnit> objects ensures that there is no mistake made when referencing the text.
3. Any application. The basic way to access translations is to create and use an instance of the <xref:Tlumach.TranslationManager> class to retrieve specific translation units by key (a simple string). This class is always available and used internally by <xref:Tlumach.TranslationUnit> objects.

Each of the ways is documented below.

## Getting Started

It is a good idea to check [out] the samples that can be found in [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach-net/tree/main/samples) before proceeding with the below instructions.

### Starting via NuGet

Add a package reference to "Tlumach" to your project.

* via NuGet package manager GUI in Visual Studio;

* via the command line:

```cmd
dotnet add package Tlumach
```

* using the text editor - add the following reference to your project:
```xml
<ItemGroup>
    <PackageReference Include="Tlumach" Version="1.*" />
</ItemGroup>
```

### Starting with source code

1. Check out Tlumach from the [Tlumach repository on GitHub](https://github.com/Allied-Bits-Ltd/tlumach)
2. Add _Tlumach.Base_ and _Tlumach_ projects to your solution and reference them from your project(s).
3. If you are going to use Generator and generated translation units (e.g. in a XAML-based UI of your application), please follow the instructions in the [Generator](generator.md) topic.


