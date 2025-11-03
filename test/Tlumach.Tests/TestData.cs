using Tlumach.Base;

using Xunit.Sdk;

namespace Tlumach.Tests
{
    class TemplateExpressionTestData : TheoryData<string, TemplateStringEscaping, bool?>
    {
        public TemplateExpressionTestData()
        {
            var testConditions = new (string input, TemplateStringEscaping escaping, bool? expected)[]
            {
                // Basic matches
                ("{}", TemplateStringEscaping.None, false),
                ("{abc}", TemplateStringEscaping.None, false),
                ("a { b } c", TemplateStringEscaping.None, false),
                ("} {", TemplateStringEscaping.None, false),
                ("{ }", TemplateStringEscaping.None, false),

                ("{}", TemplateStringEscaping.Backslash, false),
                ("{abc}", TemplateStringEscaping.Backslash, false),
                ("a { b } c", TemplateStringEscaping.Backslash, false),
                ("} {", TemplateStringEscaping.Backslash, false),
                ("{ }", TemplateStringEscaping.Backslash, false),

                ("{}", TemplateStringEscaping.Arb, true),
                ("{abc}", TemplateStringEscaping.Arb, true),
                ("a { b } c", TemplateStringEscaping.Arb, true),
                ("} {", TemplateStringEscaping.Arb, null), // Fails because { isn't closed
                ("{ }", TemplateStringEscaping.Arb, true),

                ("{}", TemplateStringEscaping.DotNet, true),
                ("{abc}", TemplateStringEscaping.DotNet, true),
                ("a { b } c", TemplateStringEscaping.DotNet, true),
                ("} {", TemplateStringEscaping.DotNet, null), // Fails because { isn't closed
                ("{ }", TemplateStringEscaping.DotNet, true),

                // Duplicated braces (ignored)
                ("{{}}", TemplateStringEscaping.None, false),
                ("{{abc}}", TemplateStringEscaping.None, false),
                ("a {{ b }} c", TemplateStringEscaping.None, false),
                ("{{ { } }}", TemplateStringEscaping.None, false),
                ("a { {{b}} } c", TemplateStringEscaping.None, false),

                ("{{}}", TemplateStringEscaping.Backslash, false),
                ("{{abc}}", TemplateStringEscaping.Backslash, false),
                ("a {{ b }} c", TemplateStringEscaping.Backslash, false),
                ("{{ { } }}", TemplateStringEscaping.Backslash, false),
                ("a { {{b}} } c", TemplateStringEscaping.Backslash, false),

                ("{{}}", TemplateStringEscaping.Arb,  true),
                ("{{abc}}", TemplateStringEscaping.Arb, true),
                ("a {{ b }} c", TemplateStringEscaping.Arb, true),
                ("{{ { } }}", TemplateStringEscaping.Arb, true),
                ("a { {{b}} } c", TemplateStringEscaping.Arb, true),

                ("{{}}", TemplateStringEscaping.DotNet, false),
                ("{{abc}}", TemplateStringEscaping.DotNet, false),
                ("a {{ b }} c", TemplateStringEscaping.DotNet, false),
                ("{{ { } }}", TemplateStringEscaping.DotNet, true), // The inner { } is valid
                ("a { {{b}} } c", TemplateStringEscaping.DotNet, true), // The outer { } is valid

                // Quotes (ignore brackets inside)
                ("'{abc}'", TemplateStringEscaping.None, false),
                ("a '{ b }' c", TemplateStringEscaping.None, false),
                ("a { 'b' } c", TemplateStringEscaping.None, false),

                ("'{abc}'", TemplateStringEscaping.Backslash, false),
                ("a '{ b }' c", TemplateStringEscaping.Backslash, false),
                ("a { 'b' } c", TemplateStringEscaping.Backslash, false),

                ("'{abc}'", TemplateStringEscaping.Arb, false),
                ("a '{ b }' c", TemplateStringEscaping.Arb, false),
                ("a { 'b' } c", TemplateStringEscaping.Arb, true), // Brackets are outside quotes

                ("'{abc}'", TemplateStringEscaping.DotNet, true),
                ("a '{ b }' c", TemplateStringEscaping.DotNet, true),
                ("a { 'b' } c",TemplateStringEscaping.DotNet,  true), // Brackets are outside quotes

                // Duplicated quotes (escaped)
                ("''{abc}''", TemplateStringEscaping.None, false), // '' is not a toggle, so { } is valid
                ("a ''{ b }'' c", TemplateStringEscaping.None, false),
                ("a '{ '' }' c", TemplateStringEscaping.None, false),
                ("a { '' } c", TemplateStringEscaping.None, false),

                ("''{abc}''", TemplateStringEscaping.Backslash, false), // '' is not a toggle, so { } is valid
                ("a ''{ b }'' c", TemplateStringEscaping.Backslash, false),
                ("a '{ '' }' c", TemplateStringEscaping.Backslash, false),
                ("a { '' } c", TemplateStringEscaping.Backslash, false),

                ("''{abc}''", TemplateStringEscaping.Arb, true), // '' is not a toggle, so { } is valid
                ("a ''{ b }'' c", TemplateStringEscaping.Arb, true),
                ("a '{ '' }' c", TemplateStringEscaping.Arb, false), // { and } are inside quotes
                ("a { '' } c", TemplateStringEscaping.Arb, true), // '' is inside valid { }

                ("''{abc}''", TemplateStringEscaping.DotNet, true),
                ("a ''{ b }'' c",TemplateStringEscaping.DotNet,  true),
                ("a '{ '' }' c", TemplateStringEscaping.DotNet, true),
                ("a { '' } c", TemplateStringEscaping.DotNet, true),

                // Mixed and complex cases
                ("a { '}' } c", TemplateStringEscaping.None, false),
                ("a { '{' } c", TemplateStringEscaping.None, false),
                ("a '}' { b } c", TemplateStringEscaping.None, false),
                ("a '{' { b } c", TemplateStringEscaping.None, false),
                ("'{ { } }'", TemplateStringEscaping.None, false),
                ("abc", TemplateStringEscaping.None, false),
                ("", TemplateStringEscaping.None, false),
                ("{", TemplateStringEscaping.None, false),
                ("}", TemplateStringEscaping.None, false),
                ("{{", TemplateStringEscaping.None, false),
                ("}}", TemplateStringEscaping.None, false),

                ("a { '}' } c", TemplateStringEscaping.Backslash, false),
                ("a { '{' } c", TemplateStringEscaping.Backslash, false),
                ("a '}' { b } c", TemplateStringEscaping.Backslash, false),
                ("a '{' { b } c", TemplateStringEscaping.Backslash, false),
                ("'{ { } }'", TemplateStringEscaping.Backslash, false),
                ("abc", TemplateStringEscaping.Backslash, false),
                ("", TemplateStringEscaping.Backslash, false),
                ("{", TemplateStringEscaping.Backslash, false),
                ("}", TemplateStringEscaping.Backslash, false),
                ("{{", TemplateStringEscaping.Backslash, false),
                ("}}", TemplateStringEscaping.Backslash, false),

                ("a { '}' } c", TemplateStringEscaping.Arb,  true), // Quoted '}' doesn't count, final '}' does
                ("a { '{' } c", TemplateStringEscaping.Arb, true), // Quoted '{' doesn't count
                ("a '}' { b } c", TemplateStringEscaping.Arb, true), // Quoted '}' is fine
                ("a '{' { b } c", TemplateStringEscaping.Arb, true), // Quoted '{' is fine
                ("'{ { } }'", TemplateStringEscaping.Arb, false), // Quoted sequence is fine
                ("abc", TemplateStringEscaping.Arb, false),
                ("", TemplateStringEscaping.Arb, false),
                ("{", TemplateStringEscaping.Arb, null),
                ("}", TemplateStringEscaping.Arb, null),
                ("{{", TemplateStringEscaping.Arb, null),
                ("}}", TemplateStringEscaping.Arb, null),

                ("a { '}' } c", TemplateStringEscaping.DotNet, true),
                ("a { '{' } c", TemplateStringEscaping.DotNet, true),
                ("a '}' { b } c", TemplateStringEscaping.DotNet, null),
                ("a '{' { b } c", TemplateStringEscaping.DotNet, true),
                ("'{ { } }'", TemplateStringEscaping.DotNet, true),
                ("'{ {a} }'", TemplateStringEscaping.DotNet, true),
                ("abc", TemplateStringEscaping.DotNet, false),
                ("", TemplateStringEscaping.DotNet, false),
                ("{", TemplateStringEscaping.DotNet, null),
                ("}", TemplateStringEscaping.DotNet, null),
                ("{{", TemplateStringEscaping.DotNet, false),
                ("}}", TemplateStringEscaping.DotNet, false),
            };
            foreach (var testCondition in testConditions)
            {
                Add(testCondition.input, testCondition.escaping, testCondition.expected);
            }
        }
    }
}
