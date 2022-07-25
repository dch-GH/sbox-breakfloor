using Sandbox;

namespace Breakfloor.Weapons
{
	public partial class BreakfloorWeapon : WeaponBase, IUse
	{
		public virtual float ReloadTime => 3.0f;

		public PickupTrigger PickupTrigger { get; protected set; }

		[Net, Predicted]
		public TimeSince TimeSinceReload { get; set; }

		[Net, Predicted]
		public bool IsReloading { get; set; }

		[Net, Predicted]
		public TimeSince TimeSinceDeployed { get; set; }

		public virtual string GetKilledByText() { return string.Empty; }

		public override void Spawn()
		{
			base.Spawn();

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

		public override void ActiveStart( Entity ent )
		{
			base.ActiveStart( ent );

			TimeSinceDeployed = 0;
		}

		public override void Simulate( Client owner )
		{
			if ( TimeSinceDeployed < 0.6f )
				return;

			if ( !IsReloading )
			{
				base.Simulate( owner );
			}

			if ( IsReloading && TimeSinceReload > ReloadTime )
			{
				OnReloadFinish();
			}
		}

		public override void AttackPrimary()
		{
			base.AttackPrimary();
			ClipAmmo--;
			if ( ClipAmmo - 1 < 0 )
			{
				ClipAmmo = 0;
			}
		}

		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && ClipAmmo > 0;
		}

		public override bool CanReload()
		{
			return base.CanReload() && ClipAmmo < MaxClip;
		}

		public override void Reload()
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

		public override void CreateViewModel()
		{
			Host.AssertClient();

			if ( string.IsNullOrEmpty( ViewModelPath ) )
				return;

			ViewModelEntity = new ViewModel
			{
				Position = Position,
				Owner = Owner,
				EnableViewmodelRendering = true
			};

			ViewModelEntity.SetModel( ViewModelPath );
			ViewModelEntity.SetAnimParameter( "deploy", true );
		}

		public bool OnUse( Entity user )
		{
			if ( Owner != null )
				return false;

			if ( !user.IsValid() )
				return false;

			user.StartTouch( this );

			return false;
		}

		public virtual bool IsUsable( Entity user )
		{
			var player = user as Player;
			if ( Owner != null ) return false;

			if ( player.Inventory is Inventory inventory )
			{
				return inventory.CanAdd( this );
			}

			return true;
		}

		[ClientRpc]
		protected virtual void ShootEffects()
		{
			Host.AssertClient();

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

				if ( !IsServer ) continue;
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
			ShootBullet( Owner.EyePosition, Owner.EyeRotation.Forward, spread, force, damage, bulletSize );
		}

		/// <summary>
		/// Shoot a multiple bullets from owners view point
		/// </summary>
		public virtual void ShootBullets( int numBullets, float spread, float force, float damage, float bulletSize )
		{
			var pos = Owner.EyePosition;
			var dir = Owner.EyeRotation.Forward;

			for ( int i = 0; i < numBullets; i++ )
			{
				ShootBullet( pos, dir, spread, force / numBullets, damage, bulletSize );
			}
		}
	}
}
