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
	[SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Unit test resource.")]
	internal sealed class CompoundMapTransform : TransformBase
	{
		static CompoundMapTransform()
		{
			_xmlContent = @"<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>
	<xsl:import href='map://type/Be.Stateless.BizTalk.Resources.Transform.CompoundContextMapTransform, Be.Stateless.BizTalk.Stream.Tests, Version=2.0.0.0, Culture=neutral, PublicKeyToken=3707daa0b119fc14' />
	<xsl:template match='two'><second><xsl:value-of select='text()'/></second></xsl:template>
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
