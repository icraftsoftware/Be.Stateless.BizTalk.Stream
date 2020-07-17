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

using System.Diagnostics.CodeAnalysis;
using Microsoft.BizTalk.Streaming;

namespace Be.Stateless.BizTalk.Stream.Extensions
{
	public interface IProbeStream
	{
		/// <summary>
		/// Probes the current <see cref="MarkableForwardOnlyEventingReadStream"/> for the message type.
		/// </summary>
		/// <returns>
		/// The message type if probing is successful, <c>null</c> otherwise.
		/// </returns>
		string MessageType { get; }

		/// <summary>
		/// The stream being probed.
		/// </summary>
		/// <remarks>
		/// This property is meant to be used by custom stream probing extensions.
		/// </remarks>
		[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global", Justification = "Public API.")]
		MarkableForwardOnlyEventingReadStream Stream { get; }
	}
}
