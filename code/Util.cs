using Sandbox;

namespace Breakfloor;

public static class Extensions
{
	public static bool SameTeam( this BreakfloorPlayer self, BreakfloorPlayer other )
	{
		if ( self is null || other is null ) return false;

		return self.Team == other.Team ||
			(self.Team == Team.FFA && other.Team == Team.FFA);
	}
	public static Team ToTeam( this int i )
	{
		return (Team)i;
	}

	public static bool IsTeam( this int i, Team e )
	{
		return i == (int)e;
	}

	public static Vector3 Normalize( in Vector3 self )
	{
		var x = System.MathF.Pow( self.x, 2 );
		var y = System.MathF.Pow( self.y, 2 );
		var z = System.MathF.Pow( self.z, 2 );

		float length = System.MathF.Sqrt( x + y + z );
		return new Vector3( self.x / length, self.y / length, self.z / length );
	}
}

public static class TraceResultExtensions
{
	public static void DoSurfaceMelee( this TraceResult tr )
	{
		var self = tr.Surface;
		if ( self == null )
			return;

		//
		// Make an impact sound
		//
		var sound = self.Sounds.Bullet;
		var surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( sound ) && surf != null )
		{
			sound = surf.Sounds.Bullet;
			surf = surf.GetBaseSurface();
		}

		if ( !string.IsNullOrWhiteSpace( sound ) )
		{
			Sound.FromWorld( sound, tr.EndPosition );
		}

		//
		// Make particle effect
		//
		string particleName = Rand.FromArray( self.ImpactEffects.Bullet );
		if ( string.IsNullOrWhiteSpace( particleName ) ) particleName = Rand.FromArray( self.ImpactEffects.Regular );

		surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( particleName ) && surf != null )
		{
			particleName = Rand.FromArray( surf.ImpactEffects.Bullet );
			if ( string.IsNullOrWhiteSpace( particleName ) ) particleName = Rand.FromArray( surf.ImpactEffects.Regular );

			surf = surf.GetBaseSurface();
		}

		if ( !string.IsNullOrWhiteSpace( particleName ) )
		{
			var ps = Particles.Create( particleName, tr.EndPosition );
			ps.SetForward( 0, tr.Normal );
		}
	}
}


