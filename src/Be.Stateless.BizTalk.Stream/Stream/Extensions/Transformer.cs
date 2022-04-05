﻿#region Copyright & License

// Copyright © 2012 - 2022 François Chabot
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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using Be.Stateless.BizTalk.Message.ExtensionObjects;
using Be.Stateless.BizTalk.Namespaces;
using Be.Stateless.BizTalk.Runtime.Caching;
using Be.Stateless.BizTalk.Xml.Xsl;
using Be.Stateless.BizTalk.Xml.Xsl.Extensions;
using Microsoft.BizTalk.Message.Interop;
using Microsoft.BizTalk.Streaming;

namespace Be.Stateless.BizTalk.Stream.Extensions
{
	/// <summary>
	/// Provides map/transform extensions to XML streams.
	/// </summary>
	/// <seealso cref="StreamExtensions.Transform(System.IO.Stream)"/>
	[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
	internal class Transformer : ITransformStream
	{
		public Transformer(System.IO.Stream[] streams)
		{
			_streams = streams ?? throw new ArgumentNullException(nameof(streams));
			if (!streams.Any()) throw new ArgumentException("IEnumerable<Stream> contains no stream.", nameof(streams));
		}

		#region ITransformStream Members

		public System.IO.Stream Apply(Type transform)
		{
			return Apply(transform, Encoding.UTF8);
		}

		public System.IO.Stream Apply(Type transform, Encoding encoding)
		{
			if (transform == null) throw new ArgumentNullException(nameof(transform));
			if (encoding == null) throw new ArgumentNullException(nameof(encoding));
			if (!transform.IsTransform())
				throw new ArgumentException(
					$"The type {transform.AssemblyQualifiedName} does not derive from TransformBase.",
					nameof(transform));

			var transformDescriptor = LookupTransformDescriptor(transform);
			var xsltArgumentList = BuildArgumentList(transformDescriptor, null);
			return Apply(transformDescriptor.CompiledXslt, xsltArgumentList, encoding);
		}

		public System.IO.Stream Apply(Type transform, XsltArgumentList arguments)
		{
			return Apply(transform, arguments, Encoding.UTF8);
		}

		public System.IO.Stream Apply(Type transform, XsltArgumentList arguments, Encoding encoding)
		{
			if (transform == null) throw new ArgumentNullException(nameof(transform));
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));
			if (encoding == null) throw new ArgumentNullException(nameof(encoding));
			if (!transform.IsTransform())
				throw new ArgumentException(
					$"The type {transform.AssemblyQualifiedName} does not derive from TransformBase.",
					nameof(transform));

			var transformDescriptor = LookupTransformDescriptor(transform);
			var xsltArgumentList = BuildArgumentList(transformDescriptor, arguments);
			return Apply(transformDescriptor.CompiledXslt, xsltArgumentList, encoding);
		}

		public ITransformStream ExtendWith(IBaseMessageContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			return this;
		}

		#endregion

		protected virtual XslCompiledTransformDescriptor LookupTransformDescriptor(Type transform)
		{
			return XsltCache.Instance[transform];
		}

		[SuppressMessage("ReSharper", "InvertIf")]
		internal XsltArgumentList BuildArgumentList(XslCompiledTransformDescriptor descriptor, XsltArgumentList arguments)
		{
			// Ensures a fresh copy of descriptor.Arguments is returned should it be augmented with either the current
			// message context or specific arguments passed along the way. It is *detrimental* for the IBaseMessageContext
			// not to be shared across different transform executions! Notice that because XsltArgumentList.Union() yields
			// a new XsltArgumentList instance, it is not necessary to .Clone() descriptor.Arguments beforehand.
			if ((descriptor.ExtensionRequirements & ExtensionRequirements.MessageContext) == ExtensionRequirements.MessageContext)
			{
				arguments = descriptor.Arguments.Union(arguments);
				arguments.AddExtensionObject(
					ExtensionObjectNamespaces.MessageContext,
					new BaseMessageContextFunctions(_context, descriptor.NamespaceResolver));
				return arguments;
			}

			// If no specific arguments has been passed along then it is assumed that descriptor.Arguments references only
			// stateless extension objects and can consequently be shared across different transform executions. Again,
			// notice XsltArgumentList.Union() yields a new XsltArgumentList instance.
			return arguments != null ? descriptor.Arguments.Union(arguments) : descriptor.Arguments;
		}

		private System.IO.Stream Apply(XslCompiledTransform xsl, XsltArgumentList arguments, Encoding encoding)
		{
			var output = new VirtualStream(DEFAULT_BUFFER_SIZE, DEFAULT_THRESHOLD_SIZE);
			// Clone() to get a modifiable copy of the transform's settings
			var settings = xsl.OutputSettings.Clone();
			settings.CloseOutput = false;
			settings.Encoding = encoding;
			if (settings.OutputMethod == XmlOutputMethod.Text) settings.NewLineHandling = NewLineHandling.None;
			using (var writer = XmlWriter.Create(output, settings))
			{
				if (_streams.Length == 1)
				{
					if (_streams[0] is CompositeXmlStream compositeStream) xsl.Transform(compositeStream.Streams, arguments, writer);
					else xsl.Transform(_streams[0], arguments, writer);
				}
				else
				{
					xsl.Transform(_streams, arguments, writer);
				}

				output.Seek(0, SeekOrigin.Begin);
				return output;
			}
		}

		private const int DEFAULT_BUFFER_SIZE = 1024 * 10; // 10 KB
		private const int DEFAULT_THRESHOLD_SIZE = 1024 * 1024; // 1 MB
		private readonly System.IO.Stream[] _streams;
		private IBaseMessageContext _context;
	}
}
