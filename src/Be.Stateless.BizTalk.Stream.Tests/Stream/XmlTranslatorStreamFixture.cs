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

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Be.Stateless.IO.Extensions;
using Be.Stateless.Linq.Extensions;
using Be.Stateless.Xml.XPath.Extensions;
using FluentAssertions;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Be.Stateless.BizTalk.Stream
{
	public class XmlTranslatorStreamFixture
	{
		[Fact]
		public void AbsorbXmlDeclaration()
		{
			using (var reader = new StreamReader(
				new XmlTranslatorStream(
					XmlReader.Create(new StringReader(@"<?xml version='1.0'?><test att='22'>value</test>")),
					Encoding.Default,
					new XmlNamespaceTranslation[] { },
					XmlTranslationRequirements.AbsorbXmlDeclaration)))
			{
				reader.ReadToEnd().Should().Be("<test att=\"22\">value</test>");
			}
		}

		[Fact]
		public void ChangeXmlEncoding()
		{
			using (var reader = new StreamReader(
				new XmlTranslatorStream(
					XmlReader.Create(new StringReader(@"<?xml version='1.0'?><test att='22'>value</test>")),
					Encoding.GetEncoding("iso-8859-1"),
					new XmlNamespaceTranslation[] { },
					XmlTranslationRequirements.Default)))
			{
				reader.ReadToEnd().Should().Be("<?xml version=\"1.0\" encoding=\"iso-8859-1\"?><test att=\"22\">value</test>");
			}
		}

		[Fact]
		public void DoesNotAbsorbXmlDeclaration()
		{
			using (var reader = new StreamReader(
				new XmlTranslatorStream(
					XmlReader.Create(new StringReader(@"<?xml version='1.0'?><test att='22'>value</test>")),
					Encoding.UTF8,
					new XmlNamespaceTranslation[] { },
					XmlTranslationRequirements.Default)))
			{
				reader.ReadToEnd().Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><test att=\"22\">value</test>");
			}
		}

		[Fact]
		public void DoesNotOutputXmlDeclarationIfOriginallyMissing()
		{
			using (var reader = new StreamReader(
				new XmlTranslatorStream(
					XmlReader.Create(new StringReader(@"<test att='22'>value</test>")),
					Encoding.UTF8,
					new XmlNamespaceTranslation[] { },
					XmlTranslationRequirements.Default)))
			{
				reader.ReadToEnd().Should().Be("<test att=\"22\">value</test>");
			}
		}

		[Fact]
		public void InputXmlReaderCannotBeMovedToContent()
		{
			var inputXmlReader = XmlReader.Create(new StringReader("<payload>dummy</payload>"));
			var stream = new XmlTranslatorStream(inputXmlReader, new[] { new XmlNamespaceTranslation { MatchingPatternString = string.Empty, ReplacementPattern = "urn:ns" } });
			// move to content after XmlTranslatorStream initialization to trick it
			inputXmlReader.MoveToContent();
			Invoking(() => stream.ReadToEnd())
				.Should().Throw<InvalidOperationException>()
				.WithMessage("There was no XML start tag open.");
		}

		[Fact]
		public void InputXmlReaderIfMovedToContentWillBeHandled()
		{
			var inputXmlReader = XmlReader.Create(new StringReader("<payload>dummy</payload>"));
			// move to content before XmlTranslatorStream initialization to let it workaround the issue
			inputXmlReader.MoveToContent();
			var stream = new XmlTranslatorStream(inputXmlReader, new[] { new XmlNamespaceTranslation { MatchingPatternString = string.Empty, ReplacementPattern = "urn:ns" } });
			stream.ReadToEnd().Should().Be("<payload xmlns=\"urn:ns\">dummy</payload>");
		}

		[Fact]
		public void ProcessAttributes()
		{
			using (var reader = XmlReader.Create(new StringReader(@"<test xmlns:ns='stuff' ns:att='22'>value</test>")))
			{
				var navigator = new XPathDocument(
					new XmlTranslatorStream(
						reader,
						Encoding.Default,
						new[] { new XmlNamespaceTranslation("stuff", "urn:test") },
						XmlTranslationRequirements.TranslateAttributeNamespace)).CreateNavigator();

				navigator.Select("/test/@ns:att", "ns=urn:test").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}

		[Fact]
		public void RemoveDefaultNamespace()
		{
			using (var reader = XmlReader.Create(new StringReader(@"<test xmlns='stuff' att='22'>value</test>")))
			{
				var navigator = new XPathDocument(
					new XmlTranslatorStream(
						reader,
						Encoding.Default,
						new[] { new XmlNamespaceTranslation("stuff", "") },
						XmlTranslationRequirements.Default)).CreateNavigator();

				navigator.Select("/test").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}

		[Fact]
		public void RemoveNamespace()
		{
			using (var reader = XmlReader.Create(new StringReader(@"<ns:test xmlns:ns='stuff' att='22'>value</ns:test>")))
			{
				var navigator = new XPathDocument(
					new XmlTranslatorStream(
						reader,
						Encoding.Default,
						new[] { new XmlNamespaceTranslation("stuff", "") },
						XmlTranslationRequirements.Default)).CreateNavigator();

				navigator.Select("/test").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}

		[Fact]
		[SuppressMessage("ReSharper", "StringLiteralTypo")]
		public void RemoveVersionFromWcfLobNamespaces()
		{
			const string input = @"<Receive xmlns='http://Microsoft.LobServices.Sap/2007/03/Idoc/3/ANY_IDOC//701/Receive'>
	<idocData>
		<EDI_DC40 xmlns='http://Microsoft.LobServices.Sap/2007/03/Types/Idoc/3/ANY_IDOC//701'>
			<IDOCTYP xmlns='http://Microsoft.LobServices.Sap/2007/03/Types/Idoc/Common/'>ANY_IDOC</IDOCTYP>
		</EDI_DC40>
	</idocData>
</Receive>";

			using (var reader = XmlReader.Create(new StringReader(input)))
			{
				var navigator = new XPathDocument(
					new XmlTranslatorStream(
						reader,
						Encoding.Default,
						new[] {
							new XmlNamespaceTranslation(
								@"http://Microsoft\.LobServices\.Sap/2007/03(/Types)?/Idoc(?:/\d)/(\w+)/(?:/\d{3})(/\w+)?",
								"http://Microsoft.LobServices.Sap/2007/03$1/Idoc/$2$3")
						},
						XmlTranslationRequirements.Default)).CreateNavigator();

				navigator.Select("//s0:*", "s0=http://Microsoft.LobServices.Sap/2007/03/Idoc/ANY_IDOC/Receive").Cast<XPathNavigator>().Should().HaveCount(2);
				navigator.Select("//s1:*", "s1=http://Microsoft.LobServices.Sap/2007/03/Types/Idoc/ANY_IDOC").Cast<XPathNavigator>().Should().HaveCount(1);
				navigator.Select("//s2:*", "s2=http://Microsoft.LobServices.Sap/2007/03/Types/Idoc/Common/").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}

		[Fact]
		public void ReplaceDefaultNamespace()
		{
			using (var reader = XmlReader.Create(new StringReader(@"<test xmlns='stuff' att='22'>value</test>")))
			{
				var navigator = new XPathDocument(
					new XmlTranslatorStream(
						reader,
						Encoding.Default,
						new[] { new XmlNamespaceTranslation("stuff", "urn:test") },
						XmlTranslationRequirements.Default)).CreateNavigator();

				navigator.Select("/s0:test", "s0=urn:test").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}

		[Fact]
		public void ReplaceGlobalNamespace()
		{
			using (var reader = XmlReader.Create(new StringReader(@"<testField att='22'>value</testField>")))
			{
				var navigator = new XPathDocument(
						new XmlTranslatorStream(
							reader,
							Encoding.Default,
							new[] { new XmlNamespaceTranslation(string.Empty, "urn:test") },
							XmlTranslationRequirements.Default))
					.CreateNavigator();

				navigator.Select("/s0:testField", "s0=urn:test").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}

		[Fact]
		public void ReplaceGlobalNamespaceWhenOtherNamespaceDeclarationsArePresent()
		{
			using (var reader = XmlReader.Create(new StringReader(@"<test><other xsi:nil='true' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' /></test>")))
			{
				var navigator = new XPathDocument(
					new XmlTranslatorStream(
						reader,
						Encoding.Default,
						new[] { new XmlNamespaceTranslation(string.Empty, "urn:test") },
						XmlTranslationRequirements.Default)).CreateNavigator();

				navigator.Select("/s0:test/s0:other/@xsi:nil", "s0=urn:test", "xsi=http://www.w3.org/2001/XMLSchema-instance").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}

		[Fact]
		public void ReplaceNamespace()
		{
			using (var reader = XmlReader.Create(new StringReader(@"<ns:test xmlns:ns='stuff' att='22'>value</ns:test>")))
			{
				var navigator = new XPathDocument(
					new XmlTranslatorStream(
						reader,
						Encoding.Default,
						new[] { new XmlNamespaceTranslation("stuff", "urn:test") },
						XmlTranslationRequirements.Default)).CreateNavigator();

				navigator.Select("/s0:test", "s0=urn:test").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}

		[Fact]
		[SuppressMessage("ReSharper", "StringLiteralTypo")]
		public void RestoreVersionInWcfLobNamespaces()
		{
			const string input = @"<Send xmlns='http://Microsoft.LobServices.Sap/2007/03/Idoc/ANY_IDOC/Send'>
	<idocData>
		<EDI_DC40 xmlns='http://Microsoft.LobServices.Sap/2007/03/Types/Idoc/ANY_IDOC'>
			<IDOCTYP xmlns='http://Microsoft.LobServices.Sap/2007/03/Types/Idoc/Common/'>ANY_IDOC</IDOCTYP>
		</EDI_DC40>
	</idocData>
</Send>";

			using (var reader = XmlReader.Create(new StringReader(input)))
			{
				var navigator = new XPathDocument(
					new XmlTranslatorStream(
						reader,
						Encoding.Default,
						new[] {
							new XmlNamespaceTranslation(
								@"http://Microsoft\.LobServices\.Sap/2007/03/((?:Types/)?Idoc(?!/Common))/(\w+)(/Send)?",
								"http://Microsoft.LobServices.Sap/2007/03/$1/3/$2//701$3")
						},
						XmlTranslationRequirements.Default)).CreateNavigator();

				navigator.Select("//s0:*", "s0=http://Microsoft.LobServices.Sap/2007/03/Idoc/3/ANY_IDOC//701/Send").Cast<XPathNavigator>().Should().HaveCount(2);
				navigator.Select("//s1:*", "s1=http://Microsoft.LobServices.Sap/2007/03/Types/Idoc/3/ANY_IDOC//701").Cast<XPathNavigator>().Should().HaveCount(1);
				navigator.Select("//s2:*", "s2=http://Microsoft.LobServices.Sap/2007/03/Types/Idoc/Common/").Cast<XPathNavigator>().Should().HaveCount(1);
			}
		}
	}

	internal static class XPathNavigatorExtensions
	{
		public static XPathNodeIterator Select(this XPathNavigator navigator, string xpath, params string[] namespaces)
		{
			var nsm = navigator.GetNamespaceManager();
			namespaces
				.Select(namespaceWithPrefix => namespaceWithPrefix.Split('='))
				.ForEach(splitString => nsm.AddNamespace(splitString[0], splitString[1]));
			return navigator.Select(xpath, nsm);
		}
	}
}
