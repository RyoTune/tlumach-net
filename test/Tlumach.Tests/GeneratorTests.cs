// <copyright file="GeneratorTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Tlumach.Generator;

namespace Tlumach.Tests
{
    public class GeneratorTests
    {
        const string TestFilesPath = "..\\..\\..\\TestData\\Generator";

        internal class TestGenerator : Tlumach.Generator.Generator
        {
            internal static string? GenerateClass(string path, string projectDir, string usingNamespace)
            {
                return GenerateClass(path, projectDir, usingNamespace);
            }
        }

        [Fact]
        public void ShouldGenerateEmptyClass()
        {
            var generator = new TestGenerator();
            //generator.GenerateClass()
        }
    }
}
