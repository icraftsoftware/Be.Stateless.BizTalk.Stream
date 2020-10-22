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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Be.Stateless.BizTalk.Schemas;
using Be.Stateless.BizTalk.Xml;
using Be.Stateless.IO;
using Be.Stateless.Linq.Extensions;
using log4net;

namespace Be.Stateless.BizTalk.Stream
{
	/// <summary>
	/// Aggregates, at the stream level, several <see cref="Stream"/>s whose contents is XML.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Because the input streams are aggregated at the stream level, the contents of the input streams must all be of UTF-8
	/// encoded and cannot have an <see cref="XmlDeclaration"/>. Note that it is also possible to use a
	/// <c>Be.Stateless.BizTalk.Xml.CompositeXmlReader</c> instead of this <see cref="CompositeXmlStream"/> should you not be
	/// able to lift either of these constraints.
	/// </para>
	/// <para>
	/// The contents of the aggregated <see cref="Stream"/>s is wrapped in an XML structured as follows:
	/// <code>
	/// <![CDATA[<agg:Root xmlns:agg="http://schemas.microsoft.com/BizTalk/2003/aggschema">
	///   <agg:InputMessagePart_0>
	///     ... content of 1st message part ...
	///   </agg:InputMessagePart_0>
	///   <agg:InputMessagePart_1>
	///     ... content of 2nd message part ...
	///   </agg:InputMessagePart_1>
	///   ...
	///   <agg:InputMessagePart_n>
	///     ... content of nth message part ...
	///   </agg:InputMessagePart_n>
	/// </agg:Root>]]>
	/// </code>
	/// </para>
	/// </remarks>
	public class CompositeXmlStream : System.IO.Stream
	{
		/// <summary>
		/// Construct an <see cref="CompositeXmlStream"/> instance wrapper around the <paramref name="streams"/>.
		/// </summary>
		/// <param name="streams">
		/// The <see cref="Stream"/>s to wrap.
		/// </param>
		public CompositeXmlStream(System.IO.Stream[] streams)
		{
			_streams = streams ?? throw new ArgumentNullException(nameof(streams));
			_state = CompositeXmlStreamState.RootAggregateOpeningTag;
		}

		#region Base Class Member Overrides

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <returns>
		/// true if the stream supports reading; otherwise, false.
		/// </returns>
		public override bool CanRead => true;

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <returns>
		/// true if the stream supports seeking; otherwise, false.
		/// </returns>
		public override bool CanSeek
		{
			get { return _streams.All(s => s.CanSeek); }
		}

		/// <summary>
		/// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the stream supports writing; otherwise, <c>false</c>.
		/// </returns>
		public override bool CanWrite => false;

		protected override void Dispose(bool disposing)
		{
			if (_logger.IsDebugEnabled) _logger.DebugFormat("{0} is {1}.", nameof(CompositeXmlStream), disposing ? "disposing" : "finalizing");
			if (disposing && _streams != null)
			{
				_streams.ForEach(s => s.Dispose());
				_streams = null;
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to
		/// the underlying device.
		/// </summary>
		public override void Flush()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// When overridden in a derived class, gets the length in bytes of the stream.
		/// </summary>
		/// <returns>
		/// A long value representing the length of the stream in bytes.
		/// </returns>
		/// <exception cref="NotSupportedException">
		/// A class derived from Stream does not support seeking.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		/// Methods were called after the stream was closed.
		/// </exception>
		public override long Length
		{
			get
			{
				try
				{
					ThrowIfDisposed();
					if (!CanSeek) throw new NotSupportedException();

					long length = ROOT_START_TAG.Length + ROOT_END_TAG.Length;
					if (_streams.Length <= 0) return length;

					length += _streams
						.Select(
							(t, i) => t.Length
								+ string.Format(CultureInfo.InvariantCulture, INPUT_MESSAGE_PART_START_TAG, i).Length
								+ string.Format(CultureInfo.InvariantCulture, INPUT_MESSAGE_PART_END_TAG, i).Length)
						.Sum();
					return length;
				}
				catch (Exception exception)
				{
					if (_logger.IsWarnEnabled) _logger.Warn($"{GetType()} stream has encountered an error.", exception);
					throw;
				}
			}
		}

		/// <summary>
		/// When overridden in a derived class, gets or sets the position within the current stream.
		/// </summary>
		/// <returns>
		/// The current position within the stream.
		/// </returns>
		/// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
		public override long Position
		{
			get
			{
				try
				{
					ThrowIfDisposed();
					if (_state != CompositeXmlStreamState.RootAggregateOpeningTag) throw new NotSupportedException();
					return 0L;
				}
				catch (Exception exception)
				{
					if (_logger.IsWarnEnabled) _logger.Warn($"{GetType()} stream has encountered an error.", exception);
					throw;
				}
			}
			set
			{
				try
				{
					ThrowIfDisposed();
					if (value != 0L) throw new NotSupportedException();
					ResetStreams();
				}
				catch (Exception exception)
				{
					if (_logger.IsWarnEnabled) _logger.Warn($"{GetType()} stream has encountered an error.", exception);
					throw;
				}
			}
		}

		/// <summary>
		/// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position
		/// within the stream by the number of bytes read.
		/// </summary>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many
		/// bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <param name="buffer">
		/// An array of bytes. When this method returns, the buffer contains the specified byte array with the values between
		/// <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read
		/// from the current source.
		/// </param>
		/// <param name="offset">
		/// The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current
		/// stream.
		/// </param>
		/// <param name="count">
		/// The maximum number of bytes to be read from the current stream.
		/// </param>
		public override int Read(byte[] buffer, int offset, int count)
		{
			try
			{
				ThrowIfDisposed();
				if (ReadCompleted && _backlog == null) return 0;
				var bufferController = new BufferController(buffer, offset, count);
				// try to exhaust backlog if any while keeping any bytes that overflows
				_backlog = bufferController.Append(_backlog);
				while (bufferController.Availability > 0 && !ReadCompleted)
				{
					// try to exhaust wrapped stream while keeping any overflow in backlog
					_backlog = ReadInternal(bufferController);
				}
				return bufferController.Count;
			}
			catch (Exception exception)
			{
				if (_logger.IsWarnEnabled) _logger.Warn($"{GetType()} stream has encountered an error.", exception);
				throw;
			}
		}

		/// <summary>
		/// When overridden in a derived class, sets the position within the current stream.
		/// </summary>
		/// <returns>
		/// The new position within the current stream.
		/// </returns>
		/// <param name="offset">
		/// A byte offset relative to the <paramref name="origin"/> parameter.
		/// </param>
		/// <param name="origin">
		/// A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.
		/// </param>
		public override long Seek(long offset, SeekOrigin origin)
		{
			try
			{
				ThrowIfDisposed();
				if (offset != 0 || origin != SeekOrigin.Begin) throw new NotSupportedException();
				_currentStreamIndex = 0;
				ReadCompleted = false;
				_state = CompositeXmlStreamState.RootAggregateOpeningTag;
				ResetStreams();
				return 0;
			}
			catch (Exception exception)
			{
				if (_logger.IsWarnEnabled) _logger.Warn($"{GetType()} stream has encountered an error.", exception);
				throw;
			}
		}

		/// <summary>
		/// When overridden in a derived class, sets the length of the current stream.
		/// </summary>
		/// <param name="value">
		/// The desired length of the current stream in bytes.
		/// </param>
		/// <exception cref="NotSupportedException">
		/// The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console
		/// output.
		/// </exception>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current
		/// position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">
		/// An array of bytes. This method copies count bytes from <paramref name="buffer"/> to the current stream.
		/// </param>
		/// <param name="offset">
		/// The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.
		/// </param>
		/// <param name="count">
		/// The number of bytes to be written to the current stream.
		/// </param>
		/// <exception cref="NotSupportedException">
		/// The stream does not support writing.
		/// </exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		#endregion

		[SuppressMessage("ReSharper", "ReturnTypeCanBeEnumerable.Global")]
		internal System.IO.Stream[] Streams
		{
			get
			{
				if (_state != CompositeXmlStreamState.RootAggregateOpeningTag) throw new InvalidOperationException(nameof(CompositeXmlStream) + "'s position is not at beginning.");
				return _streams;
			}
		}

		private bool ReadCompleted { get; set; }

		private byte[] ReadInternal(BufferController bufferController)
		{
			switch (_state)
			{
				case CompositeXmlStreamState.RootAggregateOpeningTag:
					_state = _streams.Length > 0 ? CompositeXmlStreamState.MessagePartWrapperOpeningTag : CompositeXmlStreamState.RootAggregateClosingTag;
					return bufferController.Append(Encoding.UTF8.GetBytes(ROOT_START_TAG));

				case CompositeXmlStreamState.MessagePartWrapperOpeningTag:
					_state = CompositeXmlStreamState.MessagePartContent;
					_currentStream = _streams[_currentStreamIndex];
					return bufferController.Append(Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, INPUT_MESSAGE_PART_START_TAG, _currentStreamIndex)));

				case CompositeXmlStreamState.MessagePartContent:
					var bytesRead = bufferController.Append(_currentStream.Read);
					if (bytesRead == 0) _state = CompositeXmlStreamState.MessagePartWrapperClosingTag;
					return null;

				case CompositeXmlStreamState.MessagePartWrapperClosingTag:
					var bytes = bufferController.Append(Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, INPUT_MESSAGE_PART_END_TAG, _currentStreamIndex)));
					_state = ++_currentStreamIndex < _streams.Length
						? _state = CompositeXmlStreamState.MessagePartWrapperOpeningTag
						: CompositeXmlStreamState.RootAggregateClosingTag;
					return bytes;

				case CompositeXmlStreamState.RootAggregateClosingTag:
					ReadCompleted = true;
					return bufferController.Append(Encoding.UTF8.GetBytes(ROOT_END_TAG));

				default:
					throw new InvalidOperationException($"Unexpected state value: {_state}.");
			}
		}

		private void ResetStreams()
		{
			foreach (var stream in _streams)
			{
				if (!stream.CanSeek) throw new NotSupportedException();
				stream.Seek(0, SeekOrigin.Begin);
			}
		}

		private void ThrowIfDisposed()
		{
			if (_streams == null) throw new ObjectDisposedException(GetType().Name);
		}

		private const string INPUT_MESSAGE_PART_END_TAG = "</agg:InputMessagePart_{0}>";
		private const string INPUT_MESSAGE_PART_START_TAG = "<agg:InputMessagePart_{0}>";
		private const string ROOT_END_TAG = "</agg:Root>";
		private const string ROOT_START_TAG = "<agg:Root xmlns:agg=\"" + SchemaNamespaces.BizTalkAggregate + "\">";
		private static readonly ILog _logger = LogManager.GetLogger(typeof(CompositeXmlStream));
		private byte[] _backlog;
		private System.IO.Stream _currentStream;
		private int _currentStreamIndex;
		private CompositeXmlStreamState _state;
		private System.IO.Stream[] _streams;
	}
}
