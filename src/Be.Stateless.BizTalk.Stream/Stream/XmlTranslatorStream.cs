﻿#region Copyright & License

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
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Be.Stateless.Extensions;

namespace Be.Stateless.BizTalk.Stream
{
	public class XmlTranslatorStream : Microsoft.BizTalk.Streaming.XmlTranslatorStream
	{
		[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API.")]
		public XmlTranslatorStream(XmlReader reader, XmlNamespaceTranslation[] translations)
			: this(reader, Encoding.UTF8, translations, XmlTranslationRequirements.Default) { }

		public XmlTranslatorStream(XmlReader reader, Encoding encoding, XmlNamespaceTranslation[] translations, XmlTranslationRequirements modes)
			: base(reader, encoding)
		{
			_translations = translations;
			_modes = modes;
		}

		#region Base Class Member Overrides

		protected override void TranslateAttribute()
		{
			var prefix = m_reader.Prefix;
			var localName = m_reader.LocalName;
			var namespaceUri = m_reader.NamespaceURI;
			if (IsNamespaceAttribute(prefix, localName, namespaceUri))
			{
				m_reader.ReadAttributeValue();
				var declaredNamespaceUri = m_reader.Value;
				var targetNamespaceUri = TranslateNamespaceUri(declaredNamespaceUri);
				if (!IsGlobalNamespaceUri(targetNamespaceUri))
				{
					base.TranslateStartAttribute(prefix, localName, namespaceUri);
					TranslateAttributeValue(prefix, localName, namespaceUri, targetNamespaceUri);
					m_writer.WriteEndAttribute();
				}
			}
			else
			{
				base.TranslateAttribute();
			}
		}

		protected override void TranslateStartAttribute(string prefix, string localName, string nsUri)
		{
			base.TranslateStartAttribute(prefix, localName, _modes.RequiresAttributeNamespaceTranslation() ? TranslateNamespaceUri(nsUri) : nsUri);
		}

		protected override void TranslateStartElement(string prefix, string localName, string nsUri)
		{
			var targetNamespaceUri = TranslateNamespaceUri(nsUri);
			if (IsGlobalNamespaceUri(targetNamespaceUri))
			{
				base.TranslateStartElement(null, localName, null);
			}
			else
			{
				base.TranslateStartElement(prefix, localName, targetNamespaceUri);
			}
		}

		protected override void TranslateXmlDeclaration(string target, string val)
		{
			if (!_modes.RequiresXmlDeclarationAbsorption()) m_writer.WriteStartDocument();
		}

		#endregion

		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		private bool IsNamespaceAttribute(string prefix, string localName, string nsUri)
		{
			return nsUri == XNamespace.Xmlns.NamespaceName && (prefix == "xmlns" || localName == "xmlns");
		}

		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		private bool IsGlobalNamespaceUri(string targetNamespaceUri)
		{
			return targetNamespaceUri.IsNullOrEmpty();
		}

		private string TranslateNamespaceUri(string nsUri)
		{
			var replacement = _translations
				.FirstOrDefault(r => r.MatchingPattern.IsMatch(nsUri));
			return replacement == null
				? nsUri
				: replacement.MatchingPattern.Replace(nsUri, replacement.ReplacementPattern);
		}

		private readonly XmlTranslationRequirements _modes;
		private readonly XmlNamespaceTranslation[] _translations;
	}
}
