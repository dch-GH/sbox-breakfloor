using System;
using System.Collections.Generic;
using Sandbox;
using Editor;

namespace Breakfloor
{
	[HammerEntity]
	[DrawAngles]
	[EditorModel( "models/bf_block.vmdl", CastShadows = false )]
	[Title( "Breakfloor Block Column" ), Category( "Map" ), Icon( "more_vert" ), Description( "Define a vertical column to fill with blocks." )]
	public partial class BlockSpawnColumn : Entity
	{
		[Property]
		public int NumBlocks { get; set; } = 5;

		[Property( "BlockModel" )]
		public string Block { get; set; } = "models/bf_block.vmdl";

		public static bool debug { get; set; } = false;
		private List<Vector3> debugPoints = new();

		public override void Spawn()
		{
			base.Spawn();
			Transmit = TransmitType.Always;
			EnableDrawing = false;
		}

		/// <summary>
		/// Because the column is spawnable with a volume entity, we want to use the position as
		/// set from the volume entity's code. Need to do that in PostSpawn because Position isnt set in Spawn!
		/// I wish it were though :)
		/// </summary>
		[Event.Entity.PostSpawn]
		public void PostSpawn()
		{
			Game.AssertServer();

			var full = (int)BreakfloorGame.StandardBlockSize;
			var blockCount = NumBlocks - 1;
			var end = Position + (Rotation.Down * (blockCount * full));

			for ( int z = (int)end.z; z <= (int)Position.z; z += full )
			{
				var p = new Vector3( Position.x, Position.y, z );
				debugPoints.Add( p );

				var b = new BreakFloorBlock();
				b.Position = p;
				b.WorldModel = Block;
				b.Spawn();
			}
		}

		[ConCmd.Admin( "bf_column_debug" )]
		public static void ToggleVolumeDebug()
		{
			debug = !debug;
		}

		[Event.Tick.Server]
		private void Tick()
		{
			if ( !debug ) return;

			DebugOverlay.Axis( Position, Rotation.Identity, 24, 0, false );
			DebugOverlay.Axis( Position + Rotation.Down * ((NumBlocks - 1) * BreakfloorGame.StandardBlockSize), Rotation.FromYaw( Time.Tick ), 24, 0, false );

			DebugOverlay.Sphere( Position, 2f, Color.Yellow, depthTest: false );
			DebugOverlay.Box(
				Position,
				-BreakfloorGame.StandardHalfBlockSize + Rotation.Up * BreakfloorGame.StandardBlockSize,
				BreakfloorGame.StandardHalfBlockSize + Rotation.Down * (NumBlocks * BreakfloorGame.StandardBlockSize),
				Color.Blue,
				0,
				false );

			foreach ( var p in debugPoints )
			{
				DebugOverlay.Circle( p, Rotation.Identity, 3f, Color.White.WithAlpha( 0.5f ), 0, false );
				DebugOverlay.Axis( p, Rotation.Identity, 32 );
			}
		}
	}
}
