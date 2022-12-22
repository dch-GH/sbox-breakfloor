using Sandbox;
using System;
using System.Linq;
using Breakfloor.Weapons;

namespace Breakfloor;

partial class Player
{
	public Angles OriginalViewAngles { get; private set; }

	// These are for resetting/setting the player view angles
	// to that of their spawnpoint direction, so the player faces the correct direction on respawn.
	// We need to use BuildInput because Input.Rotation is carried over
	// between disconnects/gamemode restarts and will get applied instantly in Simualate. Needs to be overridden manually. :)
	private bool shouldOrientView;
	private Angles spawnViewAngles;

	[Net, Predicted]
	public Vector3 EyeLocalPosition { get; set; }

	public Vector3 EyePosition
	{
		get => Transform.PointToWorld( EyeLocalPosition );
		set => EyeLocalPosition = Transform.PointToLocal( value );
	}

	/// <summary>
	/// Rotation of the entity's "eyes", i.e. rotation for the camera when this entity is used as the view entity. In local to the entity coordinates.
	/// </summary>
	[Net, Predicted]
	public Rotation EyeLocalRotation { get; set; }

	public Rotation EyeRotation
	{
		get => Transform.RotationToWorld( EyeLocalRotation );
		set => EyeLocalRotation = Transform.RotationToLocal( value );
	}

	/// <summary>
	/// Override the aim ray to use the player's eye position and rotation.
	/// </summary>
	public override Ray AimRay => new Ray( EyePosition, EyeRotation.Forward );
}
