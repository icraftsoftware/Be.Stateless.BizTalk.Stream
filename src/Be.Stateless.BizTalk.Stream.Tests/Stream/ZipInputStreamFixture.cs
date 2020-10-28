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

using System.IO;
using System.Reflection;
using Be.Stateless.IO.Extensions;
using Be.Stateless.Resources;
using FluentAssertions;
using Xunit;

namespace Be.Stateless.BizTalk.Stream
{
	public class ZipInputStreamFixture
	{
		[Fact]
		public void BaseInputStreamIsReadToEnd()
		{
			using (var dataStream = ResourceManager.Load(Assembly.GetExecutingAssembly(), "Be.Stateless.BizTalk.Resources.Zip.MeteringRequest.zip"))
			using (var eventingStream = new EventingReadStream(dataStream))
			using (var decompressingStream = new ZipInputStream(eventingStream))
			{
				var eosReached = false;
				eventingStream.AfterLastReadEvent += (sender, args) => eosReached = true;
				decompressingStream.Drain();
				eosReached.Should().BeTrue();
			}
		}

		[Fact]
		public void UnzipFileWithMultipleEntries()
		{
			using (var dataStream = ResourceManager.Load(Assembly.GetExecutingAssembly(), "Be.Stateless.BizTalk.Resources.Zip.MeterReadsPeriodicDelivery.zip"))
			using (var decompressedStream = new MemoryStream())
			using (var decompressingStream = new ZipInputStream(dataStream))
			{
				decompressingStream.CopyTo(decompressedStream);
				decompressedStream.ToArray().Length.Should().Be(34137);
			}
		}

		[Fact]
		public void UnzipFileWithSingleEntry()
		{
			using (var dataStream = ResourceManager.Load(Assembly.GetExecutingAssembly(), "Be.Stateless.BizTalk.Resources.Zip.MeteringRequest.zip"))
			using (var decompressedStream = new MemoryStream())
			using (var decompressingStream = new ZipInputStream(dataStream))
			{
				decompressingStream.CopyTo(decompressedStream);
				decompressedStream.ToArray().Length.Should().Be(1366759);
			}
		}
	}
}
