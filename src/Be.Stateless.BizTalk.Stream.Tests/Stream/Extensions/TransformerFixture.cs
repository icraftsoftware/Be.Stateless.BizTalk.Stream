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
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using Be.Stateless.BizTalk.ContextProperties;
using Be.Stateless.BizTalk.Resources.Transform;
using Be.Stateless.BizTalk.Runtime.Caching;
using Be.Stateless.BizTalk.Schemas;
using Be.Stateless.IO;
using Be.Stateless.IO.Extensions;
using FluentAssertions;
using Microsoft.BizTalk.Message.Interop;
using Moq;
using Xunit;

namespace Be.Stateless.BizTalk.Stream.Extensions
{
	public class TransformerFixture
	{
		[Fact]
		public void ApplySatisfiesExtensionRequirementsWithMessageContext()
		{
			var contextMock = new Mock<IBaseMessageContext>();
			var transform = typeof(CompoundContextMapTransform);
			var arguments = XsltCache.Instance[transform].Arguments;

			arguments.GetExtensionObject(ExtensionObjectNamespaces.MessageContext).Should().BeNull();

			var stream = new StringStream("<?xml version='1.0' encoding='utf-16'?><root></root>");
			var sut = new Transformer(new System.IO.Stream[] { stream });
			sut.ExtendWith(contextMock.Object).Apply(transform);

			arguments.GetExtensionObject(ExtensionObjectNamespaces.MessageContext).Should().BeNull();

			contextMock.Verify(c => c.Read(BizTalkFactoryProperties.EnvironmentTag.Name, BizTalkFactoryProperties.EnvironmentTag.Namespace), Times.Once());
			contextMock.Verify(c => c.Read(BtsProperties.Operation.Name, BtsProperties.Operation.Namespace), Times.Once());
		}

		[Fact]
		public void ApplySatisfiesExtensionRequirementsWithoutMessageContext()
		{
			var transform = typeof(IdentityTransform);
			var arguments = XsltCache.Instance[transform].Arguments;
			arguments.GetExtensionObject(ExtensionObjectNamespaces.MessageContext).Should().BeNull();

			var stream = new StringStream("<?xml version='1.0' encoding='utf-16'?><root></root>");
			var sut = new Transformer(new System.IO.Stream[] { stream });
			sut.Apply(transform);

			arguments.GetExtensionObject(ExtensionObjectNamespaces.MessageContext).Should().BeNull();
		}

		[Fact]
		public void ApplyThrowsIfExtensionRequirementsWithMessageContextCannotBeSatisfied()
		{
			var transform = typeof(CompoundContextMapTransform);

			var stream = new StringStream("<?xml version='1.0' encoding='utf-16'?><root></root>");
			var sut = new Transformer(new System.IO.Stream[] { stream });

			Action act = () => sut.Apply(transform);
			act.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null.\r\nParameter name: context");
		}

		[Fact]
		public void ApplyTransformsWithImportedAndIncludedStylesheets()
		{
			const string xml = @"<root><one>a</one><two>b</two><six>sense</six></root>";
			using (var stream = new StringStream(xml).Transform().ExtendWith(new Mock<IBaseMessageContext>().Object).Apply(typeof(CompositeMapTransform)))
			using (var reader = XmlReader.Create(stream))
			{
				reader.MoveToContent();
				reader.ReadOuterXml().Should().Be("<root><first>a</first><second>b</second><sixth>sense</sixth></root>");
			}
		}

		[Fact]
		public void BuildArgumentListYieldsFreshCopyWhenRequired()
		{
			var arguments = new XsltArgumentList();
			var contextMock = new Mock<IBaseMessageContext>();

			var sut = new Transformer(new System.IO.Stream[] { new MemoryStream() });
			sut.ExtendWith(contextMock.Object);

			var descriptor = XsltCache.Instance[typeof(IdentityTransform)];

			// no specific arguments, no message context requirement, same XsltArgumentList instance can be shared
			sut.BuildArgumentList(descriptor, null).Should().BeSameAs(descriptor.Arguments);

			// specific arguments, no message context requirement, new XsltArgumentList instance is required
			sut.BuildArgumentList(descriptor, arguments).Should().NotBeSameAs(descriptor.Arguments).And.NotBeSameAs(arguments);

			descriptor = XsltCache.Instance[typeof(CompoundContextMapTransform)];

			// no specific arguments but message context requirement, new XsltArgumentList instance is required
			sut.BuildArgumentList(descriptor, null).Should().NotBeSameAs(descriptor.Arguments).And.NotBeSameAs(arguments);

			// specific arguments and message context requirement, new XsltArgumentList instance is required
			sut.BuildArgumentList(descriptor, null).Should().NotBeSameAs(descriptor.Arguments).And.NotBeSameAs(arguments);
		}

		[Fact]
		public void TransformMultipleStreamsDiscardsXmlDeclarations()
		{
			using (var stream1 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><one/></root>"))
			using (var stream2 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><two/></root>"))
			using (var stream6 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><six/></root>"))
			using (var stream = new System.IO.Stream[] { stream1, stream2, stream6 }.Transform().Apply(typeof(IdentityTransform)))
			using (var reader = XmlReader.Create(stream))
			{
				reader.MoveToContent();
				reader.ReadOuterXml().Should().Be(
					"<agg:Root xmlns:agg=\"http://schemas.microsoft.com/BizTalk/2003/aggschema\">"
					+ "<agg:InputMessagePart_0><root><one /></root></agg:InputMessagePart_0>"
					+ "<agg:InputMessagePart_1><root><two /></root></agg:InputMessagePart_1>"
					+ "<agg:InputMessagePart_2><root><six /></root></agg:InputMessagePart_2>"
					+ "</agg:Root>");
			}
		}

		[Fact]
		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		public void TransformOneAggregateStreamDoesNotDiscardXmlDeclarationsAndThrows()
		{
			using (var stream1 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><one/></root>"))
			using (var stream2 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><two/></root>"))
			using (var stream6 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><six/></root>"))
			using (var compositeStream = new CompositeXmlStream(new System.IO.Stream[] { stream1, stream2, stream6 }))
			using (var memoryStream = new MemoryStream())
			{
				compositeStream.CopyTo(memoryStream);
				Action act = () => memoryStream.Rewind().Transform().Apply(typeof(IdentityTransform));
				act.Should().Throw<XmlException>();
			}
		}

		[Fact]
		public void TransformOneCompositeStreamDiscardsXmlDeclarations()
		{
			using (var stream1 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><one/></root>"))
			using (var stream2 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><two/></root>"))
			using (var stream6 = new StringStream("<?xml version='1.0' encoding='utf-16'?><root><six/></root>"))
			using (var compositeStream = new CompositeXmlStream(new System.IO.Stream[] { stream1, stream2, stream6 }))
			using (var stream = compositeStream.Transform().Apply(typeof(IdentityTransform)))
			using (var reader = XmlReader.Create(stream))
			{
				reader.MoveToContent();
				reader.ReadOuterXml().Should().Be(
					"<agg:Root xmlns:agg=\"http://schemas.microsoft.com/BizTalk/2003/aggschema\">"
					+ "<agg:InputMessagePart_0><root><one /></root></agg:InputMessagePart_0>"
					+ "<agg:InputMessagePart_1><root><two /></root></agg:InputMessagePart_1>"
					+ "<agg:InputMessagePart_2><root><six /></root></agg:InputMessagePart_2>"
					+ "</agg:Root>");
			}
		}
	}
}
