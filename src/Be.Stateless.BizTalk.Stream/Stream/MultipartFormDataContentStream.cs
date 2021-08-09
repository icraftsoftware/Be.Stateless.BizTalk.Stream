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
using System.Net.Http;

namespace Be.Stateless.BizTalk.Stream
{
	[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
	[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API.")]
	public class MultipartFormDataContentStream : System.IO.Stream
	{
		public MultipartFormDataContentStream(System.IO.Stream stream)
		{
			_multipartContent = new() { new StreamContent(stream) };
		}

		public MultipartFormDataContentStream(System.IO.Stream stream, string name)
		{
			_multipartContent = new() { { new StreamContent(stream), name } };
		}

		#region Base Class Member Overrides

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_multipartContent?.Dispose();
				_multipartContent = null;
				_dataContentStream?.Dispose();
				_dataContentStream = null;
			}
			base.Dispose(disposing);
		}

		public override void Flush()
		{
			DataContentStream.Flush();
		}

		public override long Length => throw new NotSupportedException();

		public override long Position { get; set; }

		public override int Read(byte[] buffer, int offset, int count)
		{
			return DataContentStream.Read(buffer, offset, count);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		#endregion

		public string ContentType => _multipartContent.Headers.ContentType.ToString();

		private System.IO.Stream DataContentStream => _dataContentStream ??= _multipartContent.ReadAsStreamAsync().Result;

		private System.IO.Stream _dataContentStream;

		private MultipartFormDataContent _multipartContent;
	}
}
