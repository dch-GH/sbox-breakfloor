using Sandbox;
using Sandbox.UI;
using static Sandbox.Event;

namespace Breakfloor.UI;

partial class Killfeed : Panel
{
	public static Killfeed Current;

	public Killfeed()
	{
		if ( !Game.IsClient ) return;
		Current = this;
	}

	public Panel AddEntry( IClient killer, IClient victim, string method )
	{
		var e = Current.AddChild<KillfeedEntry>();

		e.AddClass( method );

		if ( killer != null && killer.Pawn is Player k )
		{
			e.Killer.Text = killer.Name;
			e.Killer.SetClass( "me", killer.Id == Game.LocalClient.SteamId);
			var colors = new Color[] { BreakfloorGame.GetTeamColor( k.Team ), Color.White };
			e.Killer.Style.FontColor = Color.Average( colors );
		}


		e.Method.Text = $"{method} ";

		if ( victim != null && victim.Pawn is Player v )
		{
			e.Victim.Text = victim.Name;
			e.Victim.SetClass( "me", victim.Id == Game.LocalClient.SteamId);
			var colors = new Color[] { BreakfloorGame.GetTeamColor( v.Team ), Color.White };
			e.Victim.Style.FontColor = Color.Average( colors );
		}

		if ( killer != null && victim != null )
		{
			Log.Info( $"{killer.Name} {method} {victim.Name}" );
		}

		return e;
	}

}
