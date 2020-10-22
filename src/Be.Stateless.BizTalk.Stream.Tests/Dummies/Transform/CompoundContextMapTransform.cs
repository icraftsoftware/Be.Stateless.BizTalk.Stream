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

namespace Be.Stateless.BizTalk.Dummies.Transform
{
	[SchemaReference("Microsoft.XLANGs.BaseTypes.Any", typeof(Any))]
	internal sealed class CompoundContextMapTransform : TransformBase
	{
		[SuppressMessage("ReSharper", "StringLiteralTypo")]
		static CompoundContextMapTransform()
		{
			_xmlContent = @"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'
	xmlns:ctxt='urn:extensions.stateless.be:biztalk:message:context:2012:12'
	xmlns:bf='urn:schemas.stateless.be:biztalk:properties:system:2012:04'
	xmlns:bts='http://schemas.microsoft.com/BizTalk/2003/system-properties'
	exclude-result-prefixes='ctxt bf bts'>
	<xsl:variable name='environmentTag' select=""ctxt:Read('bf:EnvironmentTag')""/>
	<xsl:variable name='operation' select=""ctxt:Read('bts:Operation')""/>
	<xsl:template match='one'><first><xsl:value-of select='text()'/></first></xsl:template>
</xsl:stylesheet>";
		}

		#region Base Class Member Overrides

		public override string[] SourceSchemas => new[] { typeof(Any).FullName };

		public override string[] TargetSchemas => new[] { typeof(Any).FullName };

		public override string XmlContent => _xmlContent;

		public override string XsltArgumentListContent => @"<ExtensionObjects />";

		#endregion

		private static readonly string _xmlContent;
	}
}
