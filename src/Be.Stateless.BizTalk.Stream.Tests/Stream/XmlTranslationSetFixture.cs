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
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;
using static Be.Stateless.BizTalk.DelegateFactory;

namespace Be.Stateless.BizTalk.Stream
{
	public class XmlTranslationSetFixture
	{
#if DEBUG
		[Fact]
#else
		[Fact(Skip = "Only to be run in DEBUG configuration.")]
#endif
		[SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
		public void CheckItemsUniquenessThrowsWhenConflictingReplacementPatterns()
		{
			Action(
					() => new XmlTranslationSet {
						Items = new[] {
							new XmlNamespaceTranslation("sourceUrnA", "targetUrnA1"),
							new XmlNamespaceTranslation("sourceUrnA", "targetUrnA2"),
							new XmlNamespaceTranslation("sourceUrnB", "targetUrnB1"),
							new XmlNamespaceTranslation("sourceUrnB", "targetUrnB2")
						}
					})
				.Should().Throw<ArgumentException>().WithMessage(
					"[sourceUrnA], [sourceUrnB] matchingPatterns have respectively the following conflicting replacementPatterns: " +
					"[targetUrnA1, targetUrnA2], [targetUrnB1, targetUrnB2].");
		}

		[Fact]
		public void UnionWithoutOverride()
		{
			var contextReplacementSet = new XmlTranslationSet {
				Override = false,
				Items = new[] {
					new XmlNamespaceTranslation("contextSourceUrn", "contextTargetUrn"),
					new XmlNamespaceTranslation("commonSourceUrn", "commonTargetUrn")
				}
			};
			var pipelineReplacementSet = new XmlTranslationSet {
				Items = new[] {
					new XmlNamespaceTranslation("pipelineSourceUrn", "pipelineTargetUrn"),
					new XmlNamespaceTranslation("commonSourceUrn", "commonTargetUrn")
				}
			};

			contextReplacementSet.Union(pipelineReplacementSet).Should().Be(
				new XmlTranslationSet {
					Override = false,
					Items = new[] {
						new XmlNamespaceTranslation("contextSourceUrn", "contextTargetUrn"),
						new XmlNamespaceTranslation("commonSourceUrn", "commonTargetUrn"),
						new XmlNamespaceTranslation("pipelineSourceUrn", "pipelineTargetUrn")
					}
				});
		}

		[Fact]
		public void UnionWithOverride()
		{
			var contextReplacementSet = new XmlTranslationSet {
				Override = true,
				Items = new[] { new XmlNamespaceTranslation("contextSourceUrn", "contextTargetUrn") }
			};
			var pipelineReplacementSet = new XmlTranslationSet {
				Items = new[] { new XmlNamespaceTranslation("pipelineSourceUrn", "pipelineTargetUrn") }
			};

			contextReplacementSet.Union(pipelineReplacementSet).Should().Be(
				new XmlTranslationSet {
					Override = true,
					Items = new[] {
						new XmlNamespaceTranslation("contextSourceUrn", "contextTargetUrn")
					}
				});
		}
	}
}
