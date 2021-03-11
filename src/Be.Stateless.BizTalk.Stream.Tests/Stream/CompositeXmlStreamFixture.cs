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
using System.Text;
using System.Xml;
using FluentAssertions;
using Moq;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Be.Stateless.BizTalk.Stream
{
	public class CompositeXmlStreamFixture
	{
		[Fact]
		public void AggregatesIndividualStreams()
		{
			const string expected = "<agg:Root xmlns:agg=\"http://schemas.microsoft.com/BizTalk/2003/aggschema\">" +
				"<agg:InputMessagePart_0><part-one xmlns=\"part-one\"><child-one>one</child-one></part-one></agg:InputMessagePart_0>" +
				"<agg:InputMessagePart_1><part-two xmlns=\"part-two\"><child-two>two</child-two></part-two></agg:InputMessagePart_1>" +
				"<agg:InputMessagePart_2><part-six xmlns=\"part-six\"><child-six>six</child-six></part-six></agg:InputMessagePart_2>" +
				"</agg:Root>";
			using (var composite = new CompositeXmlStream(new System.IO.Stream[] { new MemoryStream(_part1), new MemoryStream(_part2), new MemoryStream(_part3) }))
			using (var xmlReader = XmlReader.Create(composite))
			{
				xmlReader.Read();
				var actual = xmlReader.ReadOuterXml();
				actual.Should().Be(expected);
			}
		}

		[Fact]
		public void AlwaysExhaustAggregatedStreams()
		{
			const string expected = "<agg:Root xmlns:agg=\"http://schemas.microsoft.com/BizTalk/2003/aggschema\">" +
				"<agg:InputMessagePart_0><part-one xmlns=\"part-one\"><child-one>one</child-one></part-one></agg:InputMessagePart_0>" +
				"<agg:InputMessagePart_1><part-two xmlns=\"part-two\"><child-two>two</child-two></part-two></agg:InputMessagePart_1>" +
				"</agg:Root>";

			using (var composite = new CompositeXmlStream(new System.IO.Stream[] { new MemoryStream(_part1), new MemoryStream(_part2) }))
			{
				var buffer = new byte[Encoding.UTF8.GetByteCount(expected)];
				var offset = 0;
				int bytesRead;
				while ((bytesRead = composite.Read(buffer, offset, 8)) > 0)
				{
					offset += bytesRead;
				}
				var actual = Encoding.UTF8.GetString(buffer);
				actual.Should().Be(expected);
			}
		}

		[Fact]
		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		public void AssumesCompoundStreamsAreUtf8Encoded()
		{
			var part1 = Encoding.UTF8.GetBytes("<part-one xmlns=\"part-one\"><child-one>one</child-one></part-one>");
			var part2 = Encoding.Unicode.GetBytes("<part-two xmlns=\"part-two\"><child-two>two</child-two></part-two>");
			using (var composite = new CompositeXmlStream(new System.IO.Stream[] { new MemoryStream(part1), new MemoryStream(part2) }))
			using (var xmlReader = XmlReader.Create(composite))
			{
				xmlReader.Read();
				Invoking(() => xmlReader.ReadOuterXml()).Should().Throw<XmlException>();
			}
		}

		[Fact]
		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		public void AssumesCompoundStreamsHaveNoXmlDeclarations()
		{
			var part1 = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\" ?><part-one xmlns=\"part-one\"><child-one>one</child-one></part-one>");
			var part2 = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\" ?><part-two xmlns=\"part-two\"><child-two>two</child-two></part-two>");
			using (var composite = new CompositeXmlStream(new System.IO.Stream[] { new MemoryStream(part1), new MemoryStream(part2) }))
			using (var xmlReader = XmlReader.Create(composite))
			{
				xmlReader.Read();
				Invoking(() => xmlReader.ReadOuterXml())
					.Should().Throw<XmlException>().WithMessage("Unexpected XML declaration. The XML declaration must be the first node in the document*");
			}
		}

		[Fact]
		public void InputStreamsAreClosed()
		{
			var mock1 = new Mock<MemoryStream>(MockBehavior.Default, _part1) { CallBase = true };
			var mock2 = new Mock<MemoryStream>(MockBehavior.Default, _part2) { CallBase = true };
			var mock3 = new Mock<MemoryStream>(MockBehavior.Default, _part3) { CallBase = true };

			using (var composite = new CompositeXmlStream(new System.IO.Stream[] { mock1.Object, mock2.Object, mock3.Object }))
			{
				var buffer = new byte[10];
				composite.Read(buffer, 0, buffer.Length);
			}

			mock1.Verify(s => s.Close());
			mock2.Verify(s => s.Close());
			mock3.Verify(s => s.Close());
		}

		private readonly byte[] _part1 = Encoding.UTF8.GetBytes("<part-one xmlns=\"part-one\"><child-one>one</child-one></part-one>");
		private readonly byte[] _part2 = Encoding.UTF8.GetBytes("<part-two xmlns=\"part-two\"><child-two>two</child-two></part-two>");
		private readonly byte[] _part3 = Encoding.UTF8.GetBytes("<part-six xmlns=\"part-six\"><child-six>six</child-six></part-six>");
	}
}
