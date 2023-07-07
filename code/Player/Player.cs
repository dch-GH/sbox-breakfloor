using Sandbox;
using System;
using System.Linq;
using Breakfloor.Weapons;

namespace Breakfloor;

[Title( "Player" ), Icon( "emoji_people" )]
public partial class Player : AnimatedEntity
{
	[Net, Predicted]
	public PawnController Controller { get; set; }

	[Net] public Team Team { get; set; }
	[Net] public BreakFloorBlock LastBlockStoodOn { get; private set; }

	[Net] public SMG Gun { get; private set; }

	public TimeSince TimeSinceDeath { get; private set; }
	public DamageInfo LastDamage { get; private set; }

	public Player() { }

	public override void Spawn()
	{
		Tags.Add( "player" );

		SetModel( "models/humans/male.vmdl" );
		var headModel = Model.Load( "models/humans/heads/adam/adam.vmdl" );

		var head = new AnimatedEntity();
		head.Model = headModel;
		head.EnableHideInFirstPerson = true;
		head.EnableShadowInFirstPerson = true;
		head.SetParent( this, true );

		Gun = new SMG();
		Gun.Owner = this;
		Gun.SetParent( this, true );

		EnableLagCompensation = true;

		// TODO: find a way to make flashlights look good on other (worldview)
		// players. Also, other player's worldview flashlight shines through geo (any map/gamemode) and onto the
		// viewmodel. Effectively wallhacks in breakfloor when someone is looking your direction.
		// For now, just make flashlights client-side per player only.
	}

	public void Respawn()
	{
		Controller = new WalkController();

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableLagCompensation = true;
		EnableShadowInFirstPerson = true;


		foreach ( var child in Children.OfType<ModelEntity>() )
			child.EnableDrawing = true;

		Gun.Reset();

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
		//Clothing.DressEntity( this );
		//{
		//	var teamColor = BreakfloorGame.GetTeamColor( Team );

		//	// Paint clothes or body to our TeamIndex color.
		//	if ( Clothing.ClothingModels.Count > 0 )
		//	{
		//		foreach ( var item in Clothing.ClothingModels )
		//		{
		//			item.RenderColor = teamColor;
		//		}
		//	}
		//	else
		//	{
		//		RenderColor = teamColor;
		//	}
		//}

		//Log.Info( $"Player:{IClient} has teamIndex: {teamIndex}." );
		//Log.Info($"Spawning player {IClient} at {spawn} because it has index {spawn.Index}");

		Transform = spawn is null ? new Transform( new Vector3( 0, 128, 0 ), Rotation.Identity, 1 ) : spawn.Transform;
		ResetInterpolation();

		LastBlockStoodOn = null;

		OrientAnglesToSpawnClient( To.Single( Client ), spawn.Transform.Rotation.Angles() );
	}

	public override void OnKilled()
	{
		TimeSinceDeath = 0;

		// No ragdoll for FP humanoid male *yet*.

		//BecomeRagdollOnClient(
		//	(Velocity / 2) + LastDamage.Force,
		//	LastDamage.BoneIndex );

		EnableAllCollisions = false;
		EnableDrawing = false;

		foreach ( var child in Children.OfType<ModelEntity>() )
		{
			child.EnableDrawing = false;
		}

		LifeState = LifeState.Dead;

		Log.Info( $"{this} died." );
		GameManager.Current?.OnKilled( this );
		Client?.AddInt( "deaths", 1 );

		//
		// Add a score to the killer
		//
		if ( LifeState == LifeState.Dead && LastDamage.Attacker != null )
		{
			if ( LastDamage.Attacker.Client != null && LastDamage.Attacker != this )
			{
				LastDamage.Attacker.Client.AddInt( "kills" );
			}
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

		Controller?.Simulate( Client, this );
		SimulateAnimation( Controller );

		Gun?.Simulate( Client );

		if ( GroundEntity != null && GroundEntity.GetType() == typeof( BreakFloorBlock ) )
		{
			LastBlockStoodOn = (BreakFloorBlock)GroundEntity;
		}

		//TickPlayerUse();
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
