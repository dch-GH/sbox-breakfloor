using Breakfloor.HammerEnts;
using Sandbox;
using System;
using System.Linq;
using static Breakfloor.BreakfloorGame;

partial class BreakfloorPlayer : Player
{
	TimeSince timeSinceDropped;
	DamageInfo LastDamage;
	public bool SupressPickupNotices { get; private set; }

	public BreakfloorPlayer()
	{
		Inventory = new DmInventory( this );
	}

	public override void Spawn()
	{
		base.Spawn();
	}

	public override void Respawn()
	{
		SetModel( "models/citizen/citizen.vmdl" );

		Controller = new WalkController();
		(Controller as WalkController).Gravity = 750.0f; //gotta get that jump height above the blocks.
		Animator = new StandardPlayerAnimator();

		CameraMode = new FirstPersonCamera();

		EnableAllCollisions = true;
		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		ClearAmmo();
		Clothing.DressEntity( this );

		SupressPickupNotices = true;

		Inventory.Add( new Pistol(), true );
		Inventory.Add( new Shotgun() );
		Inventory.Add( new SMG() );
		Inventory.Add( new Crossbow() );

		GiveAmmo( AmmoType.Pistol, 100 );
		GiveAmmo( AmmoType.Buckshot, 8 );
		GiveAmmo( AmmoType.Crossbow, 4 );

		SupressPickupNotices = false;
		Health = 100;


		LifeState = LifeState.Alive;
		Health = 100;
		Velocity = Vector3.Zero;
		WaterLevel = 0;

		CreateHull();

		var teamIndex = GetMyTeam( Client );
		Log.Info( $"Player:{Client} has teamIndex: {teamIndex}." );
		var spawn = Entity.All.OfType<BreakfloorSpawnPoint>()
			.Where(x => x.Index == teamIndex)
			.OrderBy(x => Guid.NewGuid())
			.FirstOrDefault();

		Log.Info($"Spawning player {Client} at {spawn} because it has index {spawn.Index}");

		Transform = spawn.Transform;
		
		ResetInterpolation();
	}

	public override void OnKilled()
	{
		base.OnKilled();

		Inventory.DropActive();
		Inventory.DeleteContents();

		BecomeRagdollOnClient( LastDamage.Force, GetHitboxBone( LastDamage.HitboxIndex ) );

		Controller = null;

		CameraMode = new SpectateRagdollCamera();

		EnableAllCollisions = false;
		EnableDrawing = false;

		foreach ( var child in Children.OfType<ModelEntity>() )
		{
			child.EnableDrawing = false;
		}
	}


	public override void Simulate( Client cl )
	{
		base.Simulate( cl );

		//
		// Input requested a weapon switch
		//
		if ( Input.ActiveChild != null )
		{
			ActiveChild = Input.ActiveChild;
		}

		if ( LifeState != LifeState.Alive )
			return;

		TickPlayerUse();

		if ( Input.Pressed( InputButton.Drop ) )
		{
			var dropped = Inventory.DropActive();
			if ( dropped != null )
			{
				if ( dropped.PhysicsGroup != null )
				{
					dropped.PhysicsGroup.Velocity = Velocity + (EyeRotation.Forward + EyeRotation.Up) * 300;
				}

				timeSinceDropped = 0;
				SwitchToBestWeapon();
			}
		}

		SimulateActiveChild( cl, ActiveChild );

		//
		// If the current weapon is out of ammo and we last fired it over half a second ago
		// lets try to switch to a better wepaon
		//
		if ( ActiveChild is BaseDmWeapon weapon && !weapon.IsUsable() && weapon.TimeSincePrimaryAttack > 0.5f && weapon.TimeSinceSecondaryAttack > 0.5f )
		{
			SwitchToBestWeapon();
		}
	}

	public void SwitchToBestWeapon()
	{
		var best = Children.Select( x => x as BaseDmWeapon )
			.Where( x => x.IsValid() && x.IsUsable() )
			.OrderByDescending( x => x.BucketWeight )
			.FirstOrDefault();

		if ( best == null ) return;

		ActiveChild = best;
	}

	public override void BuildInput( InputBuilder input )
	{
		if ( input.Pressed( InputButton.Slot1 ) ) SetActiveSlot( input, Inventory, 0 );
		if ( input.Pressed( InputButton.Slot2 ) ) SetActiveSlot( input, Inventory, 1 );
		if ( input.Pressed( InputButton.Slot3 ) ) SetActiveSlot( input, Inventory, 2 );
		if ( input.Pressed( InputButton.Slot4 ) ) SetActiveSlot( input, Inventory, 3 );
		if ( input.Pressed( InputButton.Slot5 ) ) SetActiveSlot( input, Inventory, 4 );
		if ( input.Pressed( InputButton.Slot6 ) ) SetActiveSlot( input, Inventory, 5 );
		if ( input.Pressed( InputButton.Slot7 ) ) SetActiveSlot( input, Inventory, 6 );
		if ( input.Pressed( InputButton.Slot8 ) ) SetActiveSlot( input, Inventory, 7 );
		if ( input.Pressed( InputButton.Slot9 ) ) SetActiveSlot( input, Inventory, 8 );

		//if ( input.MouseWheel != 0 ) SwitchActiveSlot( input, inventory, -input.MouseWheel );
	}

	private static void SetActiveSlot( InputBuilder input, IBaseInventory inventory, int i )
	{
		var player = Local.Pawn as Player;

		if ( player == null )
			return;

		var ent = inventory.GetSlot( i );
		if ( player.ActiveChild == ent )
			return;

		if ( ent == null )
			return;

		input.ActiveChild = ent;
	}

	public override void StartTouch( Entity other )
	{
		if ( timeSinceDropped < 1 ) return;

		base.StartTouch( other );
	}

	public override void TakeDamage( DamageInfo info )
	{
		LastDamage = info;

		// hack - hitbox 0 is head
		// we should be able to get this from somewhere
		if ( info.HitboxIndex == 0 )
		{
			info.Damage *= 2.0f;
		}

		base.TakeDamage( info );

		if ( info.Attacker is BreakfloorPlayer attacker && attacker != this )
		{
			// Note - sending this only to the attacker!
			attacker.DidDamage( To.Single( attacker ), info.Position, info.Damage, Health.LerpInverse( 100, 0 ) );

			TookDamage( To.Single( this ), info.Weapon.IsValid() ? info.Weapon.Position : info.Attacker.Position );
		}
	}

	[ClientRpc]
	public void DidDamage( Vector3 pos, float amount, float healthinv )
	{
		Sound.FromScreen( "dm.ui_attacker" )
			.SetPitch( 1 + healthinv * 1 );

		//HitIndicator.Current?.OnHit( pos, amount );
	}

	[ClientRpc]
	public void TookDamage( Vector3 pos )
	{
		//DebugOverlay.Sphere( pos, 5.0f, Color.Red, false, 50.0f );

		//DamageIndicator.Current?.OnHit( pos );
	}
}
