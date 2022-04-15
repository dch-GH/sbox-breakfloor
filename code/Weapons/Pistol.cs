using Sandbox;

namespace Breakfloor.Weapons
{
	[Library( "weapon_pistol", Title = "Pistol", Spawnable = true )]
	partial class Pistol : BreakfloorWeapon
	{
		public override string ViewModelPath => "weapons/rust_pistol/v_rust_pistol.vmdl";

		public override float PrimaryRate => 15.0f;
		public override float SecondaryRate => 1.0f;
		public override float ReloadTime => 2.8f;

		public TimeSince TimeSinceDischarge { get; set; }

		public override string GetKilledByText()
		{
			var options = new string[5] { "smoked", "popped", "poked a hole in", "iced", "spun the block on" };
			return Rand.FromArray<string>( options );
		}

		public override void Spawn()
		{
			base.Spawn();
			SetModel( "weapons/rust_pistol/rust_pistol.vmdl" );
		}

		public override bool CanPrimaryAttack()
		{
			return base.CanPrimaryAttack() && Input.Pressed( InputButton.Attack1 );
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

			ShootEffects();
			PlaySound( "rust_pistol.shoot" ).SetVolume(0.45f);
			ShootBullet( 0.05f, 1.5f, 9.0f, 3.0f );
			base.AttackPrimary();
		}

		public override void SimulateAnimator( PawnAnimator anim )
		{
			anim.SetAnimParameter( "holdtype", 1 );
			anim.SetAnimParameter( "aim_body_weight", 1.0f );
			anim.SetAnimParameter( "holdtype_handedness", 0 );
		}
	}

}
