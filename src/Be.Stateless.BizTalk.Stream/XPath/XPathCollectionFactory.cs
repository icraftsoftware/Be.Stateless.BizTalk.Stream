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
using System.Linq;
using System.Text.RegularExpressions;
using Be.Stateless.BizTalk.Runtime.Caching;
using Microsoft.BizTalk.XPath;

namespace Be.Stateless.BizTalk.XPath
{
	internal static class XPathCollectionFactory
	{
		[SuppressMessage("ReSharper", "PossibleMultipleEnumeration", Justification = "Any does not really enumerate.")]
		internal static XPathCollection Create(string bodyXPath)
		{
			var segments = RegexCache.Instance[PATTERN].Matches(bodyXPath).Cast<Match>()
				.Where(m => m.Success)
				.Select(m => m.Value);
			if (!segments.Any()) throw new ArgumentException("Body XPath does not match expected pattern.", nameof(bodyXPath));

			var collection = new XPathCollection();
			var partialPath = string.Empty;
			foreach (var segment in segments)
			{
				collection.Add(partialPath += segment);
			}
			if (partialPath.Length != bodyXPath.Length) throw new ArgumentException("Body XPath could not be entirely parsed.", nameof(bodyXPath));
			return collection;
		}

		internal const string PATTERN = @"/\*\[local-name\(\)='(?<name>\S+)'\s+and\s+namespace-uri\(\)='(?<ns>\S+)']";
	}
}
