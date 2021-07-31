#region Copyright & License

// Copyright © 2012 - 2021 François Chabot
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

#endregion

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using FluentAssertions;
using Xunit;

namespace Be.Stateless.BizTalk.Stream
{
	[SuppressMessage("ReSharper", "ConvertToUsingDeclaration")]
	[SuppressMessage("ReSharper", "ArrangeRedundantParentheses")]
	[SuppressMessage("Style", "IDE0063:Use simple 'using' statement")]
	public class XmlEnvelopeDecodingStreamFixture
	{
		[Fact]
		public void DecodeNestedEnvelopeWithMixedNamespaceWithoutParts()
		{
			var envelope = $"<s0:LevelOne xmlns:s0='{NS}:1' />";

			var bodyXPath = $"/*[local-name()='LevelOne' and namespace-uri()='{NS}:1']"
				+ $"/*[local-name()='LevelTwo' and namespace-uri()='{NS}:2']"
				+ $"/*[local-name()='Parts' and namespace-uri()='{NS}:3']";

			using (var stringReader = new StringReader(envelope))
			using (var xmlReader = XmlReader.Create(stringReader))
			using (var envelopeMutatorStream = new XmlEnvelopeDecodingStream(xmlReader, bodyXPath))
			{
				var result = XmlReader.Create(envelopeMutatorStream);
				result.MoveToContent();
				result.ReadOuterXml().Should().Be(
					$"<s0:LevelOne xmlns:s0=\"{NS}:1\">" + (
						$"<s0:LevelTwo xmlns:s0=\"{NS}:2\">" + (
							$"<s0:Parts xmlns:s0=\"{NS}:3\">" +
							"</s0:Parts>") +
						"</s0:LevelTwo>") +
					"</s0:LevelOne>");
			}
		}

		[Fact]
		public void DecodeNestedEnvelopeWithMixedNamespaceWithParts()
		{
			var envelope = $"<s0:LevelOne xmlns:s0=\"{NS}:1\">" + (
					$"<s0:LevelTwo xmlns:s0=\"{NS}:2\">" + (
						$"<s0:Parts xmlns:s0=\"{NS}:3\">" + (
							"<part-one></part-one>" +
							"<part-two></part-two>" +
							"<part-six></part-six>") +
						"</s0:Parts>") +
					"</s0:LevelTwo>") +
				"</s0:LevelOne>";

			var bodyXPath = $"/*[local-name()='LevelOne' and namespace-uri()='{NS}:1']"
				+ $"/*[local-name()='LevelTwo' and namespace-uri()='{NS}:2']"
				+ $"/*[local-name()='Parts' and namespace-uri()='{NS}:3']";

			using (var stringReader = new StringReader(envelope))
			using (var xmlReader = XmlReader.Create(stringReader))
			using (var envelopeMutatorStream = new XmlEnvelopeDecodingStream(xmlReader, bodyXPath))
			{
				var result = XmlReader.Create(envelopeMutatorStream);
				result.MoveToContent();
				result.ReadOuterXml().Should().Be(
					$"<s0:LevelOne xmlns:s0=\"{NS}:1\">" + (
						$"<s0:LevelTwo xmlns:s0=\"{NS}:2\">" + (
							$"<s0:Parts xmlns:s0=\"{NS}:3\">" + (
								"<part-one></part-one>" +
								"<part-two></part-two>" +
								"<part-six></part-six>") +
							"</s0:Parts>") +
						"</s0:LevelTwo>") +
					"</s0:LevelOne>");
			}
		}

		[Theory]
		[InlineData("<s0:LevelOne xmlns:s0='" + NS + "' />")]
		[InlineData("<s0:LevelOne xmlns:s0='" + NS + "'></s0:LevelOne>")]
		[InlineData("<s0:LevelOne xmlns:s0='" + NS + "'><s0:LevelTwo /></s0:LevelOne>")]
		[InlineData("<s0:LevelOne xmlns:s0='" + NS + "'><s0:LevelTwo></s0:LevelTwo></s0:LevelOne>")]
		[InlineData("<s0:LevelOne xmlns:s0='" + NS + "'><s0:LevelTwo><s0:Parts /></s0:LevelTwo></s0:LevelOne>")]
		[InlineData("<s0:LevelOne xmlns:s0='" + NS + "'><s0:LevelTwo><s0:Parts></s0:Parts></s0:LevelTwo></s0:LevelOne>")]
		public void DecodeNestedEnvelopeWithoutParts(string envelope)
		{
			var bodyXPath = $"/*[local-name()='LevelOne' and namespace-uri()='{NS}']"
				+ $"/*[local-name()='LevelTwo' and namespace-uri()='{NS}']"
				+ $"/*[local-name()='Parts' and namespace-uri()='{NS}']";

			using (var stringReader = new StringReader(envelope))
			using (var xmlReader = XmlReader.Create(stringReader))
			using (var envelopeMutatorStream = new XmlEnvelopeDecodingStream(xmlReader, bodyXPath))
			{
				var result = XmlReader.Create(envelopeMutatorStream);
				result.MoveToContent();
				result.ReadOuterXml().Should().Be(
					$"<s0:LevelOne xmlns:s0=\"{NS}\">" + (
						"<s0:LevelTwo>" + (
							"<s0:Parts>" +
							"</s0:Parts>") +
						"</s0:LevelTwo>") +
					"</s0:LevelOne>");
			}
		}

		[Fact]
		public void DecodeNestedEnvelopeWithParts()
		{
			var bodyXPath = $"/*[local-name()='LevelOne' and namespace-uri()='{NS}']"
				+ $"/*[local-name()='LevelTwo' and namespace-uri()='{NS}']"
				+ $"/*[local-name()='Parts' and namespace-uri()='{NS}']";

			var envelope = $"<s0:LevelOne xmlns:s0=\"{NS}\">" + (
					"<s0:LevelTwo>" + (
						"<s0:Parts>" + (
							"<part-one></part-one>" +
							"<part-two></part-two>" +
							"<part-six></part-six>") +
						"</s0:Parts>") +
					"</s0:LevelTwo>") +
				"</s0:LevelOne>";

			using (var stringReader = new StringReader(envelope))
			using (var xmlReader = XmlReader.Create(stringReader))
			using (var envelopeMutatorStream = new XmlEnvelopeDecodingStream(xmlReader, bodyXPath))
			{
				var result = XmlReader.Create(envelopeMutatorStream);
				result.MoveToContent();
				result.ReadOuterXml().Should().Be(
					$"<s0:LevelOne xmlns:s0=\"{NS}\">" + (
						"<s0:LevelTwo>" + (
							"<s0:Parts>" + (
								"<part-one></part-one>" +
								"<part-two></part-two>" +
								"<part-six></part-six>") +
							"</s0:Parts>") +
						"</s0:LevelTwo>") +
					"</s0:LevelOne>");
			}
		}

		[Theory]
		[InlineData("<s0:Envelope xmlns:s0='" + NS + "' />")]
		[InlineData("<s0:Envelope xmlns:s0='" + NS + "'></s0:Envelope>")]
		public void DecodeRootedEnvelopeWithoutParts(string envelope)
		{
			var bodyXPath = $"/*[local-name()='Envelope' and namespace-uri()='{NS}']";

			using (var stringReader = new StringReader(envelope))
			using (var xmlReader = XmlReader.Create(stringReader))
			using (var envelopeMutatorStream = new XmlEnvelopeDecodingStream(xmlReader, bodyXPath))
			{
				var result = XmlReader.Create(envelopeMutatorStream);
				result.MoveToContent();
				result.ReadOuterXml().Should().Be($"<s0:Envelope xmlns:s0=\"{NS}\"></s0:Envelope>");
			}
		}

		[Fact]
		public void DecodeRootedEnvelopeWithParts()
		{
			var bodyXPath = $"/*[local-name()='Envelope' and namespace-uri()='{NS}']";

			var envelope = $"<s0:Envelope xmlns:s0=\"{NS}\">" + (
					"<part-one></part-one>" +
					"<part-two></part-two>" +
					"<part-six></part-six>") +
				"</s0:Envelope>";

			using (var stringReader = new StringReader(envelope))
			using (var xmlReader = XmlReader.Create(stringReader))
			using (var envelopeMutatorStream = new XmlEnvelopeDecodingStream(xmlReader, bodyXPath))
			{
				var result = XmlReader.Create(envelopeMutatorStream);
				result.MoveToContent();
				result.ReadOuterXml().Should().Be(
					$"<s0:Envelope xmlns:s0=\"{NS}\">" + (
						"<part-one></part-one>" +
						"<part-two></part-two>" +
						"<part-six></part-six>") +
					"</s0:Envelope>");
			}
		}

		private const string NS = "urn:envelope:dummy";
	}
}
