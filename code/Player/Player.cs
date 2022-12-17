using Breakfloor.HammerEnts;
using Sandbox;
using System;
using System.Linq;
using Breakfloor.Weapons;
using Breakfloor.UI;
using System.Runtime.CompilerServices;
using Breakfloor.Events;

namespace Breakfloor;

[Title( "Player" ), Icon( "emoji_people" )]
partial class BreakfloorPlayer : AnimatedEntity
{
	[Net, Predicted]
	public BreakfloorWalkController Controller { get; set; }

	[Net, Predicted] public Entity ActiveChild { get; set; }
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Entity ActiveChildInput { get; set; }
	[ClientInput] public Angles ViewAngles { get; set; }
	public Angles OriginalViewAngles { get; private set; }


	[Net] public Team Team { get; set; }
	[Net] public BreakFloorBlock LastBlockStoodOn { get; private set; }

	public TimeSince TimeSinceDeath { get; private set; }
	public DamageInfo LastDamage { get; private set; }

	// These are for resetting/setting the player view angles
	// to that of their spawnpoint direction, so the player faces the correct direction on respawn.
	// We need to use BuildInput because Input.Rotation is carried over
	// between disconnects/gamemode restarts and will get applied instantly in Simualate. Needs to be overridden manually. :)
	private bool shouldOrientView;
	private Angles spawnViewAngles;

	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	[Net, Predicted]
	public Vector3 EyeLocalPosition { get; set; }

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted]
	public Rotation EyeLocalRotation { get; set; }

	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Override the aim ray to use the player's eye position and rotation.
	/// </summary>
	public override Ray AimRay => new Ray( EyePosition, AimRay.Forward );

	public BreakfloorPlayer()
	{

	}

	public override void Spawn()
	{
		EnableLagCompensation = true;

		Tags.Add( "player" );

		base.Spawn();
		// TODO: find a way to make flashlights look good on other (worldview)
		// players. Also, other player's worldview flashlight shines through geo (any map/gamemode) and onto the
		// viewmodel. Effectively wallhacks in breakfloor when someone is looking your direction.
		// For now, just make flashlights client-side per player only.
	}

	public override void ClientSpawn()
	{
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
	}

	public void Respawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Controller = new BreakfloorWalkController();
		Controller.Pawn = this;
		Controller.Client = Client;
		Controller.Gravity = 750.0f; //gotta get that jump height above the blocks.

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Clothing.DressEntity( this );

		//Inventory = new Inventory( this ); //this should fix the bug of not being able to swap weapons after respawning.
		//Inventory.DeleteContents();
		//Inventory.Add( new SMG(), true );

		LifeState = LifeState.Alive;
		Health = 100;
		Velocity = Vector3.Zero;

		// CreateHull
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16, -16, 0 ), new Vector3( 16, 16, 72 ) );
		EnableHitboxes = true;

		var spawn = Entity.All.OfType<BreakfloorSpawnPoint>()
			.Where( x => x.Index == Team )
			.OrderBy( x => Guid.NewGuid() )
			.FirstOrDefault();

		{
			var teamColor = BreakfloorGame.GetTeamColor( Team );

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

		//Log.Info( $"Player:{IClient} has teamIndex: {teamIndex}." );
		//Log.Info($"Spawning player {IClient} at {spawn} because it has index {spawn.Index}");

		Transform = spawn.Transform;
		ResetInterpolation();

		LastBlockStoodOn = null;

		OrientAnglesToSpawnClient( To.Single( Client ), spawn.Transform.Rotation.Angles() );
		RespawnClient();

	}

	[ClientRpc]
	private void RespawnClient()
	{
		EnableShadowInFirstPerson = true;
		EnableDrawing = false;
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
		TimeSinceDeath = 0;
		base.OnKilled();

		//Inventory.DeleteContents();

		BecomeRagdollOnClient(
			(Velocity / 2) + LastDamage.Force,
			LastDamage.BoneIndex );

		Controller = null;

		//CameraMode = new SpectateRagdollCamera();

		EnableAllCollisions = false;
		EnableDrawing = false;

		foreach ( var child in Children.OfType<ModelEntity>() )
		{
			child.EnableDrawing = false;
		}
	}

	public override void Simulate( IClient cl )
	{
		if ( LifeState == LifeState.Dead )
		{
			if ( TimeSinceDeath > 2 && Game.IsServer )
			{
				Respawn();
			}

			return;
		}

		Controller?.Simulate();

		if ( LifeState != LifeState.Alive )
			return;

		if ( GroundEntity != null && GroundEntity.GetType() == typeof( BreakFloorBlock ) )
		{
			LastBlockStoodOn = (BreakFloorBlock)GroundEntity;
		}

		//TickPlayerUse();

		FlashlightSimulate();
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		Controller?.FrameSimulate();

		// Place camera
		Camera.Position = EyePosition;
		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FieldOfView = 90;

		//Update the flashlight position on the client in framesim
		//so the movement is nice and smooth.
		FlashlightFrameSimulate();
	}

	public override void BuildInput()
	{
		if ( shouldOrientView )
		{
			ViewAngles = spawnViewAngles;
			shouldOrientView = false;
			return;
		}

		OriginalViewAngles = ViewAngles;
		InputDirection = Input.AnalogMove;

		if ( Input.StopProcessing )
			return;

		var look = Input.AnalogLook;

		if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
		{
			look = look.WithYaw( look.yaw * -1f );
		}

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;
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
		if ( Game.IsServer && Health > 0f && LifeState == LifeState.Alive )
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
			Event.Run( BFEVents.LocalPlayerHurt );
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

	private void SetActiveSlot( int i )
	{
		//var player = Game.LocalPawn as Player;

		//if ( player == null )
		//	return;

		//var activeChild = player.ActiveChild;
		//if ( activeChild is BreakfloorGun weapon && weapon.IsReloading ) return; //No weapon switch while reloading

		//var ent = Inventory.GetSlot( i );
		//if ( activeChild == ent )
		//	return;

		//if ( ent == null )
		//	return;

		//input.ActiveChild = ent;
	}

	[ClientRpc]
	public void PlaySoundClient( string snd )
	{
		PlaySound( snd );
	}
}

