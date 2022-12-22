using Sandbox;
using System;
using System.Linq;
using Breakfloor.Weapons;

namespace Breakfloor;

[Title( "Player" ), Icon( "emoji_people" )]
public partial class Player : AnimatedEntity
{
	[Net, Predicted]
	public PlayerController Controller { get; set; }

	[Net, Predicted] public Entity ActiveChild { get; set; }

	public Angles OriginalViewAngles { get; private set; }

	[Net] public Team Team { get; set; }
	[Net] public BreakFloorBlock LastBlockStoodOn { get; private set; }

	[Net] public SMG Gun { get; private set; }

	public TimeSince TimeSinceDeath { get; private set; }
	public DamageInfo LastDamage { get; private set; }

	// These are for resetting/setting the player view angles
	// to that of their spawnpoint direction, so the player faces the correct direction on respawn.
	// We need to use BuildInput because Input.Rotation is carried over
	// between disconnects/gamemode restarts and will get applied instantly in Simualate. Needs to be overridden manually. :)
	private bool shouldOrientView;
	private Angles spawnViewAngles;

	[Net, Predicted]
	public Vector3 EyeLocalPosition { get; set; }

	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

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
	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );

	public Player() { }

	public override void Spawn()
	{
		EnableLagCompensation = true;

		Tags.Add( "player" );

		base.Spawn();
		Gun = new SMG();
		Gun.Owner = this;
		Gun.Parent = this;

		// TODO: find a way to make flashlights look good on other (worldview)
		// players. Also, other player's worldview flashlight shines through geo (any map/gamemode) and onto the
		// viewmodel. Effectively wallhacks in breakfloor when someone is looking your direction.
		// For now, just make flashlights client-side per player only.
	}

	public void Respawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Controller = new PlayerController();
		Controller.Pawn = this;
		Controller.Client = Client;
		Controller.Gravity = 750.0f; //gotta get that jump height above the blocks.

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableLagCompensation = true;
		EnableShadowInFirstPerson = true;

		Gun.Reload();

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

		// We color the clothes every respawn because they might have changed team.
		// (There's also an NRE if you try to clothe in Spawn).
		Clothing.DressEntity( this );
		{
			var teamColor = BreakfloorGame.GetTeamColor( Team );

			// Paint clothes or body to our TeamIndex color.
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

	public override void OnKilled()
	{
		TimeSinceDeath = 0;
		base.OnKilled();

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

		Log.Info( $"{this} died." );
	}

	public override void Simulate( IClient cl )
	{
		if ( LifeState == LifeState.Dead || Input.Released( InputButton.Grenade ) )
		{
			if ( TimeSinceDeath > 2 && Game.IsServer )
			{
				Respawn();
			}

			return;
		}

		Controller?.Simulate();
		SimulateAnimation( Controller );

		Gun?.Simulate( Client );

		if ( LifeState != LifeState.Alive )
			return;

		if ( GroundEntity != null && GroundEntity.GetType() == typeof( BreakFloorBlock ) )
		{
			LastBlockStoodOn = (BreakFloorBlock)GroundEntity;
		}

		//TickPlayerUse();
	}

	private void SimulateAnimation( PlayerController controller )
	{
		if ( controller == null )
			return;

		// where should we be rotated to
		var turnSpeed = 0.02f;

		Rotation rotation;

		// If we're a bot, spin us around 180 degrees.
		if ( Client.IsBot )
			rotation = ViewAngles.WithYaw( ViewAngles.yaw + 180f ).ToRotation();
		else
			rotation = ViewAngles.ToRotation();

		var idealRotation = Rotation.LookAt( rotation.Forward.WithZ( 0 ), Vector3.Up );
		Rotation = Rotation.Slerp( Rotation, idealRotation, controller.WishVelocity.Length * Time.Delta * turnSpeed );
		Rotation = Rotation.Clamp( idealRotation, 45.0f, out var shuffle ); // lock facing to within 45 degrees of look direction

		var animHelper = new CitizenAnimationHelper( this );

		animHelper.WithWishVelocity( controller.WishVelocity );
		animHelper.WithVelocity( Velocity );
		animHelper.WithLookAt( EyePosition + EyeRotation.Forward * 100.0f, 1.0f, 1.0f, 0.5f );
		animHelper.AimAngle = rotation;
		animHelper.FootShuffle = shuffle;
		animHelper.DuckLevel = MathX.Lerp( animHelper.DuckLevel, controller.HasTag( "ducked" ) ? 1 : 0, Time.Delta * 10.0f );
		animHelper.VoiceLevel = (Game.IsClient && Client.IsValid()) ? Client.Voice.LastHeard < 0.5f ? Client.Voice.CurrentLevel : 0.0f : 0.0f;
		animHelper.IsGrounded = GroundEntity != null;
		animHelper.IsSitting = controller.HasTag( "sitting" );
		animHelper.IsNoclipping = controller.HasTag( "noclip" );
		animHelper.IsClimbing = controller.HasTag( "climbing" );
		animHelper.IsSwimming = this.GetWaterLevel() >= 0.5f;
		animHelper.IsWeaponLowered = false;

		if ( controller.HasEvent( "jump" ) )
			animHelper.TriggerJump();

		if ( Gun is not null )
			Gun.SimulateAnimator( animHelper );
		else
		{
			animHelper.HoldType = CitizenAnimationHelper.HoldTypes.None;
			animHelper.AimBodyWeight = 0.5f;
		}
	}

	public override void TakeDamage( DamageInfo info )
	{
		if ( LifeState == LifeState.Dead )
			return;

		if ( info.Attacker != null && this.SameTeam( info.Attacker as Player ) )
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

		if ( info.Attacker is Player attacker && attacker != this )
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
}
