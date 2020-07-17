#region Copyright & License

// Copyright © 2012 - 2020 François Chabot
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
using FluentAssertions;
using Xunit;

namespace Be.Stateless.BizTalk.Stream
{
	public class XmlTranslationSetConverterFixture
	{
		[Fact]
		public void CanConvertFrom()
		{
			var sut = new XmlTranslationSetConverter();
			sut.CanConvertFrom(typeof(string)).Should().BeTrue();
		}

		[Fact]
		public void CanConvertTo()
		{
			var sut = new XmlTranslationSetConverter();
			sut.CanConvertTo(typeof(string)).Should().BeTrue();
		}

		[Fact]
		public void Deserialize()
		{
			XmlTranslationSetConverter.Deserialize(XML).Should().Be(_translationSet);
		}

		[Fact]
		public void DeserializeEmptyString()
		{
			XmlTranslationSetConverter.Deserialize(string.Empty).Should().Be(XmlTranslationSet.Empty);
		}

		[Fact]
		public void Serialize()
		{
			XmlTranslationSetConverter.Serialize(_translationSet).Should().Be(XML.Replace(Environment.NewLine, string.Empty));
		}

		private const string XML = @"<xt:XmlTranslations override=""true"" xmlns:xt=""" + XmlTranslationSet.NAMESPACE + @""">
<xt:NamespaceTranslation matchingPattern=""sourceUrnA"" replacementPattern=""targetUrnA"" />
<xt:NamespaceTranslation matchingPattern=""sourceUrnB"" replacementPattern=""targetUrnB"" />
</xt:XmlTranslations>";

		private static readonly XmlTranslationSet _translationSet = new XmlTranslationSet {
			Override = true,
			Items = new[] {
				new XmlNamespaceTranslation("sourceUrnA", "targetUrnA"),
				new XmlNamespaceTranslation("sourceUrnB", "targetUrnB")
			}
		};
	}
}
