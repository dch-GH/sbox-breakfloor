using Sandbox;

namespace Breakfloor;

partial class BreakfloorPlayer
{
	public PlayerCorpse Ragdoll { get; set; }

	[ClientRpc]
	private void BecomeRagdollOnClient( Vector3 force, int forceBone )
	{
		var ragdoll = new PlayerCorpse
		{
			Position = Position,
			Rotation = Rotation
		};

		ragdoll.CopyFrom( this );
		ragdoll.ApplyForceToBone( force, forceBone );
		ragdoll.Player = this;

		Ragdoll = ragdoll;
	}
}

public class PlayerCorpse : ModelEntity
{
	public BreakfloorPlayer Player { get; set; }

	private TimeSince TimeSinceSpawned { get; set; }

	public PlayerCorpse()
	{
		UsePhysicsCollision = true;
		TimeSinceSpawned = 0f;
		PhysicsEnabled = true;
	}

	public void CopyFrom( Player player )
	{
		RenderColor = player.RenderColor;

		SetModel( player.GetModelName() );
		TakeDecalsFrom( player );

		// We have to use `this` to refer to the extension methods.
		this.CopyBonesFrom( player );
		this.SetRagdollVelocityFrom( player );

		foreach ( var child in Children )
		{
			if ( !child.Tags.Has( "clothes" ) ) continue;
			if ( child is not ModelEntity e ) continue;

			var model = e.GetModelName();

			var clothing = new ModelEntity();
			clothing.SetModel( model );
			clothing.SetParent( e, true );
			clothing.RenderColor = e.RenderColor;
			clothing.CopyBodyGroups( e );
			clothing.CopyMaterialGroup( e );
		}
	}

	public void ApplyForceToBone( Vector3 force, int forceBone )
	{
		PhysicsGroup.AddVelocity( force );

		if ( forceBone >= 0 )
		{
			var body = GetBonePhysicsBody( forceBone );

			if ( body != null )
				body.ApplyForce( force * 1000 );
			else
				PhysicsGroup.AddVelocity( force );
		}
	}

	public override void Spawn()
	{
		Tags.Add( "corpse" );

		base.Spawn();
	}

	[Event.Tick.Client]
	protected virtual void ClientTick()
	{
		if ( IsClientOnly && TimeSinceSpawned > 10f )
		{
			Delete();
		}
	}
}
