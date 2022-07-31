using Breakfloor.UI;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Breakfloor
{
	// i know it should be an enum, BUT, because of the way clientdata works its just better to use static ints...
	// saves you from doing a lot of casting and copy pasting. this is good enough imo and is effectively an enum.
	public static class Teams
	{
		public const int None = -1;
		public const int A = 0;
		public const int B = 1;
	}

	public partial class BreakfloorGame : Game
	{
		public const string TeamDataKey = "team";

		public static List<Client> TeamA = new List<Client>();
		public static List<Client> TeamB = new List<Client>();

		public static Color GetTeamColor( int index )
		{
			switch ( index )
			{
				case Teams.A:
					return Color.FromBytes( 237, 36, 79 );
				case Teams.B:
					return Color.FromBytes( 26, 154, 240 );
				default:
					return Color.White;
			}
		}

		public void JoinTeam( Client p, int index )
		{
			switch ( index )
			{
				case Teams.A:
					if ( !TeamA.Contains( p.Client ) )
					{
						TeamA.Add( p );
						p.SetValue( "team", Teams.A );
						Log.Info( $"Client:{p} joined team A." );
						BFChatbox.AddInformation( To.Everyone, $"{p.Name} joined team RED.", $"avatar:{p.PlayerId}", isPlayerAdmin: false );
					}
					break;
				case Teams.B:
					if ( !TeamB.Contains( p.Client ) )
					{
						TeamB.Add( p );
						p.SetValue( "team", Teams.B );
						Log.Info( $"Client:{p} joined team B." );
						BFChatbox.AddInformation( To.Everyone, $"{p.Name} joined team BLUE.", $"avatar:{p.PlayerId}", isPlayerAdmin: false );
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
				JoinTeam( client, Teams.A );
			}
			else
			{
				JoinTeam( client, Teams.B );
			}
		}

		public static bool SameTeam( Client a, Client b )
		{
			if ( a == null || b == null ) return false;

			int aValue = a.GetValue<int>( "team" );
			int bValue = b.GetValue<int>( "team" );
			return aValue == bValue;
		}
	}
}
