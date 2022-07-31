using System;
using System.Collections.Generic;
using Sandbox;
using SandboxEditor;

namespace Breakfloor
{
	[HammerEntity]
	[DrawAngles]
	[BoundsHelper( "mins", "maxs" )]
	[Title( "Breakfloor Block Volume" ), Category( "Map" ), Icon( "apps" ), Description("Define a box volume to fill with blocks. Axis-aligned.")]
	public partial class BlockSpawnVolume : ModelEntity
	{
		[Property]
		Vector3 Mins { get; set; }

		[Property]
		Vector3 Maxs { get; set; }

		[Property( "BlockModel" )]
		public string Block { get; set; } = "models/bf_block.vmdl";

		//public static bool debug { get; set; } = false;
		private List<Vector3> debugPoints = new();

		public override void Spawn()
		{
			base.Spawn();

			var full = (int)BreakfloorGame.StandardBlockSize;
			var hb = (int)BreakfloorGame.StandardHalfBlockSize;
			Transmit = TransmitType.Always;

			EnableAllCollisions = false; // just in case
			EnableDrawing = false;

			for ( int x = (int)(Position.x + Mins.x) + hb; x < ((int)Position.x + Maxs.x) - hb; x += full )
			{
				for ( int y = (int)(Position.y + Mins.y) + hb; y < ((int)Position.y + Maxs.y) - hb; y += full )
				{
					for ( int z = (int)(Position.z + Mins.z) + hb; z < ((int)Position.z + Maxs.z) - hb; z += full )
					{
						var p = new Vector3( x, y, z );
						debugPoints.Add( p );

						var b = new BreakFloorBlock();

						b.Position = p;
						b.WorldModel = Block;
						b.Spawn();

					}
				}
			}
		}

		//[ConCmd.Admin( "bf_vol_debug" )]
		//public static void ToggleVolumeDebug()
		//{
		//	debug = !debug;
		//}

		//[Event.Tick.Server]
		//private void Tick()
		//{
		//	if ( !debug ) return;

		//	DebugOverlay.Box( Position, mins: Mins, maxs: Maxs, Color.Random.WithAlpha( 0.05f ), depthTest: false );

		//	var minPos = Position + Mins;
		//	var maxPos = Position + Maxs;

		//	DebugOverlay.Axis( minPos, Rotation.Identity, 24, 0, false );
		//	DebugOverlay.Axis( maxPos, Rotation.Identity, 24, 0, false );

		//	DebugOverlay.Line( Position, Position + Rotation.Up, Color.Yellow, depthTest: false );
		//	DebugOverlay.Line( Position, Position + Rotation.Forward, Color.Blue, depthTest: false );
		//	DebugOverlay.Line( Position, Position + Rotation.Right, Color.Red, depthTest: false );

		//	foreach ( var p in debugPoints )
		//	{
		//		DebugOverlay.Circle( p, Rotation.Identity, 3f, Color.White.WithAlpha( 0.5f ), 0, false );
		//		DebugOverlay.Axis( p, Rotation.Identity, 32 );
		//	}
		//}
	}


}
