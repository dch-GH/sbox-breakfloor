using Sandbox;
using Breakfloor.Events;


namespace Breakfloor;

partial class Player
{
	[ClientInput] public Vector3 InputDirection { get; protected set; }
	[ClientInput] public Entity ActiveChildInput { get; set; }
	[ClientInput] public Angles ViewAngles { get; set; }

	public override void ClientSpawn()
	{
		FlashlightEntity = new SpotLightEntity
		{
			Enabled = false,
			DynamicShadows = true,
			Range = 3200f,
			Falloff = 0.3f,
			LinearAttenuation = 0.3f,
			Brightness = 8f,
			Color = Color.FromBytes( 200, 200, 200, 230 ),
			InnerConeAngle = 9,
			OuterConeAngle = 32,
			FogStrength = 1.0f,
			Owner = this,
			LightCookie = Texture.Load( "materials/effects/lightcookie.vtex" ),
			EnableViewmodelRendering = true
		};
	}

	public override void FrameSimulate( IClient cl )
	{
		base.FrameSimulate( cl );

		Controller?.FrameSimulate();

		// Place camera
		Camera.Position = EyePosition;
		Camera.Rotation = ViewAngles.ToRotation();
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Game.Preferences.FieldOfView );
		Camera.FirstPersonViewer = this;

		//Update the flashlight position on the client in framesim
		//so the movement is nice and smooth.
		FlashlightFrameSimulate();
	}

	public override void BuildInput()
	{
		if ( shouldOrientView )
		{
			ViewAngles = spawnViewAngles;
			shouldOrientView = false;
			return;
		}

		OriginalViewAngles = ViewAngles;
		InputDirection = Input.AnalogMove;

		if ( Input.StopProcessing )
			return;

		var look = Input.AnalogLook;

		if ( ViewAngles.pitch > 90f || ViewAngles.pitch < -90f )
		{
			look = look.WithYaw( look.yaw * -1f );
		}

		var viewAngles = ViewAngles;
		viewAngles += look;
		viewAngles.pitch = viewAngles.pitch.Clamp( -89f, 89f );
		viewAngles.roll = 0f;
		ViewAngles = viewAngles.Normal;
	}

	private async void HitMarker( float pitch )
	{
		await GameTask.Delay( 60 );
		Sound.FromScreen( "ui.bf_hitmarker" ).SetPitch( 1 + pitch * 1 );
	}

	[ClientRpc]
	private void RespawnClient()
	{
		EnableShadowInFirstPerson = true;
		EnableDrawing = false;
	}

	/// <summary>
	/// See the overridden BuildInput method.
	/// </summary>
	/// <param name="ang"></param>
	[ClientRpc]
	private void OrientAnglesToSpawnClient( Angles ang )
	{
		shouldOrientView = true;
		spawnViewAngles = ang;
	}

	[ClientRpc]
	public void TookDamage( Vector3 pos )
	{
		if ( IsLocalPawn )
		{
			Event.Run( BFEVents.LocalPlayerHurt );
		}
	}

	[ClientRpc]
	public void DidDamage( Vector3 pos, float amount, float healthinv )
	{
		HitMarker( healthinv );
	}

	[ClientRpc]
	public void PlaySoundClient( string snd )
	{
		PlaySound( snd );
	}
}
