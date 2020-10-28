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
using System.IO;
using System.Linq;
using System.Text;
using Be.Stateless.BizTalk.Unit.Stream.Extensions;
using Be.Stateless.Dummies.IO;
using Be.Stateless.IO;
using FluentAssertions;
using Xunit;

namespace Be.Stateless.BizTalk.Stream
{
	public class ZipOutputStreamFixture
	{
		[Fact]
		public void ValidateCompressedData()
		{
			using (var clearStream = TextStreamDummy.Create(1024 * 64))
			using (var compressedStream = new ZipOutputStream(clearStream, "entry-name"))
			{
				compressedStream.IsZipValid().Should().BeTrue();
			}
		}

		[Fact]
		public void ZipUnzipLargePayload()
		{
			using (var memoryStream = new MemoryStream())
			using (var clearStream = new ReplicatingReadStream(TextStreamDummy.Create(1024 * 64), memoryStream))
			using (var compressedStream = new ZipOutputStream(clearStream, "entry-name"))
			using (var decompressedStream = new ZipInputStream(compressedStream))
			{
				using (var reader = new StreamReader(decompressedStream))
				{
					var output = reader.ReadToEnd();
					output.Should().Be(Encoding.UTF8.GetString(memoryStream.ToArray()));
				}
			}
		}

		[Fact]
		public void ZipUnzipLargePayloadUsingSmallBuffer()
		{
			using (var memoryStream = new MemoryStream())
			using (var clearStream = new ReplicatingReadStream(TextStreamDummy.Create(1024 * 64), memoryStream))
			using (var compressedStream = new ZipOutputStream(clearStream, "entry-name", 256))
			using (var decompressedStream = new ZipInputStream(compressedStream))
			{
				using (var reader = new StreamReader(decompressedStream))
				{
					var output = reader.ReadToEnd();
					output.Should().Be(Encoding.UTF8.GetString(memoryStream.ToArray()));
				}
			}
		}

		[Fact]
		public void ZipUnzipLargePayloadUsingTinyBuffer()
		{
			// computing content beforehand is much faster than using a ReplicatingReadStream
			var content = Enumerable.Range(0, 1024)
				.Select(i => Guid.NewGuid().ToString())
				.Aggregate(string.Empty, (k, v) => k + v);
			using (var clearStream = new StringStream(content))
			using (var compressedStream = new ZipOutputStream(clearStream, "entry-name", 16))
			using (var decompressedStream = new ZipInputStream(compressedStream))
			{
				using (var reader = new StreamReader(decompressedStream))
				{
					var output = reader.ReadToEnd();
					output.Should().Be(content);
				}
			}
		}

		[Fact]
		public void ZipUnzipSmallStringPayload()
		{
			const string content = "text";
			using (var clearStream = new StringStream(content))
			using (var compressedStream = new ZipOutputStream(clearStream, "entry-name"))
			using (var decompressedStream = new ZipInputStream(compressedStream))
			{
				using (var reader = new StreamReader(decompressedStream))
				{
					var output = reader.ReadToEnd();
					output.Should().Be(content);
				}
			}
		}
	}
}
