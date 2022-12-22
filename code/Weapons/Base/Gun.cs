using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sandbox;

namespace Breakfloor;

/// <summary>
/// Generic skeleton for a gun.
/// </summary>
[Display( Name = "BF Weapon Base" ), Icon( "sports_martial_arts" )]
public partial class Gun : AnimatedEntity
{
	public virtual float PrimaryRate => 5.0f;
	public virtual float SecondaryRate => 15.0f;
	public virtual int MaxClip => 10;

	public virtual string ViewModelPath => default;
	public BaseViewModel ViewModelEntity { get; protected set; }
	public PickupTrigger PickupTrigger { get; protected set; }

	[Net, Predicted]
	public TimeSince TimeSinceDeployed { get; set; }

	[Net, Predicted]
	public TimeSince TimeSincePrimaryAttack { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceSecondaryAttack { get; set; }

	[Net, Predicted]
	public TimeSince TimeSinceReload { get; set; }

	[Net, Predicted]
	public bool IsReloading { get; set; }

	public virtual float ReloadTime => 3.0f;

	[Net]
	public int ClipAmmo { get; protected set; }

	public virtual string GetKilledByText( DamageInfo dmg ) { return string.Empty; }

	/// <summary>
	/// Utility - return the entity we should be spawning particles from etc
	/// </summary>
	public virtual ModelEntity EffectEntity => (ViewModelEntity.IsValid() && IsFirstPersonMode) ? ViewModelEntity : this;

	public override void Spawn()
	{
		base.Spawn();
		Tags.Add( "gun" );

		PhysicsEnabled = true;
		UsePhysicsCollision = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		PickupTrigger = new PickupTrigger
		{
			Parent = this,
			Position = Position,
			EnableTouch = true,
			EnableAllCollisions = false,
			EnableSelfCollisions = false
		};

		PickupTrigger.PhysicsBody.AutoSleep = false;
		ClipAmmo = MaxClip;
	}

	public override void ClientSpawn()
	{
		base.ClientSpawn();
		CreateViewModel();
	}

	public override void Simulate( IClient player )
	{
		if ( TimeSinceDeployed < 0.6f )
			return;

		if ( CanReload() || (Owner.IsValid && ClipAmmo <= 0 && player.GetClientData<bool>( Breakfloor.BreakfloorGame.BF_AUTO_RELOAD_KEY )) )
		{
			Reload();
		}

		if ( IsReloading && TimeSinceReload > ReloadTime )
		{
			OnReloadFinish();
		}

		//
		// Reload could have changed our owner
		//
		if ( !Owner.IsValid() )
			return;

		if ( CanPrimaryAttack() )
		{
			using ( LagCompensation() )
			{
				TimeSincePrimaryAttack = 0;
				AttackPrimary();
			}
		}

		//
		// AttackPrimary could have changed our owner
		//
		if ( !Owner.IsValid() )
			return;

		if ( CanSecondaryAttack() )
		{
			using ( LagCompensation() )
			{
				TimeSinceSecondaryAttack = 0;
				AttackSecondary();
			}
		}
	}

	public virtual bool CanReload()
	{
		if ( !Owner.IsValid() || !Input.Down( InputButton.Reload ) ) return false;
		if(ClipAmmo < MaxClip)
			return true;

		return false;
	}

	public virtual void Reload()
	{
		if ( IsReloading )
			return;

		TimeSinceReload = 0;
		IsReloading = true;

		(Owner as AnimatedEntity)?.SetAnimParameter( "b_reload", true );

		StartReloadEffects();
	}


	public virtual void OnReloadFinish()
	{
		IsReloading = false;
		ClipAmmo = MaxClip;
	}

	[ClientRpc]
	public virtual void StartReloadEffects()
	{
		ViewModelEntity?.SetAnimParameter( "reload", true );
	}

	public virtual bool CanPrimaryAttack()
	{
		if ( !Owner.IsValid() || !Input.Down( InputButton.PrimaryAttack ) ) return false;

		var rate = PrimaryRate;
		if ( rate <= 0 ) return true;

		return TimeSincePrimaryAttack > (1 / rate) && ClipAmmo > 0; ;
	}

	public virtual void AttackPrimary()
	{
		ClipAmmo--;
		if ( ClipAmmo - 1 < 0 )
		{
			ClipAmmo = 0;
		}
	}


	public virtual bool CanSecondaryAttack()
	{
		if ( !Owner.IsValid() || !Input.Down( InputButton.SecondaryAttack ) ) return false;

		var rate = SecondaryRate;
		if ( rate <= 0 ) return true;

		return TimeSinceSecondaryAttack > (1 / rate);
	}

	public virtual void AttackSecondary()
	{

	}

	/// <summary>
	/// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
	/// hits, like if you're going through layers or ricocet'ing or something.
	/// </summary>
	public virtual IEnumerable<TraceResult> TraceBullet( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		var tr = Trace.Ray( start, end )
				.UseHitboxes()
				.Ignore( Owner )
				.Ignore( this )
				.WithoutTags( "gun" )
				.Size( radius )
				.Run();

		if ( tr.Hit )
			yield return tr;
	}

	/// <summary>
	/// Create the viewmodel. You can override this in your base classes if you want
	/// to create a certain viewmodel entity.
	/// </summary>
	public virtual void CreateViewModel()
	{
		Game.AssertClient();

		if ( string.IsNullOrEmpty( ViewModelPath ) )
			return;

		ViewModelEntity = new ViewModel
		{
			Position = Position,
			Owner = Owner,
			EnableViewmodelRendering = true,
		};

		ViewModelEntity.SetModel( ViewModelPath );
		ViewModelEntity.SetAnimParameter( "deploy", true );
	}

	/// <summary>
	/// We're done with the viewmodel - delete it
	/// </summary>
	public virtual void DestroyViewModel()
	{
		ViewModelEntity?.Delete();
		ViewModelEntity = null;
	}

	public virtual void SimulateAnimator( CitizenAnimationHelper anim )
	{
		anim.HoldType = CitizenAnimationHelper.HoldTypes.Pistol;
		anim.Handedness = CitizenAnimationHelper.Hand.Both;
		anim.AimBodyWeight = 1.0f;
	}

	[ClientRpc]
	protected virtual void ShootEffects()
	{
		Game.AssertClient();

		Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );

		if ( IsLocalPawn )
		{
			//_ = new Sandbox.ScreenShake.Perlin();
		}

		ViewModelEntity?.SetAnimParameter( "fire", true );
		//CrosshairPanel?.CreateEvent( "fire" );
	}

	/// <summary>
	/// Shoot a single bullet
	/// </summary>
	public virtual void ShootBullet( Vector3 pos, Vector3 dir, float spread, float force, float damage, float bulletSize )
	{

		var forward = dir;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		//
		// ShootBullet is coded in a way where we can have bullets pass through shit
		// or bounce off shit, in which case it'll return multiple results
		//
		foreach ( var tr in TraceBullet( pos, pos + forward * 5000, bulletSize ) )
		{
			tr.Surface.DoBulletImpact( tr ); //TODO: would be nice if this didnt happen on friendlies?

			if ( !Game.IsServer ) continue;
			if ( !tr.Entity.IsValid() ) continue;

			//
			// We turn predictiuon off for this, so any exploding effects don't get culled etc
			//
			using ( Prediction.Off() )
			{
				var damageInfo = DamageInfo.FromBullet( tr.EndPosition, forward * 100 * force, damage )
					.UsingTraceResult( tr )
					.WithAttacker( Owner )
					.WithWeapon( this );

				tr.Entity.TakeDamage( damageInfo );
			}
		}
	}

	/// <summary>
	/// Shoot a single bullet from owners view point
	/// </summary>
	public virtual void ShootBullet( float spread, float force, float damage, float bulletSize )
	{
		ShootBullet( Owner.AimRay.Position, Owner.AimRay.Forward, spread, force, damage, bulletSize );
	}

	/// <summary>
	/// Shoot a multiple bullets from owners view point
	/// </summary>
	public virtual void ShootBullets( int numBullets, float spread, float force, float damage, float bulletSize )
	{
		var pos = Owner.AimRay.Position;
		var dir = Owner.AimRay.Forward;

		for ( int i = 0; i < numBullets; i++ )
		{
			ShootBullet( pos, dir, spread, force / numBullets, damage, bulletSize );
		}
	}
}

