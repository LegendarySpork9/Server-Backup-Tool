// Copyright © - Unpublished - Toby Hunter
using ServerBackupTool.API.Functions;

namespace ServerBackupTool.UnitTests.API.Functions
{
    [TestClass]
    public class ParameterFunctionTest
    {
        private class TwoStringModel
        {
            public string? Name { get; set; }
            public string? Value { get; set; }
        }

        private class NullPropertyModel
        {
            public string? Name { get; set; }
            public int? Count { get; set; }
        }

        private class ListModel
        {
            public string? Name { get; set; }
            public List<string>? Items { get; set; }
        }

        private enum TestEnum { Alpha, Beta }

        /// <summary>
        /// Checks that a model with string properties returns a formatted string.
        /// </summary>
        [TestMethod]
        public void FormatParameters_ModelWithStringProperties_ReturnsFormattedString()
        {
            TwoStringModel model = new()
            {
                Name = "test",
                Value = "abc"
            };

            string result = ParameterFunction.FormatParameters(model);

            Assert.IsTrue(
                result.Contains("\"Name: test\""),
                $"Expected result to contain '\"Name: test\"' but got '{result}'.");
            Assert.IsTrue(
                result.Contains("\"Value: abc\""),
                $"Expected result to contain '\"Value: abc\"' but got '{result}'.");
        }

        /// <summary>
        /// Checks that a null model returns an empty string.
        /// </summary>
        [TestMethod]
        public void FormatParameters_NullModel_ReturnsEmptyString()
        {
            string result = ParameterFunction.FormatParameters(null!);

            Assert.AreEqual(
                string.Empty,
                result);
        }

        /// <summary>
        /// Checks that a primitive int returns a quoted value.
        /// </summary>
        [TestMethod]
        public void FormatParameters_PrimitiveInt_ReturnsQuotedValue()
        {
            string result = ParameterFunction.FormatParameters(42);

            Assert.AreEqual(
                "\"42\"",
                result);
        }

        /// <summary>
        /// Checks that a primitive bool returns a quoted value.
        /// </summary>
        [TestMethod]
        public void FormatParameters_PrimitiveBool_ReturnsQuotedValue()
        {
            string result = ParameterFunction.FormatParameters(true);

            Assert.AreEqual(
                "\"True\"",
                result);
        }

        /// <summary>
        /// Checks that a string value returns a quoted value.
        /// </summary>
        [TestMethod]
        public void FormatParameters_StringValue_ReturnsQuotedValue()
        {
            string result = ParameterFunction.FormatParameters("hello");

            Assert.AreEqual(
                "\"hello\"",
                result);
        }

        /// <summary>
        /// Checks that an enum value returns a quoted value.
        /// </summary>
        [TestMethod]
        public void FormatParameters_EnumValue_ReturnsQuotedValue()
        {
            string result = ParameterFunction.FormatParameters(TestEnum.Alpha);

            Assert.AreEqual(
                "\"Alpha\"",
                result);
        }

        /// <summary>
        /// Checks that a model with a null property outputs the property name with null.
        /// </summary>
        [TestMethod]
        public void FormatParameters_ModelWithNullProperty_OutputsNull()
        {
            NullPropertyModel model = new()
            {
                Name = "test",
                Count = null
            };

            string result = ParameterFunction.FormatParameters(model);

            Assert.IsTrue(
                result.Contains("\"Name: test\""),
                $"Expected result to contain '\"Name: test\"' but got '{result}'.");
            Assert.IsTrue(
                result.Contains("\"Count: null\""),
                $"Expected result to contain '\"Count: null\"' but got '{result}'.");
        }

        /// <summary>
        /// Checks that a model with a list property outputs each item separately.
        /// </summary>
        [TestMethod]
        public void FormatParameters_ModelWithListProperty_OutputsEachItem()
        {
            ListModel model = new()
            {
                Name = "group",
                Items = ["a", "b"]
            };

            string result = ParameterFunction.FormatParameters(model);

            Assert.IsTrue(
                result.Contains("\"Name: group\""),
                $"Expected result to contain '\"Name: group\"' but got '{result}'.");
            Assert.IsTrue(
                result.Contains("\"Items: a\""),
                $"Expected result to contain '\"Items: a\"' but got '{result}'.");
            Assert.IsTrue(
                result.Contains("\"Items: b\""),
                $"Expected result to contain '\"Items: b\"' but got '{result}'.");
        }

        /// <summary>
        /// Checks that the formatted output does not end with a comma.
        /// </summary>
        [TestMethod]
        public void FormatParameters_DoesNotEndWithComma()
        {
            TwoStringModel model = new()
            {
                Name = "x",
                Value = "y"
            };

            string result = ParameterFunction.FormatParameters(model);

            Assert.IsFalse(
                result.TrimEnd().EndsWith(','),
                $"Expected result not to end with a comma but got '{result}'.");
        }
    }
}
