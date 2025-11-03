using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Tlumach.Base;

namespace Tlumach.Tests
{
    [Trait("Category", "Parser")]
    [Trait("Category", "Base")]
    public class BaseParserTests
    {

        [Theory]
        [InlineData(TemplateStringEscaping.Backslash, "TemplateStringEscaping.Backslash")]
        [InlineData(TemplateStringEscaping.DotNet, "TemplateStringEscaping.DotNet")]
        public void ShouldConvertTemplateEscapeModeToStringRight(TemplateStringEscaping templateEscapeMode, string expected)
        {
            TranslationConfiguration config = new(string.Empty, null, null, null, templateEscapeMode);
            Assert.Equal(expected, config.GetTemplateEscapeModeFullValue());

        }
    }
}
