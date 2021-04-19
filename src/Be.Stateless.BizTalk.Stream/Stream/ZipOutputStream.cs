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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Be.Stateless.Extensions;
using Be.Stateless.IO;

namespace Be.Stateless.BizTalk.Stream
{
	/// <summary>
	/// Wraps a zip-compressing stream around a data stream.
	/// </summary>
	/// <remarks>
	/// <see cref="ZipOutputStream"/> relies on <see cref="ZipArchive"/> for the compression and will have exactly one <see
	/// cref="ZipArchiveEntry"/>.
	/// </remarks>
	/// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.io.compression.ziparchive">ZipArchive</seealso>
	/// <seealso href="http://my.safaribooksonline.com/book/operating-systems-and-server-administration/microsoft-biztalk/9780470046425/pipelines/121"></seealso>
	public class ZipOutputStream : System.IO.Stream
	{
		#region Nested Type: BufferStream

		/// <summary>
		/// This stream is used as underlying stream given to the .Net's <see cref="ZipArchive"/> class used to compress the data
		/// stream. The <see cref="Position"/> property has been implemented because it is used by the .Net API.
		/// </summary>
		private class BufferStream : System.IO.Stream
		{
			public BufferStream(ZipOutputStream zipOutputStream)
			{
				_zipOutputStream = zipOutputStream;
				_position = 0L;
			}

			#region Base Class Member Overrides

			public override bool CanRead => false;

			public override bool CanSeek => false;

			public override bool CanWrite => true;

			public override void Close() { }

			public override void Flush() { }

			public override long Length => throw new NotSupportedException();

			public override long Position
			{
				get => _position;
				set => throw new NotSupportedException();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotSupportedException();
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
				if (buffer == null) throw new ArgumentNullException(nameof(buffer));
				if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Cannot be negative.");
				if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be negative.");
				if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the byte array length.");

				_zipOutputStream.AppendCompressedBytes(buffer, offset, count);
				_position += count;
			}

			#endregion

			private readonly ZipOutputStream _zipOutputStream;
			private long _position;
		}

		#endregion

		public ZipOutputStream(System.IO.Stream streamToCompress, string zipEntryName, int bufferSize = 4 * 1024)
		{
			if (zipEntryName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(zipEntryName));
			if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize), "Buffer size must be strictly positive.");

			_streamToCompress = streamToCompress ?? throw new ArgumentNullException(nameof(streamToCompress));
			_zipEntryName = zipEntryName;
			_buffer = new byte[bufferSize];
		}

		#region Base Class Member Overrides

		public override bool CanRead => true;

		public override bool CanSeek => false;

		public override bool CanWrite => false;

		public override void Close()
		{
			if (_streamToCompress != null)
			{
				_streamToCompress.Close();
				_streamToCompress = null;
			}
			if (_compressedStream != null)
			{
				_compressedStream.Close();
				_compressedStream = null;
			}
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
			if (_streamToCompress == null) throw new ObjectDisposedException(nameof(ZipOutputStream));
			if (buffer == null) throw new ArgumentNullException(nameof(buffer));
			if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Cannot be negative.");
			if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be negative.");
			if (offset + count > buffer.Length) throw new ArgumentException("The sum of offset and count is greater than the byte array length.");

			_bufferController = new BufferController(buffer, offset, count);
			_backlogs = _bufferController.Append(_backlogs).ToList();
			while (_bufferController.Availability > 0 && !_eos)
			{
				var bytesRead = _streamToCompress.Read(_buffer, 0, _buffer.Length);
				if (bytesRead == 0 && !_eos)
				{
					CompressedStream.Close();
					// force writing the last bytes of the archive into the BufferStream and so the _bufferController
					_archive.Dispose();
					_eos = true;
				}
				else
				{
					CompressedStream.Write(_buffer, 0, bytesRead);
				}
			}
			return _bufferController.Count;
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

		[SuppressMessage("ReSharper", "InvertIf")]
		private System.IO.Stream CompressedStream
		{
			get
			{
				if (_compressedStream == null)
				{
					_archive = new ZipArchive(new BufferStream(this), ZipArchiveMode.Create, true);
					var zipEntry = _archive.CreateEntry(_zipEntryName);
					_compressedStream = zipEntry.Open();
				}
				return _compressedStream;
			}
		}

		private void AppendCompressedBytes(byte[] buffer, int offset, int count)
		{
			var backlog = _bufferController.Append(buffer, offset, count);
			if (backlog is { Length: > 0 }) _backlogs.Add(backlog);
		}

		private readonly byte[] _buffer;
		private readonly string _zipEntryName;

		[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The Dispose method is called by Read()")]
		private ZipArchive _archive;

		private IList<byte[]> _backlogs = new List<byte[]>();
		private BufferController _bufferController;
		private System.IO.Stream _compressedStream;
		private bool _eos;
		private System.IO.Stream _streamToCompress;
	}
}
