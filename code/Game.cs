using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using Breakfloor.UI;

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
				Devs = new List<long>() { 76561197998255119 };
				_ = new BreakfloorHud();
				RoundTimer = TimeSpan.FromMinutes( RoundTimeCvar ); //so the timer can be frozen at the roundtimecvar.
			}

		}

		public override void PostLevelLoaded()
		{
			base.PostLevelLoaded();
		}

		public override void ClientJoined( Client cl )
		{
			var isAdmin = Devs.Contains( cl.PlayerId );
			Log.Info( $"\"{cl.Name}\" has joined the game" );
			BFChatbox.AddInformation( To.Everyone, $"{cl.Name} has joined", $"avatar:{cl.PlayerId}", isAdmin );

			//Decide which team the client will be on.
			var teamDifference = TeamA.Count - TeamB.Count;
			if ( teamDifference < 0 )
			{
				JoinTeam( cl, TeamIndexA );
			}
			else if ( teamDifference > 0 )
			{
				JoinTeam( cl, TeamIndexB );
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
				RestartRound();
				RoundTimer = TimeSpan.FromMinutes( RoundTimeCvar );
				roundTimerStarted = true;
			}
		}

		public override void ClientDisconnect( Client cl, NetworkDisconnectionReason reason )
		{
			if ( TeamA.Contains( cl ) ) TeamA.Remove( cl );
			if ( TeamB.Contains( cl ) ) TeamB.Remove( cl );
			base.ClientDisconnect( cl, reason );
		}

		[Event.Tick.Server]
		public void ServerTick()
		{
			//This is probably naive but I don't care lol.
			if ( roundTimerStarted && roundTimerLastSecond >= 1 )
			{
				RoundTimer = RoundTimer.Subtract( TimeSpan.FromSeconds( 1 ) );
				if ( RoundTimer.TotalSeconds <= 0 )
				{
					//handle restart round restart the timer etc.
					RestartRound();

				}

				roundTimerLastSecond = 0;
			}
		}

		public void RestartRound()
		{
			foreach ( var block in Entity.All.OfType<BreakFloorBlock>() )
			{
				block.Reset();
			}

			foreach ( var c in Client.All )
			{
				(c.Pawn as BreakfloorPlayer).Respawn();
				c.SetInt( "kills", 0 );
				c.SetInt( "deaths", 0 );
			}

			RoundTimer = TimeSpan.FromMinutes( RoundTimeCvar );
		}

		public override void OnKilled( Client victimClient, Entity victimPawn )
		{
			//override the base.onkilled and tweaking it instead of calling it. its does some dumb shit.
			Host.AssertServer();

			Log.Info( $"{victimClient.Name} was killed" );

			if ( victimPawn.LastAttacker != null )
			{
				//If we died from the kill floor at the bottom
				//Count the kill for the player who destroyed the block we last stood on
				if ( victimPawn.LastAttacker.GetType() == typeof( TriggerHurt )
					&& victimPawn is BreakfloorPlayer ply )
				{
					var block = ply.LastBlockStoodOn;	
					if ( block != null && block.Broken )
					{
						if ( block.LastAttacker == victimPawn ) 
						{
							//the player broke their own block and died
							var suicideText = new string[3] { "suicided", "played themself", "dug straight down" };
							OnKilledMessage( To.Everyone, victimClient.PlayerId,
							victimClient.Name,
							-1,
							String.Empty,
							Rand.FromArray<string>( suicideText ) );
						}
						else
						{
							//someone else resulted in them getting killed
							OnKilledMessage( To.Everyone, block.LastAttacker.Client.PlayerId,
								block.LastAttacker.Client.Name,
								victimClient.PlayerId,
								victimClient.Name,
								"BREAKFLOORED" );

							//actually give the block breaker the kill
							block.LastAttacker.Client.AddInt( "kills" );
						}
			
					}
					else
					{
						OnKilledMessage( To.Everyone, victimClient.PlayerId,
						victimClient.Name,
						-1,
						String.Empty,
						"died");
					}

					return;
				}

				if ( victimPawn.LastAttacker.Client != null )
				{
					var killedByText = (victimPawn.LastAttackerWeapon as Weapon).GetKilledByText();

					if ( string.IsNullOrEmpty( killedByText ) )
					{
						killedByText = victimPawn.LastAttackerWeapon?.ClassInfo?.Title;
					}

					OnKilledMessage( victimPawn.LastAttacker.Client.PlayerId, victimPawn.LastAttacker.Client.Name,
						victimClient.PlayerId,
						victimClient.Name,
						killedByText );
				}
				else
				{
					OnKilledMessage( victimPawn.LastAttacker.NetworkIdent, victimPawn.LastAttacker.ToString(), victimClient.PlayerId, victimClient.Name, "killed" );
				}
			}
			else
			{
				OnKilledMessage( 0, "", victimClient.PlayerId, victimClient.Name, "died" );
			}

			//Log.Info( $"{client.Name} was killed by {killer.Client.NetworkIdent} with {weapon}" );
		}
	}
}
