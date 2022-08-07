using Breakfloor.UI;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Breakfloor
{
	public enum Team
	{
		None = -1,
		FFA = 0,
		RED = 1,
		BLUE = 2,
		GREEN = 3,
		YELLOW = 4,
	}

	public enum TeamMode
	{
		FFA = 1,
		TwoOpposing = 2,
		ThreeWay = 3,
		FourWay = 4
	}

	public partial class BreakfloorGame : Game
	{
		public const string TeamDataKey = "team";

		public static List<Client> TeamFFA = new();
		public static List<Client> TeamRed = new();
		public static List<Client> TeamBlue = new();
		public static List<Client> TeamGreen = new();
		public static List<Client> TeamYellow = new();


		public static Color GetTeamColor( Team index )
		{
			switch ( index )
			{
				case Team.RED:
					return Color.FromBytes( 237, 36, 79 );
				case Team.BLUE:
					return Color.FromBytes( 26, 154, 240 );
				default:
					return Color.White;
			}
		}

		public void JoinTeam( Client p, Team index )
		{
			switch ( index )
			{
				case Team.FFA:
					if ( !TeamFFA.Contains( p ) )
					{
						TeamFFA.Add( p );
						Log.Info( $"Client:{p} joined the free-for-all." );
						BFChatbox.AddInformation( To.Everyone, $"{p.Name} joined the free-for-all.", $"avatar:{p.PlayerId}", isPlayerAdmin: false );
					}
					break;
				case Team.RED:
					if ( !TeamRed.Contains( p ) )
					{
						TeamRed.Add( p );
						Log.Info( $"Client:{p} joined team RED." );
						BFChatbox.AddInformation( To.Everyone, $"{p.Name} joined team RED.", $"avatar:{p.PlayerId}", isPlayerAdmin: false );
					}
					break;
				case Team.BLUE:
					if ( !TeamBlue.Contains( p ) )
					{
						TeamBlue.Add( p );
						Log.Info( $"Client:{p} joined team BlUE." );
						BFChatbox.AddInformation( To.Everyone, $"{p.Name} joined team BLUE.", $"avatar:{p.PlayerId}", isPlayerAdmin: false );
					}
					break;
				case Team.GREEN:
					if ( !TeamGreen.Contains( p ) )
					{
						TeamGreen.Add( p );
						Log.Info( $"Client:{p} joined team GREEN." );
						BFChatbox.AddInformation( To.Everyone, $"{p.Name} joined team GREEN.", $"avatar:{p.PlayerId}", isPlayerAdmin: false );
					}
					break;
				case Team.YELLOW:
					if ( !TeamYellow.Contains( p ) )
					{
						TeamYellow.Add( p );
						Log.Info( $"Client:{p} joined team YELLOW." );
						BFChatbox.AddInformation( To.Everyone, $"{p.Name} joined team YELLOW.", $"avatar:{p.PlayerId}", isPlayerAdmin: false );
					}
					break;
			}
		}
	}
}
