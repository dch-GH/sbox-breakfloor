using Sandbox;
using Sandbox.UI;
using System.Numerics;

namespace Breakfloor.Weapons
{
	[Library( "weapon_smg", Title = "SMG" )]
	public partial class SMG : Gun
	{
		public override string ViewModelPath => "models/mp5/fp_mp5.vmdl_c";

		public override float PrimaryRate => 14.0f;
		public override float SecondaryRate => 1.2f;
		public override float ReloadTime => 3;
		public override int MaxClip => 31;

		private float gunBashRange = 52f;
		private float gunBashDamage = 9f;

		private readonly string[] primaryOptions =
		{
			"sprayed", "dusted", "swiss cheese'd", "dakka'd", "gunned down",
			"smoked", "popped", "poked a hole in", "iced", "spun the block on"
		};

		private readonly string[] secondaryOptions = new string[]
		{
			"smacked", "bludgeoned", "beat down"
		};

		public override string GetKilledByText( DamageInfo dmg )
		{
			if ( dmg.HasTag( DamageTags.Bullet ) )
				return Game.Random.FromArray<string>( primaryOptions );
			else if ( dmg.HasTag( DamageTags.Blunt ) )
				return Game.Random.FromArray<string>( secondaryOptions );
			else
				return base.GetKilledByText( dmg );
		}

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "models/mp5/w_mp5.vmdl" );
		}

		public override void Simulate( IClient owner )
		{
			base.Simulate( owner );
		}

		public override void Reload()
		{
			base.Reload();
		}

		// We do this since we have overriden the Base Simulate behavior
		// which originally bailed out mid Simulate if IsReloading was true.
		// We want to be able to SecondaryAttack even while reloading.
		// So, instead we just remove that and allow per weapon IsReloading check per attack function.
		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && !IsReloading;
		}

		public override void AttackPrimary()
		{
			TimeSincePrimaryAttack = 0;
			TimeSinceSecondaryAttack = 0;

			(Owner as AnimatedEntity)?.SetAnimParameter( "b_attack", true );

			//
			// Tell the clients to play the shoot effects
			//
			ShootEffects();
			PlaySound( "ar.shoot" ).SetVolume( 0.40f );

			const float damage = 8;

			// Shoot the bullets
			var spread = (Owner as Player).Controller.HasTag( "ducked" )
				? 0.09f
				: 0.12f;

			ShootBullet( spread, 1.5f, damage, 3.0f );
			base.AttackPrimary();
		}

		public override void AttackSecondary()
		{
			TimeSincePrimaryAttack = 0;
			TimeSinceSecondaryAttack = 0;

			// Gun bash
			var pos = Owner.AimRay.Position;
			var forward = pos + (Owner.AimRay.Forward * gunBashRange);
			var tr = Trace.Ray( pos, forward )
					.UseHitboxes()
					.Ignore( Owner )
					.Ignore( this )
					.WithoutTags( "gun" )
					.Size( 1 )
					.Run();

			PlaySound( "gun_bash" );

			if ( !tr.Hit ) return;
			tr.DoSurfaceMelee();

			if ( !Game.IsServer ) return;
			if ( !tr.Entity.IsValid() ) return;

			var force = 2f;

			//
			// Turn off prediction, so any exploding effects don't get culled
			//
			using ( Prediction.Off() )
			{
				var damageInfo = DamageInfo.Generic( gunBashDamage )
					.WithPosition( tr.EndPosition )
					.WithForce( forward * force )
					.WithWeapon( this )
					.WithTag( DamageTags.Blunt )
					.UsingTraceResult( tr )
					.WithAttacker( this );

				tr.Entity.TakeDamage( damageInfo );
			}

			base.AttackSecondary();
		}

		public override bool CanSecondaryAttack()
		{
			if ( !Owner.IsValid() || !Input.Down( InputButton.SecondaryAttack ) ) return false;

			var rate = SecondaryRate;
			if ( rate <= 0 ) return true;

			return TimeSinceSecondaryAttack > (1 / rate);
		}

		public override void SimulateAnimator( CitizenAnimationHelper anim )
		{
			anim.HoldType = CitizenAnimationHelper.HoldTypes.Rifle;
			anim.AimBodyWeight = 1.0f;
		}

		public override void CreateViewModel()
		{
			base.CreateViewModel();
			(ViewModelEntity as ViewModel).PosOffset = new Vector3( 8.88f, -0.67f, -0.11f );
		}

		[ClientRpc]
		protected override void ShootEffects()
		{
			Game.AssertClient();

			Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
			//Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
			ViewModelEntity?.SetAnimParameter( "fire", true );
		}
	}
}
