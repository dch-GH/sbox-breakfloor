using System;
using Sandbox;
using Editor;

namespace Breakfloor.HammerEnts
{
	[HammerEntity]
	[ClassName("bf_spawn")]
	[Title( "Breakfloor Spawn Point" ), Category( "Gameplay" ), Icon( "place" )]
	public class BreakfloorSpawnPoint : SpawnPoint
	{
		[Property]
		public Team Index { get; set; } = Team.None;
	}
}
