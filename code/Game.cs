using Sandbox;
using System;
using System.Linq;

namespace Breakfloor
{
	partial class BreakfloorGame : Game
	{
		public static readonly Vector3 BlockDimensions = new Vector3( 64, 64, 64 );

		public static BreakfloorGame Instance => Current as BreakfloorGame;

		[Net]
		public TimeSpan RoundTimer { get; private set; }
		private bool roundTimerStarted = false;
		private TimeSince roundTimerLastSecond;

		public BreakfloorGame()
		{
			//
			// Create the HUD entity. This is always broadcast to all clients
			// and will create the UI panels clientside. It's accessible 
			// globally via Hud.Current, so we don't need to store it.
			//
			if ( IsServer )
			{
				_ = new Breakfloor.UI.BreakfloorHud();
				RoundTimer = TimeSpan.FromMinutes( RoundTimeCvar ); //so the timer can be frozen at the roundtimecvar.
			}

		}

		public override void PostLevelLoaded()
		{
			base.PostLevelLoaded();
		}

		public override void ClientJoined( Client cl )
		{
			base.ClientJoined( cl );

			//Decide which team the client will be on.
			var teamDifference = TeamA.Count - TeamB.Count;
			if ( teamDifference < 0 )
			{
				JoinTeam( cl, Team.A );
			}
			else if ( teamDifference > 0 )
			{
				JoinTeam( cl, Team.B );
			}
			else
			{
				JoinRandomTeam( cl );
			}

			var player = new BreakfloorPlayer();
			cl.Pawn = player;
			player.UpdateClothes( cl );
			player.Respawn();

			//Update the status of the round timer AFTER the joining client's
			//team is set.
			if ( !roundTimerStarted && TeamA.Count >= 1 && TeamB.Count >= 1 )
			{
				RoundTimer = TimeSpan.FromMinutes( RoundTimeCvar );
				roundTimerStarted = true;
			}
		}

		[Event.Tick.Server]
		public void ServerTick()
		{
			if ( roundTimerStarted && roundTimerLastSecond >= 1 )
			{
				RoundTimer = RoundTimer.Subtract( TimeSpan.FromSeconds( 1 ) );
				if ( RoundTimer.TotalSeconds <= 0 )
				{
					//handle restart round restart the timer etc.
				}

				roundTimerLastSecond = 0;
			}
		}

	}
}
