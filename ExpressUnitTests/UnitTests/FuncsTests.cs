using ExpressControls;

namespace ExpressTests.UnitTests
{
    public class FuncsTests
    {
        public class ConvertQParamsToStringTests
        {
            [Fact]
            public void ConvertQParamsToString_EmptyDictionary_ReturnsEmptyString()
            {
                var queryParams = new Dictionary<string, string>();
                var result = Funcs.ConvertQParamsToString(queryParams);

                Assert.Equal(string.Empty, result);
            }

            [Fact]
            public void ConvertQParamsToString_SingleParameter_ReturnsCorrectFormat()
            {
                var queryParams = new Dictionary<string, string> { { "key", "value" } };
                var result = Funcs.ConvertQParamsToString(queryParams);

                Assert.Equal("key=value", result);
            }

            [Fact]
            public void ConvertQParamsToString_MultipleParameters_ReturnsJoinedWithAmpersand()
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "name", "john" },
                    { "age", "25" },
                    { "city", "newyork" },
                };
                var result = Funcs.ConvertQParamsToString(queryParams);

                Assert.Contains("name=john", result);
                Assert.Contains("age=25", result);
                Assert.Contains("city=newyork", result);
                Assert.Equal(2, result.Count(c => c == '&'));
            }

            [Fact]
            public void ConvertQParamsToString_SpecialCharactersInKey_EscapesCorrectly()
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "key with spaces", "value" },
                    { "key&with&ampersands", "value" },
                };
                var result = Funcs.ConvertQParamsToString(queryParams);

                Assert.Contains("key%20with%20spaces=value", result);
                Assert.Contains("key%26with%26ampersands=value", result);
            }

            [Fact]
            public void ConvertQParamsToString_SpecialCharactersInValue_EscapesCorrectly()
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "message", "hello world" },
                    { "data", "value&with&ampersands" },
                    { "encoded", "special+chars%here" },
                };
                var result = Funcs.ConvertQParamsToString(queryParams);

                Assert.Contains("message=hello%20world", result);
                Assert.Contains("data=value%26with%26ampersands", result);
                Assert.Contains("encoded=special%2Bchars%25here", result);
            }

            [Fact]
            public void ConvertQParamsToString_EmptyKeyAndValue_HandlesCorrectly()
            {
                var queryParams = new Dictionary<string, string> { { "key", "" }, { "", "value" } };
                var result = Funcs.ConvertQParamsToString(queryParams);

                Assert.Contains("=", result);
                Assert.Contains("key=", result);
                Assert.Contains("=value", result);
            }

            [Fact]
            public void ConvertQParamsToString_UnicodeCharacters_EscapesCorrectly()
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "name", "José" },
                    { "city", "北京" },
                    { "emoji", "🚀" },
                };
                var result = Funcs.ConvertQParamsToString(queryParams);

                Assert.Contains("name=Jos%C3%A9", result);
                Assert.Contains("city=%E5%8C%97%E4%BA%AC", result);
                Assert.Contains("emoji=%F0%9F%9A%80", result);
            }

            [Fact]
            public void ConvertQParamsToString_UrlUnsafeCharacters_EscapesAll()
            {
                var queryParams = new Dictionary<string, string>
                {
                    { "unsafe", "!@#$%^&*()+=[]{}|\\:;\"'<>?,./" },
                };
                var result = Funcs.ConvertQParamsToString(queryParams);

                Assert.DoesNotContain("!@#$%^&*()+=[]{}|\\:;\"'<>?,./", result);
                Assert.StartsWith("unsafe=", result);
                Assert.Contains("%", result);
            }
        }
    }
}
