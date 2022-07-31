using System;
using System.Collections.Generic;
using Sandbox;
using SandboxEditor;

namespace Breakfloor
{
	[HammerEntity]
	[DrawAngles]
	[BoundsHelper( "mins", "maxs" )]
	[ClassName( "bf_block_volume" )]
	[Title( "Breakfloor Block Volume" ), Category( "Map" ), Icon( "apps" ), Description( "Define a box volume to fill with blocks. Axis-aligned." )]
	public partial class BlockSpawnVolume : Entity
	{
		[Property]
		public Vector3 Mins { get; set; }

		[Property]
		public Vector3 Maxs { get; set; }

		[Property( "BlockModel" )]
		public string Block { get; set; } = "models/bf_block.vmdl";

		public static bool debug { get; set; } = false;
		private List<Vector3> debugPoints = new();

		public override void Spawn()
		{
			base.Spawn();
			Transmit = TransmitType.Always;

			var full = (int)BreakfloorGame.StandardBlockSize;
			var hb = (int)BreakfloorGame.StandardHalfBlockSize;

			EnableDrawing = false;

			for ( int x = (int)(Position.x + Mins.x) + hb; x <= ((int)Position.x + Maxs.x) - hb; x += full )
			{
				for ( int y = (int)(Position.y + Mins.y) + hb; y <= ((int)Position.y + Maxs.y) - hb; y += full )
				{
					for ( int z = (int)(Position.z + Mins.z) + hb; z <= ((int)Position.z + Maxs.z) - hb; z += full )
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

		[ConCmd.Admin( "bf_vol_debug" )]
		public static void ToggleVolumeDebug()
		{
			debug = !debug;
		}

		[Event.Tick.Server]
		private void Tick()
		{
			if ( !debug ) return;

			DebugOverlay.Box( Position, mins: Mins, maxs: Maxs, Color.Blue.WithAlpha( 0.45f ), depthTest: false );

			var minPos = Position + Mins;
			var maxPos = Position + Maxs;

			DebugOverlay.Axis( minPos, Rotation.Identity, 24, 0, false );
			DebugOverlay.Axis( maxPos, Rotation.Identity, 24, 0, false );

			DebugOverlay.Sphere( maxPos, 4, Color.White, 0, false );

			DebugOverlay.Axis( Position, Rotation.Identity, BreakfloorGame.StandardBlockSize, depthTest: false );

			foreach ( var p in debugPoints )
			{
				DebugOverlay.Axis( p, Rotation.Identity, 32, 0, false );
			}
		}
	}

	[HammerEntity]
	[DrawAngles]
	[ClassName( "bf_column_volume" )]
	[Title( "Breakfloor Column Volume" ), Category( "Map" ), Icon( "texture" ), Description( "Define a box volume to fill with column. Axis-aligned." )]
	public partial class ColumnSpawnVolume : BlockSpawnVolume
	{
		[Property]
		int NumBlocksPerColumn { get; set; } = 5;

		[Property]
		bool CheckerStyle { get; set; } = false;

		public override void Spawn()
		{
			base.Spawn();
			Transmit = TransmitType.Always;

			var full = (int)BreakfloorGame.StandardBlockSize;
			var hb = (int)BreakfloorGame.StandardHalfBlockSize;

			EnableDrawing = false;

			int i = 0;
			int j = 0;
			for ( int x = (int)(Position.x + Mins.x) + hb; x <= ((int)Position.x + Maxs.x) - hb; x += full )
			{
				i++;

				for ( int y = (int)(Position.y + Mins.y) + hb; y <= ((int)Position.y + Maxs.y) - hb; y += full )
				{
					j++;

					if ( CheckerStyle && (i + j) % 2 == 0 )
						continue;

					new BlockSpawnColumn
					{
						Position = new Vector3( x, y, Position.z - hb ),
						NumBlocks = NumBlocksPerColumn
					}.Spawn();				
				}
			}
		}
	}
}
