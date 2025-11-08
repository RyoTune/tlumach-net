// <copyright file="ArbParserTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "Arb")]
    public class ArbParserTests
    {
        [Theory]
        [ClassData(typeof(TemplateExpressionTestData))]
        public void ShouldDetectTemplatedString(string input, TemplateStringEscaping escaping, bool? expected)
        {
            ArbParser.TemplateEscapeMode = escaping;

            if (expected is null)
                Assert.Throws<GenericParserException>(() => ArbParser.StringHasParameters(input, escaping));
            else
                Assert.Equal(expected, ArbParser.StringHasParameters(input, escaping));
        }
    }
}
