using System;
using System.Collections.Generic;
using Sandbox;

namespace Breakfloor;

public static class Extensions
{
	public static bool SameTeam( this Player self, Player other )
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
		string particleName = Game.Random.FromArray( self.ImpactEffects.Bullet );
		if ( string.IsNullOrWhiteSpace( particleName ) ) particleName = Game.Random.FromArray( self.ImpactEffects.Regular );

		surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( particleName ) && surf != null )
		{
			particleName = Game.Random.FromArray( surf.ImpactEffects.Bullet );
			if ( string.IsNullOrWhiteSpace( particleName ) ) particleName = Game.Random.FromArray( surf.ImpactEffects.Regular );

			surf = surf.GetBaseSurface();
		}

		if ( !string.IsNullOrWhiteSpace( particleName ) )
		{
			var ps = Particles.Create( particleName, tr.EndPosition );
			ps.SetForward( 0, tr.Normal );
		}
	}
}

public static class IEnumerableExtensions
{
	public static int HashCombine<T>( this IEnumerable<T> e, Func<T, decimal> selector )
	{
		var result = 0;

		foreach ( var el in e )
			result = HashCode.Combine( result, selector.Invoke( el ) );

		return result;
	}
}
