using System;
using Sandbox;

namespace Breakfloor.HammerEnts
{
	[Library("bf_spawn")]
	public class BreakfloorSpawnPoint : SpawnPoint
	{
		//this is fucked
		[Property]
		public int Index { get; set; }
	}
}
