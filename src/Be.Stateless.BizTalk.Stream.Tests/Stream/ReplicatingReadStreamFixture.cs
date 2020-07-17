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
using System.Text;
using Be.Stateless.IO;
using Be.Stateless.IO.Extensions;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Be.Stateless.BizTalk.Stream
{
	public class ReplicatingReadStreamFixture
	{
		[Fact]
		public void LengthCanBeReadAfterStreamExhaustion()
		{
			using (var stream = new ReplicatingReadStream(new MemoryStream(_content), new MemoryStream()))
			{
				stream.Drain();
				stream.Length.Should().Be(_content.Length);
			}
		}

		[Fact]
		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		public void LengthIsUnknownBeforeStreamExhaustion()
		{
			using (var stream = new ReplicatingReadStream(new MemoryStream(_content), new MemoryStream()))
			{
				Func<long> act = () => stream.Length;
				act.Should().Throw<NotSupportedException>();
			}
		}

		[Fact]
		public void ReplicatingReadStreamRequiresTargetStreamToReplicate()
		{
			Func<System.IO.Stream> act = () => new ReplicatingReadStream(new MemoryStream(_content), null);
			act.Should().Throw<ArgumentNullException>().WithMessage("Value cannot be null.\r\nParameter name: target");
		}

		[Fact]
		public void SourceAndTargetStreamsAreClosed()
		{
			var sourceStream = new Mock<System.IO.Stream> { CallBase = true };
			sourceStream.Setup(s => s.Close()).Verifiable("sourceStream");
			var targetStream = new Mock<System.IO.Stream> { CallBase = true };
			targetStream.Setup(s => s.Close()).Verifiable("targetStream");
			using (new ReplicatingReadStream(sourceStream.Object, targetStream.Object)) { }
			sourceStream.Verify();
			targetStream.Verify();
		}

		[Fact]
		public void SourceAndTargetStreamsAreDisposed()
		{
			var sourceStream = new Mock<System.IO.Stream> { CallBase = true };
			// notice that because sourceStream.As<IDisposable>().Verify(s => s.Dispose()) is, oddly enough, never
			// satisfied, fallback on testing that the protected override Dispose(bool disposing) is being called,
			// which is just an indirect way to test that dispose is being called.
			sourceStream.Protected()
				.Setup("Dispose", true, true)
				.Verifiable("sourceStream.Dispose()");
			var targetStream = new Mock<System.IO.Stream> { CallBase = true };
			targetStream.Protected()
				.Setup("Dispose", true, true)
				.Verifiable("targetStream.Dispose()");

			using (new ReplicatingReadStream(sourceStream.Object, targetStream.Object)) { }

			sourceStream.Verify();
			sourceStream.Protected().Verify("Dispose", Times.Once(), true, true);
			targetStream.Verify();
			targetStream.Protected().Verify("Dispose", Times.Once(), true, true);

			// notice also that Dispose just call Close... why are we bother at all... :/
			sourceStream.Verify(s => s.Close());
			targetStream.Verify(s => s.Close());
		}

		[Fact]
		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		public void SourceStreamCannotBeSoughtBeforeExhaustion()
		{
			using (var stream = new ReplicatingReadStream(new MemoryStream(_content), new MemoryStream()))
			{
				stream.CanSeek.Should().BeFalse();
				// don't drain the whole stream
				stream.Read(new byte[1024], 0, 1024);
				Action act = () => stream.Position = 0;
				act.Should().Throw<InvalidOperationException>()
					.WithMessage($"{nameof(ReplicatingReadStream)} is not seekable while the inner stream has not been thoroughly read and replicated.");
				act = () => stream.Seek(0, SeekOrigin.Begin);
				act.Should().Throw<InvalidOperationException>()
					.WithMessage($"{nameof(ReplicatingReadStream)} cannot be sought while the inner stream has not been thoroughly read and replicated.");
			}
		}

		[Fact]
		public void SourceStreamIsReplicatedToTargetStreamWhileBeingRead()
		{
			using (var targetStream = new MemoryStream())
			using (var stream = new ReplicatingReadStream(new MemoryStream(_content), targetStream))
			{
				stream.Drain();
				_content.Should().BeEquivalentTo(targetStream.ToArray());
			}
		}

		[Fact]
		[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
		public void SourceStreamIsSeekableAfterExhaustion()
		{
			using (var stream = new ReplicatingReadStream(new MemoryStream(_content), new MemoryStream()))
			{
				stream.CanSeek.Should().BeFalse();
				stream.Drain();
				stream.CanSeek.Should().BeTrue();
				Action act = () => stream.Position = 0;
				act.Should().NotThrow();
			}
		}

		[Fact]
		public void TargetStreamIsCommittedOnlyOnceEvenIfStreamIsRewound()
		{
			var targetStream = new Mock<System.IO.Stream> { CallBase = true };
			var streamTransacted = targetStream.As<ITransactionalStream>();
			streamTransacted.Setup(s => s.Commit());
			using (var stream = new ReplicatingReadStream(new MemoryStream(_content), targetStream.Object))
			{
				stream.Drain();
				stream.Position = 0;
				stream.Drain();
			}
			streamTransacted.Verify(s => s.Commit(), Times.Once());
		}

		[Fact]
		public void TargetStreamIsCommittedUponSourceStreamExhaustion()
		{
			var targetStream = new Mock<System.IO.Stream> { CallBase = true };
			targetStream.As<ITransactionalStream>().Setup(st => st.Commit()).Verifiable("targetStream");
			using (var stream = new ReplicatingReadStream(new MemoryStream(_content), targetStream.Object))
			{
				stream.Drain();
			}
			targetStream.VerifyAll();
		}

		[Fact]
		public void TargetStreamIsNotCommittedIfSourceStreamNotExhausted()
		{
			var targetStream = new Mock<System.IO.Stream> { CallBase = true };
			targetStream.As<ITransactionalStream>();
			using (var stream = new ReplicatingReadStream(new MemoryStream(_content), targetStream.Object))
			{
				// don't drain the whole stream
				stream.Read(new byte[1024], 0, 1024);
			}
			// Rollback() is never called explicitly when targetStream is disposed, but neither is Commit()
			targetStream.As<ITransactionalStream>().Verify(s => s.Commit(), Times.Never());
		}

		private readonly byte[] _content = Encoding.Unicode.GetBytes(new string('A', 3999));
	}
}
