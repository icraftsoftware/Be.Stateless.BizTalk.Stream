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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using Be.Stateless.BizTalk.Runtime.Caching;
using Be.Stateless.BizTalk.XPath;
using Microsoft.BizTalk.XPath;

namespace Be.Stateless.BizTalk.Stream
{
	public class XmlEnvelopeDecodingStream : Microsoft.BizTalk.Streaming.XmlTranslatorStream
	{
		[SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Public API.")]
		public XmlEnvelopeDecodingStream(System.IO.Stream stream, string bodyXPath) : this(XmlReader.Create(stream, new XmlReaderSettings { CloseInput = true }), bodyXPath) { }

		public XmlEnvelopeDecodingStream(XmlReader reader, string bodyXPath) : this(reader, XPathCollectionFactory.Create(bodyXPath)) { }

		private XmlEnvelopeDecodingStream(XmlReader reader, XPathCollection xpathExpressions) : base(new XPathReader(reader, xpathExpressions))
		{
			XPathReader = (XPathReader) m_reader;
		}

		#region Base Class Member Overrides

		protected override void TranslateElement()
		{
			if (!_decoded && XPathReader.IsEmptyElement)
			{
				base.TranslateStartElement(XPathReader.Prefix, XPathReader.LocalName, XPathReader.NamespaceURI);
				base.TranslateAttributes();
				// considering only empty elements ensures that body xpath is processed to its deepest
				DecodeEnvelope();
				base.TranslateEndElement(true);
			}
			else
			{
				base.TranslateElement();
			}
		}

		protected override void TranslateEndElement(bool full)
		{
			if (!_decoded)
			{
				// walking the elements backwards (from deepest upwards) ensures that body xpath has been processed to its deepest
				DecodeEnvelope();
			}
			Debug.Assert(full || !_decoded); // if not full then not yet decoded
			// after envelope has been decoded, children might have been injected and only fully-ended elements can consequently follow
			base.TranslateEndElement(true);
		}

		#endregion

		private XPathReader XPathReader { get; }

		[SuppressMessage("ReSharper", "InvertIf")]
		private void DecodeEnvelope()
		{
			var matchedXPathExpressionIndexes = Enumerable.Range(0, XPathReader.XPathList.Count).Where(i => XPathReader.Match(i)).ToArray();
			var matchedXPathExpressionIndex = matchedXPathExpressionIndexes.Any() ? matchedXPathExpressionIndexes.Single() : -1;
			// not matching the body xpath at its deepest ---i.e. the last expression of XPathReader.XPathList--- means that there
			// are XML elements to inject underneath the deepest expression matched to complete the full extent of the body xpath.
			if (0 <= matchedXPathExpressionIndex && matchedXPathExpressionIndex < XPathReader.XPathList.Count)
			{
				InsertNextMissingBodyXPathElement(matchedXPathExpressionIndex + 1);
				_decoded = true;
			}
		}

		[SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
		private void InsertNextMissingBodyXPathElement(int index)
		{
			if (index >= XPathReader.XPathList.Count) return;

			// pattern forces match from end of line leftwards
			const string pattern = XPathCollectionFactory.PATTERN + "$";
			var match = RegexCache.Instance[pattern].Match(XPathReader.XPathList[index].XPath);
			if (!match.Success) throw new InvalidOperationException("Body XPath does not match expected pattern.");
			var name = match.Groups["name"].Value;
			var ns = match.Groups["ns"].Value;
			base.TranslateStartElement(XPathReader.Prefix, name, ns); // might redefine prefix
			InsertNextMissingBodyXPathElement(index + 1);
			base.TranslateEndElement(true);
		}

		private bool _decoded;
	}
}
