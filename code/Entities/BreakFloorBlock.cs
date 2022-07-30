using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using SandboxEditor;

namespace Breakfloor
{
	[HammerEntity]
	[ClassName( "bf_block" )]
	[BoxSize( 64 )]
	public class BreakFloorBlock : ModelEntity
	{
		[Property( "WorldModel" )]
		public string WorldModel { get; set; }

		public bool Broken => LifeState == LifeState.Dead;

		public override void Spawn()
		{
			base.Spawn();
			Model = Model.Load( WorldModel );

			Tags.Add( "solid" );

			PhysicsEnabled = false;
			UsePhysicsCollision = true;
			EnableShadowReceive = false;

			Reset();
		}

		public override void TakeDamage( DamageInfo info )
		{
			RenderColor = RenderColor.Darken( 0.35f );
			base.TakeDamage( info );
		}

		public override void OnKilled()
		{
			Sound.FromWorld( "bf_block_glassbreak", Position ).SetVolume( 0.6f );
			EnableAllCollisions = false;
			EnableDrawing = false;
			LifeState = LifeState.Dead;
		}

		public void Reset()
		{
			LifeState = LifeState.Alive;
			Health = BreakfloorGame.BlockHealthCvar;
			EnableAllCollisions = true;
			EnableDrawing = true;
			RenderColor = Color.White;
		}
	}
}
