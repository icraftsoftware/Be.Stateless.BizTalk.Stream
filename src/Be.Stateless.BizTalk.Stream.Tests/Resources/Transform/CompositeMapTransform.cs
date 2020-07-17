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
using Microsoft.XLANGs.BaseTypes;

namespace Be.Stateless.BizTalk.Resources.Transform
{
	[SchemaReference(@"Be.Stateless.BizTalk.Schemas.Xml.Any", typeof(Schemas.Xml.Any))]
	internal sealed class CompositeMapTransform : TransformBase
	{
		[SuppressMessage("ReSharper", "StringLiteralTypo")]
		static CompositeMapTransform()
		{
			// TODO do not declare context and bf and bts ns as they are already defined in nested CompoundContextMapTransform
			_xmlContent = @"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'
	xmlns:ctxt='urn:extensions.stateless.be:biztalk:message:context:2012:12'
	xmlns:bf='urn:schemas.stateless.be:biztalk:properties:system:2012:04'
	xmlns:bts='http://schemas.microsoft.com/BizTalk/2003/system-properties'
	exclude-result-prefixes='ctxt bf bts'>
	<xsl:import href='map://type/Be.Stateless.BizTalk.Resources.Transform.IdentityTransform, Be.Stateless.BizTalk.Stream.Tests, Version=2.0.0.0, Culture=neutral, PublicKeyToken=3707daa0b119fc14'/>
	<xsl:include href='map://type/Be.Stateless.BizTalk.Resources.Transform.CompoundMapTransform, Be.Stateless.BizTalk.Stream.Tests, Version=2.0.0.0, Culture=neutral, PublicKeyToken=3707daa0b119fc14'/>
	<xsl:template match='six'><sixth><xsl:value-of select='text()'/></sixth></xsl:template>
</xsl:stylesheet>";
		}

		#region Base Class Member Overrides

		public override string[] SourceSchemas => new[] { @"Be.Stateless.BizTalk.Schemas.Xml.Any" };

		public override string[] TargetSchemas => new[] { @"Be.Stateless.BizTalk.Schemas.Xml.Any" };

		public override string XmlContent => _xmlContent;

		public override string XsltArgumentListContent => @"<ExtensionObjects />";

		#endregion

		private static readonly string _xmlContent;
	}
}
