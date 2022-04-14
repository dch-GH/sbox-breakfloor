using Breakfloor.HammerEnts;
using Sandbox;
using System;
using System.Linq;
using Breakfloor.Weapons;

namespace Breakfloor
{
	partial class BreakfloorPlayer : Player
	{
		DamageInfo LastDamage;
		public bool SupressPickupNotices { get; private set; }

		[Net] public BreakFloorBlock LastBlockStoodOn { get; private set; }

		public BreakfloorPlayer()
		{
			Inventory = new Inventory( this );
		}

		public override void Spawn()
		{
			base.Spawn();
		}

		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new WalkController();
			(Controller as WalkController).Gravity = 750.0f; //gotta get that jump height above the blocks.
			Animator = new StandardPlayerAnimator();

			CameraMode = new FirstPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			Clothing.DressEntity( this );

			SupressPickupNotices = true;

			Inventory.Add( new Pistol(), true );
			Inventory.Add( new SMG() );

			SupressPickupNotices = false;
			Health = 100;

			LifeState = LifeState.Alive;
			Health = 100;
			Velocity = Vector3.Zero;
			WaterLevel = 0;

			CreateHull();

			var teamIndex = BreakfloorGame.GetMyTeam( Client );
			var spawn = Entity.All.OfType<BreakfloorSpawnPoint>()
				.Where( x => x.Index == teamIndex )
				.OrderBy( x => Guid.NewGuid() )
				.FirstOrDefault();

			//Log.Info( $"Player:{Client} has teamIndex: {teamIndex}." );
			//Log.Info($"Spawning player {Client} at {spawn} because it has index {spawn.Index}");

			Transform = spawn.Transform;

			ResetInterpolation();

			LastBlockStoodOn = null;
		}

		public override void OnKilled()
		{
			base.OnKilled();

			Inventory.DeleteContents();

			BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex ) );

			Controller = null;

			CameraMode = new SpectateRagdollCamera();

			EnableAllCollisions = false;
			EnableDrawing = false;

			foreach ( var child in Children.OfType<ModelEntity>() )
			{
				child.EnableDrawing = false;
			}
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			//
			// Input requested a weapon switch
			//
			if ( Input.ActiveChild != null )
			{
				ActiveChild = Input.ActiveChild;
			}

			if ( LifeState != LifeState.Alive )
				return;

			if ( GroundEntity != null && GroundEntity.GetType() == typeof( BreakFloorBlock ) )
			{
				LastBlockStoodOn = (BreakFloorBlock)GroundEntity;
			}

			TickPlayerUse();

			SimulateActiveChild( cl, ActiveChild );

		}

		public void SwitchToBestWeapon()
		{
			var best = Children.Select( x => x as BreakfloorWeapon )
				.Where( x => x.IsValid() )
				.FirstOrDefault();

			if ( best == null ) return;

			ActiveChild = best;
		}

		public override void BuildInput( InputBuilder input )
		{
			if ( input.Pressed( InputButton.Slot1 ) ) SetActiveSlot( input, Inventory, 0 );
			if ( input.Pressed( InputButton.Slot2 ) ) SetActiveSlot( input, Inventory, 1 );
			if ( input.Pressed( InputButton.Slot3 ) ) SetActiveSlot( input, Inventory, 2 );
			if ( input.Pressed( InputButton.Slot4 ) ) SetActiveSlot( input, Inventory, 3 );
			if ( input.Pressed( InputButton.Slot5 ) ) SetActiveSlot( input, Inventory, 4 );
			if ( input.Pressed( InputButton.Slot6 ) ) SetActiveSlot( input, Inventory, 5 );
			if ( input.Pressed( InputButton.Slot7 ) ) SetActiveSlot( input, Inventory, 6 );
			if ( input.Pressed( InputButton.Slot8 ) ) SetActiveSlot( input, Inventory, 7 );
			if ( input.Pressed( InputButton.Slot9 ) ) SetActiveSlot( input, Inventory, 8 );

		}

		private static void SetActiveSlot( InputBuilder input, IBaseInventory inventory, int i )
		{
			var player = Local.Pawn as Player;

			if ( player == null )
				return;

			var activeChild = player.ActiveChild;
			if ( activeChild is BreakfloorWeapon weapon && weapon.IsReloading ) return; //No weapon switch while reloading

			var ent = inventory.GetSlot( i );
			if ( activeChild == ent )
				return;

			if ( ent == null )
				return;

			input.ActiveChild = ent;
		}

		public override void TakeDamage( DamageInfo info )
		{
			if ( info.Attacker != null && BreakfloorGame.SameTeam( Client, info.Attacker.Client ) )
			{
				return;
			}

			LastDamage = info;

			// hack - hitbox 0 is head
			// we should be able to get this from somewhere
			if ( info.HitboxIndex == 0 )
			{
				info.Damage *= 2.0f;
			}

			base.TakeDamage( info );

			if ( info.Attacker is BreakfloorPlayer attacker && attacker != this )
			{
				// Note - sending this only to the attacker!
				attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, Health.LerpInverse( 100, 0 ) );

				TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position );
			}
		}

		[ClientRpc]
		public void DidDamage( Vector3 pos, float amount, float healthinv )
		{
			HitMarker( healthinv );

			//HitIndicator.Current?.OnHit( pos, amount );
		}

		private async void HitMarker( float pitch )
		{
			await GameTask.Delay( 60 );
			Sound.FromScreen( "ui.bf_hitmarker" ).SetPitch( 1 + pitch * 1 );
		}

		[ClientRpc]
		public void TookDamage( Vector3 pos )
		{
			//DebugOverlay.Sphere( pos, 5.0f, Color.Red, false, 50.0f );

			//DamageIndicator.Current?.OnHit( pos );
		}
	}

}
