using Breakfloor.HammerEnts;
using Sandbox;
using System;
using System.Linq;
using Breakfloor.Weapons;
using Breakfloor.UI;

namespace Breakfloor
{
	partial class BreakfloorPlayer : Player
	{
		TimeSince timeSinceDied;
		DamageInfo LastDamage;

		[Net] public BreakFloorBlock LastBlockStoodOn { get; private set; }

		// These are for resetting/setting the player view angles
		// to that of their spawnpoint direction, so the player faces the correct direction.
		// We need to use BuildInput because Input.Rotation is carried over
		// between disconnects/gamemode restarts and will get applied instantly in Simualate. Needs to be overridden manually. :)
		private bool shouldOrientView;
		private Angles spawnViewAngles;

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

			Inventory.DeleteContents();
			Inventory.Add( new Pistol() );
			Inventory.Add( new SMG(), true );

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

			{
				var teamColor = BreakfloorGame.GetTeamColor( teamIndex );

				//Paint clothes or body to our TeamIndex color.
				if ( Clothing.ClothingModels.Count > 0 )
				{
					foreach ( var item in Clothing.ClothingModels )
					{
						item.RenderColor = teamColor;
					}
				}
				else
				{
					RenderColor = teamColor;
				}
			}


			//Log.Info( $"Player:{Client} has teamIndex: {teamIndex}." );
			//Log.Info($"Spawning player {Client} at {spawn} because it has index {spawn.Index}");

			Transform = spawn.Transform;
			ResetInterpolation();

			LastBlockStoodOn = null;

			OrientAnglesToSpawnClient( To.Single( Client ), spawn.Transform.Rotation.Angles() );

		}

		/// <summary>
		/// See the overridden BuildInput method.
		/// </summary>
		/// <param name="ang"></param>
		[ClientRpc]
		private void OrientAnglesToSpawnClient( Angles ang )
		{
			shouldOrientView = true;
			spawnViewAngles = ang;
		}

		public override void OnKilled()
		{
			timeSinceDied = 0;
			base.OnKilled();

			Inventory.DeleteContents();

			// facepunch fucked up ragdoll stuff with the physics tags update i think, disable it for now.
			//BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex ) );

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
			if ( LifeState == LifeState.Dead )
			{
				if ( timeSinceDied > 2 && IsServer )
				{
					Respawn();
				}

				return;
			}

			var controller = GetActiveController();
			controller?.Simulate( cl, this, GetActiveAnimator() );

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

			if ( shouldOrientView )
			{
				input.ViewAngles = spawnViewAngles;
				shouldOrientView = false;
				return;
			}

		}

		public override void TakeDamage( DamageInfo info )
		{
			if ( LifeState == LifeState.Dead )
				return;

			if ( info.Attacker != null && BreakfloorGame.SameTeam( Client, info.Attacker.Client ) )
			{
				return;
			}

			LastDamage = info;
			this.ProceduralHitReaction( info );

			//
			// Add a score to the killer
			//
			if ( LifeState == LifeState.Dead && info.Attacker != null )
			{
				if ( info.Attacker.Client != null && info.Attacker != this )
				{
					info.Attacker.Client.AddInt( "kills" );
				}
			}

			if ( info.Attacker is BreakfloorPlayer attacker && attacker != this )
			{
				// Note - sending this only to the attacker!
				attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, Health.LerpInverse( 100, 0 ) );

				TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position );
			}

			LastAttacker = info.Attacker;
			LastAttackerWeapon = info.Weapon;
			if ( IsServer && Health > 0f && LifeState == LifeState.Alive )
			{
				Health -= info.Damage;
				if ( Health <= 0f )
				{
					Health = 0f;
					OnKilled();
				}
			}
		}

		[ClientRpc]
		public void TookDamage( Vector3 pos )
		{
			if ( IsLocalPawn )
			{
				//_ = new Sandbox.ScreenShake.Perlin( size: 1.8f, rotation: 0.8f );
				DamageIndicator.Current?.Hurt();
				Breakfloor.UI.Health.Current?.Hurt();
			}
		}

		[ClientRpc]
		public void DidDamage( Vector3 pos, float amount, float healthinv )
		{
			HitMarker( healthinv );
		}

		private async void HitMarker( float pitch )
		{
			await GameTask.Delay( 60 );
			Sound.FromScreen( "ui.bf_hitmarker" ).SetPitch( 1 + pitch * 1 );
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

			Log.Info( "slotttt" );

			input.ActiveChild = ent;
		}

		public void SwitchToBestWeapon()
		{
			var best = Children.Select( x => x as BreakfloorWeapon )
				.Where( x => x.IsValid() )
				.FirstOrDefault();

			if ( best == null ) return;

			ActiveChild = best;
		}
	}
}
