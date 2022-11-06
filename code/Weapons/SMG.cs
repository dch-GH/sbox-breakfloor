using Sandbox;
using Sandbox.UI;
using System.Numerics;

namespace Breakfloor.Weapons
{
	[Library( "weapon_smg", Title = "SMG" )]
	partial class SMG : BreakfloorGun
	{
		public override string ViewModelPath => "models/mp5/fp_mp5.vmdl";

		public override float PrimaryRate => 14.0f;
		public override float SecondaryRate => 1.2f;
		public override float ReloadTime => 3.4f;
		public override int MaxClip => 30;

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

		public override string GetKilledByText( DamageFlags flags )
		{
			// I'm certain this will need a rewrite when FP makes DamageFlags strings
			// like Hitbox tags.
			if ( flags.HasFlag( DamageFlags.Bullet ) )
				return Rand.FromArray<string>( primaryOptions );
			else if ( flags.HasFlag( DamageFlags.Blunt ) )
				return Rand.FromArray<string>( secondaryOptions );
			else
				return base.GetKilledByText( flags );
		}

		public override void Spawn()
		{
			base.Spawn();
			// Set world model.
			SetModel( "models/mp5/w_mp5.vmdl" );
		}

		public override void Simulate( Client owner )
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
			var spread = (Owner as Breakfloor.BreakfloorPlayer).Controller.HasTag( "ducked" )
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
			var pos = Owner.EyePosition;
			var forward = pos + (Owner.EyeRotation.Forward * gunBashRange);
			var tr = Trace.Ray( pos, forward )
					.UseHitboxes()
					.Ignore( Owner )
					.Ignore( this )
					.WithoutTags( "gun" )
					.Size( 1 )
					.Run();

			if ( ViewModelEntity.IsValid()
				&& ViewModelEntity is BreakfloorViewmodel vm )
			{
				vm.ImpulseForce = Owner.EyeRotation.Forward * 16f;
			}

			PlaySound( "gun_bash" );

			if ( !tr.Hit ) return;
			tr.DoSurfaceMelee();

			if ( !IsServer ) return;
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
					.WithFlag( DamageFlags.Blunt )
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

		[ClientRpc]
		protected override void ShootEffects()
		{
			Host.AssertClient();

			Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
			Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );
			ViewModelEntity?.SetAnimParameter( "fire", true );
		}

		public override void SimulateAnimator( PawnAnimator anim )
		{
			anim.SetAnimParameter( "holdtype", 3 ); // TODO this is shit
			anim.SetAnimParameter( "aim_body_weight", 1.0f );
		}
	}
}
