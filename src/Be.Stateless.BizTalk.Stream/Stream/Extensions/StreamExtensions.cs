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
using Microsoft.BizTalk.Streaming;
using Microsoft.XLANGs.BaseTypes;

namespace Be.Stateless.BizTalk.Stream.Extensions
{
	/// <summary>
	/// Provides dependency injection support to <see cref="System.IO.Stream"/> extension methods through various categories of
	/// dedicated extension interfaces.
	/// </summary>
	/// <remarks>
	/// The purpose of this factory is to make <see cref="System.IO.Stream"/> extension methods amenable to mocking, <see
	/// href="http://blogs.clariusconsulting.net/kzu/how-to-mock-extension-methods/"/>.
	/// </remarks>
	/// <seealso href="http://blogs.clariusconsulting.net/kzu/how-extension-methods-ruined-unit-testing-and-oop-and-a-way-forward/"/>
	/// <seealso href="http://blogs.clariusconsulting.net/kzu/making-extension-methods-amenable-to-mocking/"/>
	[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API.")]
	public static class StreamExtensions
	{
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Mock Injection Hook")]
		[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Mock Injection Hook")]
		internal static Func<MarkableForwardOnlyEventingReadStream, IProbeStream> StreamProberFactory { get; set; } = stream => new Prober(stream);

		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Mock Injection Hook")]
		[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global", Justification = "Mock Injection Hook")]
		internal static Func<System.IO.Stream[], ITransformStream> StreamTransformerFactory { get; set; } = streams => new Transformer(streams);

		/// <summary>
		/// Ensure the <see cref="System.IO.Stream"/> is wrapped in a <see cref="MarkableForwardOnlyEventingReadStream"/> and
		/// thereby ready for probing, see <see cref="Probe"/>.
		/// </summary>
		/// <param name="stream">
		/// The current <see cref="System.IO.Stream"/>.
		/// </param>
		/// <returns>
		/// A <see cref="MarkableForwardOnlyEventingReadStream"/> stream.
		/// </returns>
		public static MarkableForwardOnlyEventingReadStream AsMarkable(this System.IO.Stream stream)
		{
			return MarkableForwardOnlyEventingReadStream.EnsureMarkable(stream);
		}

		/// <summary>
		/// Ensure the <see cref="System.IO.Stream"/> is wrapped in a <see cref="MarkableForwardOnlyEventingReadStream"/> and
		/// thereby ready for probing, see <see cref="Probe"/>.
		/// </summary>
		/// <param name="stream">
		/// The current <see cref="System.IO.Stream"/>.
		/// </param>
		/// <returns>
		/// A <see cref="MarkableForwardOnlyEventingReadStream"/> stream.
		/// </returns>
		/// <exception cref="InvalidCastException">
		/// If <paramref name="stream"/> is not already wrapped in a <see cref="MarkableForwardOnlyEventingReadStream"/>.
		/// </exception>
		[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
		public static MarkableForwardOnlyEventingReadStream EnsureMarkable(this System.IO.Stream stream)
		{
			return (MarkableForwardOnlyEventingReadStream) stream;
		}

		/// <summary>
		/// Support for <see cref="System.IO.Stream"/> probing.
		/// </summary>
		/// <param name="stream">
		/// The current <see cref="System.IO.Stream"/>.
		/// </param>
		/// <returns>
		/// The <see cref="IProbeStream"/> instance that will probe the current <see cref="System.IO.Stream"/>s.
		/// </returns>
		/// <remarks>
		/// The <paramref name="stream"/> is expected to be markable, that is to say, it has to be of type <see
		/// cref="MarkableForwardOnlyEventingReadStream"/>.
		/// </remarks>
		public static IProbeStream Probe(this System.IO.Stream stream)
		{
			return StreamProberFactory(stream.EnsureMarkable());
		}

		/// <summary>
		/// Support for <see cref="TransformBase"/>-derived transforms directly applied to one <see cref="System.IO.Stream"/>.
		/// </summary>
		/// <param name="stream">
		/// The current <see cref="System.IO.Stream"/>.
		/// </param>
		/// <returns>
		/// The <see cref="ITransformStream"/> instance that will apply the transform on the current <see
		/// cref="System.IO.Stream"/>.
		/// </returns>
		public static ITransformStream Transform(this System.IO.Stream stream)
		{
			return StreamTransformerFactory(new[] { stream });
		}

		/// <summary>
		/// Support for <see cref="TransformBase"/>-derived transforms directly applied to several <see
		/// cref="System.IO.Stream"/>s.
		/// </summary>
		/// <param name="streams">
		/// The current <see cref="System.IO.Stream"/>s.
		/// </param>
		/// <returns>
		/// The <see cref="ITransformStream"/> instance that will apply the transform on the current <see
		/// cref="System.IO.Stream"/>s.
		/// </returns>
		public static ITransformStream Transform(this System.IO.Stream[] streams)
		{
			return StreamTransformerFactory(streams);
		}
	}
}
