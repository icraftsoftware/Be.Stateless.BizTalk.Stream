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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using Be.Stateless.Linq;

namespace Be.Stateless.BizTalk.Stream
{
	[XmlRoot(ElementName = "XmlTranslations", Namespace = NAMESPACE)]
	public class XmlTranslationSet : IEquatable<XmlTranslationSet>
	{
		#region Operators

		public static bool operator ==(XmlTranslationSet left, XmlTranslationSet right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(XmlTranslationSet left, XmlTranslationSet right)
		{
			return !Equals(left, right);
		}

		#endregion

		#region IEquatable<XmlTranslationSet> Members

		public bool Equals(XmlTranslationSet other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Override.Equals(other.Override) && Items.SequenceEqual(other.Items);
		}

		#endregion

		#region Base Class Member Overrides

		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((XmlTranslationSet) obj);
		}

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			unchecked
			{
				return (Override.GetHashCode() * 397) ^ (Items != null ? Items.GetHashCode() : 0);
			}
		}

		#endregion

		[SuppressMessage("Performance", "CA1819:Properties should not return arrays")]
		[XmlElement("NamespaceTranslation")]
		public XmlNamespaceTranslation[] Items
		{
			get => _items;
			set
			{
				_items = value ?? Empty.Items;
				CheckItemsUniqueness(_items);
			}
		}

		[XmlAttribute("override")]
		public bool Override { get; set; }

		public XmlTranslationSet Union(XmlTranslationSet second)
		{
			if (second == null) throw new ArgumentNullException(nameof(second));
			return Override
				? this
				: new XmlTranslationSet { Items = Items.Union(second.Items).ToArray() };
		}

		[Conditional("DEBUG")]
		[SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		[SuppressMessage("ReSharper", "UseStringInterpolation")]
		private void CheckItemsUniqueness(IEnumerable<XmlNamespaceTranslation> items)
		{
			var conflictingReplacements = items
				// find MatchingPatterns declared multiple times
				.GroupBy(i => i.MatchingPatternString)
				// keep only those that have conflicting ReplacementPatterns, i.e. several distinct ones
				.Where(g => g.Distinct(new LambdaComparer<XmlNamespaceTranslation>((lns, rns) => lns.ReplacementPattern == rns.ReplacementPattern)).Count() > 1)
				.ToArray();

			if (conflictingReplacements.Any())
				throw new ArgumentException(
					string.Format(
						"[{0}] matchingPatterns have respectively the following conflicting replacementPatterns: [{1}].",
						string.Join("], [", conflictingReplacements.Select(p => p.Key).ToArray()),
						string.Join("], [", conflictingReplacements.Select(g => string.Join(", ", g.Select(nr => nr.ReplacementPattern).ToArray())).ToArray())));
		}

		public const string NAMESPACE = "urn:schemas.stateless.be:biztalk:translations:2013:07";
		public static readonly XmlTranslationSet Empty = new XmlTranslationSet { Items = Array.Empty<XmlNamespaceTranslation>() };
		private XmlNamespaceTranslation[] _items;
	}
}
