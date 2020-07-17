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
using Be.Stateless.BizTalk.Stream.Extensions;
using Microsoft.BizTalk.Streaming;
using Moq;

namespace Be.Stateless.BizTalk.Unit.Stream
{
	[SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API")]
	[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Public API")]
	public class ProbeStreamMockInjectionScope : IDisposable
	{
		public ProbeStreamMockInjectionScope()
		{
			_proberFactory = StreamExtensions.StreamProberFactory;
			Mock = new();
			StreamExtensions.StreamProberFactory = _ => Mock.Object;
		}

		#region IDisposable Members

		public void Dispose()
		{
			StreamExtensions.StreamProberFactory = _proberFactory;
		}

		#endregion

		public Mock<IProbeStream> Mock { get; }

		private readonly Func<MarkableForwardOnlyEventingReadStream, IProbeStream> _proberFactory;
	}
}
