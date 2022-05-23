using System;
using Sandbox;
using SandboxEditor;

namespace Breakfloor.HammerEnts
{
	[HammerEntity]
	[ClassName("bf_spawn")]
	public class BreakfloorSpawnPoint : SpawnPoint
	{
		//this is fucked
		[Property]
		public int Index { get; set; }
	}
}
