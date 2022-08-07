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
		[Net] public Team Team { get; set; }
		[Net] public BreakFloorBlock LastBlockStoodOn { get; private set; }

		TimeSince timeSinceDied;
		DamageInfo LastDamage;

		// These are for resetting/setting the player view angles
		// to that of their spawnpoint direction, so the player faces the correct direction on respawn.
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

			// TODO: find a way to make flashlights look good on other
			// players. Also, the light shines through geo (any map/gamemode) and onto the
			// viewmodel. Effectively wallhacks in breakfloor when someone is looking your direction.

			FlashlightEntity = new SpotLightEntity
			{
				Enabled = false,
				DynamicShadows = true,
				Range = 3200f,
				Falloff = 0.3f,
				LinearAttenuation = 0.3f,
				Brightness = 8f,
				Color = Color.FromBytes( 200, 200, 200, 230 ),
				InnerConeAngle = 9,
				OuterConeAngle = 32,
				FogStrength = 1.0f,
				Owner = this,
				LightCookie = Texture.Load( "materials/effects/lightcookie.vtex" ),

				// this helps with not casting the player model shadow clientside 
				// (from the light being inside player model)
				// no idea what to do for player puppets though, thats still fucked.
				EnableViewmodelRendering = true
			};

			FlashlightPosOffset = 14;
		}

		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new BreakfloorWalkController();
			(Controller as BreakfloorWalkController).Gravity = 750.0f; //gotta get that jump height above the blocks.

			Animator = new StandardPlayerAnimator();

			CameraMode = new FirstPersonCamera();

			EnableAllCollisions = true;
			EnableDrawing = true;
			EnableHideInFirstPerson = true;
			EnableShadowInFirstPerson = true;

			Clothing.DressEntity( this );

			Inventory = new Inventory( this ); //this should fix the bug of not being able to swap weapons after respawning.
			Inventory.DeleteContents();
			Inventory.Add( new Pistol() );
			Inventory.Add( new SMG(), true );

			LifeState = LifeState.Alive;
			Health = 100;
			Velocity = Vector3.Zero;
			WaterLevel = 0;

			CreateHull();

			var teamIndex = Client.GetValue<int>( BreakfloorGame.TeamDataKey );
			var spawn = Entity.All.OfType<BreakfloorSpawnPoint>()
				.Where( x => (int)x.Index == teamIndex )
				.OrderBy( x => Guid.NewGuid() )
				.FirstOrDefault();

			{
				var teamColor = BreakfloorGame.GetTeamColor( teamIndex.ToTeam() );

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
			RespawnClient();

		}

		[ClientRpc]
		private void RespawnClient()
		{

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

			BecomeRagdollOnClient(
				Velocity,
				LastDamage.Flags,
				LastDamage.Position,
				LastDamage.Force,
				GetHitboxBone( LastDamage.HitboxIndex ) );

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
			FlashlightSimulate();
		}

		public override void FrameSimulate( Client cl )
		{
			base.FrameSimulate( cl );

			//Update the flashlight position on the client in framesim
			//so the movement is nice and smooth.
			FlashlightFrameSimulate();
		}

		public override void BuildInput( InputBuilder input )
		{
			if ( input.Pressed( InputButton.Slot1 ) ) SetActiveSlot( input, 0 );
			if ( input.Pressed( InputButton.Slot2 ) ) SetActiveSlot( input, 1 );
			if ( input.Pressed( InputButton.Slot3 ) ) SetActiveSlot( input, 2 );
			if ( input.Pressed( InputButton.Slot4 ) ) SetActiveSlot( input, 3 );
			if ( input.Pressed( InputButton.Slot5 ) ) SetActiveSlot( input, 4 );
			if ( input.Pressed( InputButton.Slot6 ) ) SetActiveSlot( input, 5 );
			if ( input.Pressed( InputButton.Slot7 ) ) SetActiveSlot( input, 6 );
			if ( input.Pressed( InputButton.Slot8 ) ) SetActiveSlot( input, 7 );
			if ( input.Pressed( InputButton.Slot9 ) ) SetActiveSlot( input, 8 );

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

			if ( info.Attacker != null && this.SameTeam( info.Attacker as BreakfloorPlayer ) )
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

		private void SetActiveSlot( InputBuilder input, int i )
		{
			var player = Local.Pawn as Player;

			if ( player == null )
				return;

			var activeChild = player.ActiveChild;
			if ( activeChild is BreakfloorWeapon weapon && weapon.IsReloading ) return; //No weapon switch while reloading

			var ent = Inventory.GetSlot( i );
			if ( activeChild == ent )
				return;

			if ( ent == null )
				return;

			input.ActiveChild = ent;
		}

		[ClientRpc]
		public void PlaySoundClient( string snd )
		{
			PlaySound( snd );
		}
	}
}
