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

using System.Diagnostics.CodeAnalysis;
using Microsoft.BizTalk.Streaming;

namespace Be.Stateless.BizTalk.Stream
{
	/// <summary>
	/// This stream wraps an underlying stream into a read-only seekable stream. It does so by reusing BizTalk's <see
	/// cref="Microsoft.BizTalk.Streaming.ReadOnlySeekableStream">Microsoft.BizTalk.Streaming.ReadOnlySeekableStream</see> with
	/// a <see cref="VirtualStream"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The sole purpose of this Stream is to implement an often used pattern which is to use the <see
	/// cref="Microsoft.BizTalk.Streaming.ReadOnlySeekableStream">Microsoft.BizTalk.Streaming.ReadOnlySeekableStream</see> in
	/// conjunction with a <see cref="VirtualStream"/>. The pattern being that <see cref="VirtualStream"/> will hold the stream
	/// data in memory until a threshold is reached after which it will switch to disk. The benefit being that it avoids using
	/// unnecessary IO resources for small messages. Note that there is a small performance hit once the stream changes
	/// persistence mode.
	/// </para>
	/// <para>
	/// The developer can alternatively choose to directly use the <see
	/// cref="Microsoft.BizTalk.Streaming.ReadOnlySeekableStream">Microsoft.BizTalk.Streaming.ReadOnlySeekableStream</see> which
	/// exposes constructor forcing the stream to always store the data into a File on disk.
	/// </para>
	/// <para>
	/// Note that the temporary file is created in the AppData directory of the user running the process i.e.
	/// C:\Users\{User}\AppData\Local\Temp. For example, if the process is a BizTalk Host Instance, the temporary file will be
	/// created in the AppData folder of the Service Account under which it is running. The temporary file name is prefixed with
	/// the string "VST".
	/// </para>
	/// </remarks>
	public class ReadOnlySeekableStream : Microsoft.BizTalk.Streaming.ReadOnlySeekableStream
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="source">
		/// The underlying stream.
		/// </param>
		/// <param name="bufferSize">
		/// Buffer size in bytes used by <see cref="Microsoft.BizTalk.Streaming.ReadOnlySeekableStream"/> and <see
		/// cref="VirtualStream"/>.
		/// </param>
		/// <param name="thresholdSize">
		/// Size in bytes after which <see cref="VirtualStream"/> will switch from a MemoryStream to a FileStream.
		/// </param>
		[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The base class Dispose() method calls Close() on the VirtualStream")]
		public ReadOnlySeekableStream(System.IO.Stream source, int bufferSize = 8 * 1024, int thresholdSize = 1024 * 1024) : base(
			source,
			new VirtualStream(bufferSize, thresholdSize),
			bufferSize) { }
	}
}
