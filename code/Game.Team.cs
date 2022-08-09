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
		public static Color GetTeamColor( Team index )
		{
			switch ( index )
			{
				case Team.RED:
					return Color.FromBytes( 237, 36, 79 );
				case Team.BLUE:
					return Color.FromBytes( 26, 154, 240 );
				case Team.GREEN:
					return Color.Green;
				case Team.YELLOW:
					return Color.Yellow;
				default:
					return Color.White;
			}
		}

		public static int GetTeamCount( Team team )
		{
			if ( Client.All.Count <= 1 ) return 0;

			int num = 0;
			foreach ( var c in Client.All )
			{
				var pawn = (BreakfloorPlayer)c.Pawn;
				if ( pawn.Team == team )
					num++;
			}

			return num;
		}

		public void JoinTeam( Client p, Team index )
		{
			var team = Enum.GetName<Team>( index );
			BFChatbox.AddInformation( To.Everyone, $"{p.Name} joined team {team}.", $"avatar:{p.PlayerId}", isPlayerAdmin: false );
			Log.Info( $"Client:{p} joined team {team}" );
		}

		public Team HandleTeamAssign( Client cl )
		{
			Team decidedTeam = Team.None;
			switch ( gameRules.TeamSetup )
			{
				case TeamMode.FFA:
					JoinTeam( cl, Team.FFA );
					decidedTeam = Team.FFA;
					break;
				case TeamMode.TwoOpposing:
					var teamDifference = GetTeamCount( Team.RED ) - GetTeamCount( Team.BLUE );
					if ( teamDifference < 0 )
					{
						JoinTeam( cl, Team.RED );
						decidedTeam = Team.RED;
					}
					else if ( teamDifference > 0 )
					{
						JoinTeam( cl, Team.BLUE );
						decidedTeam = Team.BLUE;
					}
					else
					{
						Log.Info( $"Joining random team:{cl}" );
						var randomValue = Rand.Int( (int)Team.RED, (int)Team.BLUE );
						JoinTeam( cl,
							randomValue == 1 ? Team.RED : Team.BLUE );
						decidedTeam = randomValue == 1 ? Team.RED : Team.BLUE;
					}
					break;
				case TeamMode.ThreeWay:
					break;
				case TeamMode.FourWay:
					break;
			}

			return decidedTeam;
		}
	}
}
