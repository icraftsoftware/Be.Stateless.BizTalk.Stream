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
using System.IO;
using System.IO.Compression;
using System.Xml;
using Be.Stateless.BizTalk.Stream;
using Microsoft.BizTalk.Streaming;
using XmlTranslatorStream = Be.Stateless.BizTalk.Stream.XmlTranslatorStream;

namespace Be.Stateless.BizTalk.Unit.Stream.Extensions
{
	[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Public API.")]
	[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API.")]
	public static class StreamExtensions
	{
		/// <summary>
		/// This extension method uses <see cref="ZipArchive"/> constructor to check if the stream contains the data of a valid
		/// Zip Archive.
		/// </summary>
		/// <param name="stream">
		/// The stream to check if it is the in zip archive format
		/// </param>
		/// <returns>
		/// true if the stream is in the zip archive format, false otherwise
		/// </returns>
		/// <remarks>
		/// This is an adaptation of an accepted way to check that a Zip File is valid by leveraging the <see
		/// cref="InvalidDataException"/>, see the following article: <a
		/// href="https://stackoverflow.com/questions/38970926/validating-zip-files-using-system-io-compression">Validating zip
		/// files using System.IO.Compression</a>.
		/// </remarks>
		[SuppressMessage("ReSharper", "UnusedVariable")]
		public static bool IsValidZipArchive(this System.IO.Stream stream)
		{
			try
			{
				using (var zipArchive = new ZipArchive(stream))
				{
					var entries = zipArchive.Entries;
					return true;
				}
			}
			catch (InvalidDataException)
			{
				return false;
			}
		}

		/// <summary>
		/// Applies a set of <see cref="XmlNamespaceTranslation"/> translations to an XML <see cref="Stream"/>.
		/// </summary>
		/// <param name="stream">
		/// The XML <see cref="Stream"/> to be translated.
		/// </param>
		/// <param name="translations">
		/// The set of <see cref="XmlNamespaceTranslation"/> translations to apply.
		/// </param>
		/// <returns>
		/// The translated <see cref="Stream"/>.
		/// </returns>
		[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Done by XmlTranslatorStream.")]
		public static System.IO.Stream Translate(this System.IO.Stream stream, XmlNamespaceTranslation[] translations)
		{
			return new ReadOnlySeekableStream(new XmlTranslatorStream(XmlReader.Create(stream), translations));
		}
	}
}
