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

using System.Text.RegularExpressions;
using Be.Stateless.Runtime.Caching;

namespace Be.Stateless.BizTalk.Runtime.Caching
{
	/// <summary>
	/// Runtime memory cache for <see cref="Regex"/>> compiled regular expressions.
	/// </summary>
	/// <seealso cref="Cache{TKey,TItem}"/>
	/// <seealso cref="SlidingCache{TKey,TItem}"/>
	public class RegexCache : SlidingCache<string, Regex>
	{
		/// <summary>
		/// Singleton <see cref="RegexCache"/> instance.
		/// </summary>
		public static RegexCache Instance { get; } = new();

		/// <summary>
		/// Create the singleton <see cref="RegexCache"/> instance.
		/// </summary>
		private RegexCache() : base(key => key, key => new(key, RegexOptions.Compiled)) { }
	}
}
