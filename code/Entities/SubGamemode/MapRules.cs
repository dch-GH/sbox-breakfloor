using System;
using Sandbox;
using SandboxEditor;

namespace Breakfloor
{
	[HammerEntity]
	[EditorModel( "models/editor/cordon_helper.vmdl" )]
	[Category( "Logic" )]
	[ClassName( "bf_map_rules" )]
	public class MapRules : Entity
	{
		[Property]
		[Description( "How many teams are on this map." )]
		public int TeamCount { get; set; } = 2;

		[Property]
		[Description( "How many players can be on each team." )]
		public int MaxTeamSize { get; set; } = 1;
	}
}


