using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Breakfloor
{
	[Library( "bf_block" )]
	[Hammer.BoxSize( 32 )]
	internal class BreakFloorBlock : ModelEntity
	{
		[Property( "WorldModel" )]
		public string WorldModel { get; set; }

		public override void Spawn()
		{
			base.Spawn();
			Model = Model.Load( WorldModel );
			CollisionGroup = CollisionGroup.Default;
			PhysicsEnabled = false;
			UsePhysicsCollision = true;
			EnableShadowReceive = false;
			Reset();
		}

		public override void TakeDamage( DamageInfo info )
		{
			base.TakeDamage( info );
		}

		public override void OnKilled()
		{
			Sound.FromWorld( "bf_block_glassbreak", Position );
			EnableAllCollisions = false;
			EnableDrawing = false;

		}

		public void Reset()
		{
			Health = BreakfloorGame.BlockHealthCvar;
			EnableAllCollisions = true;
			EnableDrawing = true;
		}
	}
}
