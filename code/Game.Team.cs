using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Breakfloor
{
	public enum Team : int
	{
		None = -1,
		A = 0,
		B = 1
	}

	public partial class BreakfloorGame : Game
	{

		public static List<Client> TeamA = new List<Client>();
		public static List<Client> TeamB = new List<Client>();

		public static Team GetMyTeam(Client client )
		{
			return TeamA.Contains( client ) ? Team.A : Team.B;
		}

		public static string GetMyTeamTag(Client client)
		{
			return TeamA.Contains( client ) ? "team_a" : "team_b";
		}

		public void JoinTeam( Client p, Team index )
		{
			switch ( index )
			{
				case Team.A:
					if ( !TeamA.Contains( p.Client ) )
					{
						TeamA.Add( p );
						p.SetValue( "team", (int)Team.A );
						Log.Info( $"Client:{p} joined team A." );
					}
					break;
				case Team.B:
					if ( !TeamB.Contains( p.Client ) )
					{
						TeamB.Add( p );
						p.SetValue( "team", (int)Team.B );
						Log.Info( $"Client:{p} joined team B." );
					}
					break;
			}
		}

		public void JoinRandomTeam( Client client )
		{
			Log.Info( $"Joining random team:{client}" );

			var randomValue = Rand.Int( 0, 2 );
			if ( randomValue == 1 )
			{
				JoinTeam( client, Team.A );
			}
			else
			{
				JoinTeam( client, Team.B );
			}
		}
	}
}
