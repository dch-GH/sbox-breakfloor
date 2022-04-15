using Sandbox;

namespace Breakfloor.Weapons
{
	[Library( "weapon_smg", Title = "SMG", Spawnable = true )]
	partial class SMG : BreakfloorWeapon
	{
		public override string ViewModelPath => "weapons/rust_smg/v_rust_smg.vmdl";

		public override float PrimaryRate => 9.0f;
		public override float SecondaryRate => 1.0f;
		public override float ReloadTime => 5.0f;
		public override int MaxClip => 24;

		public override string GetKilledByText()
		{
			var options = new string[5] { "sprayed", "dusted", "swiss cheese'd", "shot up", "gunned down" };
			return Rand.FromArray<string>( options );
		}

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "weapons/rust_smg/rust_smg.vmdl" );
		}

		public override void Reload()
		{
			base.Reload();
		}

		public override void AttackPrimary()
		{
			TimeSincePrimaryAttack = 0;
			TimeSinceSecondaryAttack = 0;

			(Owner as AnimEntity)?.SetAnimParameter( "b_attack", true );

			//
			// Tell the clients to play the shoot effects
			//
			ShootEffects();
			PlaySound( "rust_smg.shoot").SetVolume(0.45f);

			// Shoot the bullets
			var spread = (Owner as Breakfloor.BreakfloorPlayer).Controller.HasTag( "ducked" )
				? 0.09f
				: 0.12f; //yikes lol
			ShootBullet( spread, 1.5f, 9.0f, 3.0f );
			base.AttackPrimary();
		}

		public override void AttackSecondary()
		{
			// Grenade lob
		}

		[ClientRpc]
		protected override void ShootEffects()
		{
			Host.AssertClient();

			Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
			Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );

			if ( Owner == Local.Pawn )
			{
				new Sandbox.ScreenShake.Perlin( 0.5f, 4.0f, 1.0f, 0.5f );
			}

			ViewModelEntity?.SetAnimParameter( "fire", true );
			CrosshairPanel?.CreateEvent( "fire" );
		}

		public override void SimulateAnimator( PawnAnimator anim )
		{
			anim.SetAnimParameter( "holdtype", 2 ); // TODO this is shit
			anim.SetAnimParameter( "aim_body_weight", 1.0f );
		}
	}
}
