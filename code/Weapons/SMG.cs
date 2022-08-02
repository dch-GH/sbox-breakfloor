using Sandbox;

namespace Breakfloor.Weapons
{
	[Library( "weapon_smg", Title = "SMG" )]
	partial class SMG : BreakfloorWeapon
	{
		public override string ViewModelPath => "weapons/rust_smg/v_rust_smg.vmdl";

		public override float PrimaryRate => 12.0f;
		public override float SecondaryRate => 1.0f;
		public override float ReloadTime => 3.0f;
		public override int MaxClip => 35;

		public override string GetKilledByText()
		{
			var options = new string[5] { "sprayed", "dusted", "swiss cheese'd", "dakka'd", "gunned down" };
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
			// Grenade lob
		}

		//public override void Simulate( Client owner )
		//{
		//	if ( IsServer ) return;
		//	Log.Info( TimeSincePrimaryAttack );
		//	base.Simulate( owner );
		//}

		[ClientRpc]
		protected override void ShootEffects()//   CrosshairPanel?.CreateEvent( "fire" );
		{
			Host.AssertClient();

			Particles.Create( "particles/pistol_muzzleflash.vpcf", EffectEntity, "muzzle" );
			Particles.Create( "particles/pistol_ejectbrass.vpcf", EffectEntity, "ejection_point" );

			if ( Owner == Local.Pawn )
			{
				//new Sandbox.ScreenShake.Perlin( 0.5f, 4.0f, 1.0f, 0.5f );
			}

			ViewModelEntity?.SetAnimParameter( "fire", true );
			//CrosshairPanel?.CreateEvent( "fire" );
		}

		public override void SimulateAnimator( PawnAnimator anim )
		{
			anim.SetAnimParameter( "holdtype", 2 ); // TODO this is shit
			anim.SetAnimParameter( "aim_body_weight", 1.0f );
		}
	}
}
