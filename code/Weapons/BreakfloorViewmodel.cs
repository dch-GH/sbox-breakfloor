using Sandbox;
using System;

public class BreakfloorViewmodel : BaseViewModel
{
	protected float SwingInfluence => 0.04f;
	protected float ReturnSpeed => 4.0f;
	protected float MaxOffsetLength => 6.0f;
	protected float BobCycleTime => 6;
	protected Vector3 BobDirection => new Vector3( 0.0f, 1.0f, 0.5f );

	private Vector3 swingOffset;
	private float lastPitch;
	private float lastYaw;
	private float bobAnim;

	private bool activated = false;

	public Vector3 Offset = new Vector3( 2.03f, 3.18f, -1.48f );
	public Vector3 ImpulseForce = Vector3.Zero;

	public bool EnableSwingAndBob = true;

	public float YawInertia { get; private set; }
	public float PitchInertia { get; private set; }

	public override void PostCameraSetup( ref CameraSetup camSetup )
	{
		if ( !Local.Pawn.IsValid() )
			return;

		if ( !activated )
		{
			lastPitch = camSetup.Rotation.Pitch();
			lastYaw = camSetup.Rotation.Yaw();

			YawInertia = 0;
			PitchInertia = 0;

			activated = true;
		}

		Position = camSetup.Position + (camSetup.Rotation.Right * Offset.x) + (camSetup.Rotation.Backward * Offset.y) + (camSetup.Rotation.Up * Offset.z);
		Rotation = camSetup.Rotation;

		var cameraBoneIndex = GetBoneIndex( "camera" );
		if ( cameraBoneIndex != -1 )
		{
			camSetup.Rotation *= (Rotation.Inverse * GetBoneTransform( cameraBoneIndex ).Rotation);
		}

		var newPitch = Rotation.Pitch();
		var newYaw = Rotation.Yaw();

		PitchInertia = Angles.NormalizeAngle( newPitch - lastPitch );
		YawInertia = Angles.NormalizeAngle( lastYaw - newYaw );

		if ( EnableSwingAndBob )
		{
			var playerVelocity = Local.Pawn.Velocity;
			if ( Local.Pawn is Player player )
			{
				var controller = player.GetActiveController();
				if ( controller != null && controller.HasTag( "noclip" ) )
				{
					playerVelocity = Vector3.Zero;
				}
			}

			var verticalDelta = playerVelocity.z * Time.Delta;
			var viewDown = Rotation.FromPitch( newPitch ).Up * -1.0f;
			verticalDelta *= (1.0f - System.MathF.Abs( viewDown.Cross( Vector3.Down ).y ));
			var pitchDelta = PitchInertia - verticalDelta * 1;
			var yawDelta = YawInertia;

			var offset = CalcSwingOffset( pitchDelta, yawDelta );
			offset += CalcBobbingOffset( playerVelocity );

			Position += Rotation * offset;
		}
		else
		{
			SetAnimParameter( "aim_yaw_inertia", YawInertia );
			SetAnimParameter( "aim_pitch_inertia", PitchInertia );
		}

		Position = Vector3.Lerp( Position, Position + ImpulseForce, Time.Delta * 11f );

		ImpulseForce.x = MathX.Approach( ImpulseForce.x, 0, Time.Delta * 38f );
		ImpulseForce.y = MathX.Approach( ImpulseForce.y, 0, Time.Delta * 38f );
		ImpulseForce.z = MathX.Approach( ImpulseForce.z, 0, Time.Delta * 38f );

		lastPitch = newPitch;
		lastYaw = newYaw;
	}

	protected Vector3 CalcSwingOffset( float pitchDelta, float yawDelta )
	{
		Vector3 swingVelocity = new Vector3( 0, MathX.Clamp( yawDelta, -2, 2 ), pitchDelta );

		swingOffset -= swingOffset * ReturnSpeed * Time.Delta;
		swingOffset += (swingVelocity * SwingInfluence);

		if ( swingOffset.Length > MaxOffsetLength )
		{
			swingOffset = swingOffset.Normal * MaxOffsetLength;
		}

		return swingOffset;
	}

	protected Vector3 CalcBobbingOffset( Vector3 velocity )
	{
		bobAnim += Time.Delta * BobCycleTime;

		var twoPI = System.MathF.PI * 2.0f;

		if ( bobAnim > twoPI )
		{
			bobAnim -= twoPI;
		}

		var speed = new Vector2( velocity.x, velocity.y ).Length;
		speed = speed > 10.0 ? speed : 0.0f;
		var offset = BobDirection * (speed * 0.005f) * System.MathF.Cos( bobAnim );
		offset = offset.WithZ( -System.MathF.Abs( offset.z ) );

		return offset;
	}
}
