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

using FluentAssertions;
using Microsoft.BizTalk.XPath;
using Xunit;

namespace Be.Stateless.BizTalk.XPath
{
	public class XPathCollectionFactoryFixture
	{
		[Fact]
		public void CreateFromMultiSegmentXPath()
		{
			var segments = new[] {
				$"/*[local-name()='LevelOne' and namespace-uri()='{NS}:1']",
				$"/*[local-name()='LevelTwo' and namespace-uri()='{NS}:2']",
				$"/*[local-name()='Parts' and namespace-uri()='{NS}:3']"
			};
			var bodyXPath = segments[0] + segments[1] + segments[2];

			XPathCollectionFactory.Create(bodyXPath)
				.Should().BeEquivalentTo(
					new XPathCollection {
						segments[0],
						segments[0] + segments[1],
						segments[0] + segments[1] + segments[2]
					});
		}

		[Fact]
		public void CreateFromSingleSegmentXPath()
		{
			var bodyXPath = $"/*[local-name()='Envelope' and namespace-uri()='{NS}:1']";

			XPathCollectionFactory.Create(bodyXPath)
				.Should().BeEquivalentTo(
					new XPathCollection {
						bodyXPath
					});
		}

		private const string NS = "urn:envelope:dummy";
	}
}
