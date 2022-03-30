using System;
using Sandbox;

namespace Breakfloor.HammerEnts
{
	[Library("bf_spawn")]
	public class BreakfloorSpawnPoint : SpawnPoint
	{
		[Property]
		public Team Index { get; set; }
	}
}
