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
using System.IO.Compression;
using Be.Stateless.IO.Extensions;
using Microsoft.BizTalk.Streaming;

namespace Be.Stateless.BizTalk.Stream
{
	/// <summary>
	/// Wraps a zip-decompressing stream around a data stream and ensures that the data stream is exhausted once the
	/// decompression is complete. It supports the PK Zip archive format with an entry compressed with the DEFLATE algorithm.
	/// </summary>
	/// <remarks>
	/// <see cref="ZipInputStream"/> relies on <see cref="ZipArchive"/> for the decompression of the zip-stream. The stream
	/// containing the zipped data must have exactly one <see cref="ZipArchiveEntry"/>. If more than one entry exist, only the
	/// first one is decompressed and the remaining entries are disregarded. If the underlying stream given to the constructor
	/// is not seekable, <see cref="ZipInputStream"/> will leverage BizTalk's built-in <see cref="ReadOnlySeekableStream"/> and
	/// <see cref="VirtualStream"/> to avoid loading the whole stream in memory. This is necessary as <see cref="ZipArchive"/>
	/// loads the entire archive stream in memory if the underlying stream does not support seeking.
	/// </remarks>
	/// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchive">ZipArchive</seealso>
	/// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchivemode#remarks">ZipArchiveMode</seealso>
	public class ZipInputStream : System.IO.Stream
	{
		public ZipInputStream(System.IO.Stream streamToDecompress)
		{
			if (streamToDecompress == null) throw new ArgumentNullException(nameof(streamToDecompress));
			if (!streamToDecompress.CanSeek) streamToDecompress = new ReadOnlySeekableStream(streamToDecompress);
			_baseInputStream = streamToDecompress;
		}

		#region Base Class Member Overrides

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override void Close()
		{
			_decompressionStream?.Close();
			_archive?.Dispose();
			_baseInputStream?.Close();
			base.Close();
		}

		public override void Flush()
		{
			throw new NotSupportedException();
		}

		public override long Length => throw new NotSupportedException();

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			_archive ??= new ZipArchive(_baseInputStream, ZipArchiveMode.Read, true);
			_decompressionStream ??= _archive.Entries[0].Open();
			var byteCount = _decompressionStream.Read(buffer, offset, count);
			if (byteCount == 0) _baseInputStream.Drain();
			return byteCount;
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
			throw new NotImplementedException();
		}

		#endregion

		private readonly System.IO.Stream _baseInputStream;
		private ZipArchive _archive;
		private System.IO.Stream _decompressionStream;
	}
}
