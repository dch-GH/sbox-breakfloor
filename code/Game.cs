﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using Breakfloor.UI;
using Breakfloor.Weapons;

namespace Breakfloor
{
	partial class BreakfloorGame : Game
	{
		public static readonly string VERSION = "1.1.0";
		public static readonly Vector3 BlockDimensions = new Vector3( 64, 64, 64 );

		public static BreakfloorGame Instance => Current as BreakfloorGame;

		[Net]
		public TimeSpan RoundTimer { get; private set; } // TODO: replace with RealTimeUntil?
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
				Log.Info( $"Breakfloor server running version: {VERSION}" );
			}

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

			BFChatbox.AddInformation( To.Single( cl ),
				$"Welcome to Breakfloor! Toggle auto reloading by typing \"bf_auto_reload true\" in the console." );

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
			//gotta love foreachs :D

			foreach ( var block in Entity.All.OfType<BreakFloorBlock>() )
			{
				block.Reset();
			}

			foreach ( var e in WorldEntity.All )
			{
				e.RemoveAllDecals();
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

			var vic = (BreakfloorPlayer)victimPawn;

			if ( victimPawn.LastAttacker.GetType() == typeof( TriggerHurt ) )
			{
				var block = vic.LastBlockStoodOn;
				if ( block != null && block.Broken )
				{
					if ( block.LastAttacker == vic ) //Player caused their own downfall
					{
						var suicideText = new string[3] { "got themself killed", "played themself", "dug straight down" };
						OnKilledMessage( To.Everyone,
							victimClient.PlayerId,
							victimClient.Name,
							-1,
							String.Empty,
							Rand.FromArray<string>( suicideText ) );
					}
					else //Other player caused it
					{
						OnKilledMessage( To.Everyone,
							block.LastAttacker.Client.PlayerId,
							block.LastAttacker.Client.Name,
							victimClient.PlayerId,
							victimClient.Name,
							"BREAKFLOORED" );

						block.LastAttacker.Client.AddInt( "kills" );
					}
					return;
				}

				//Otherwise, they just fell through a hole in the blocks and died
				OnKilledMessage( To.Everyone,
					victimClient.PlayerId,
					victimClient.Name,
					-1,
					String.Empty,
					"died" );
				return;
			}

			if ( victimPawn.LastAttacker.Client != null )
			{
				var killedByText = (victimPawn.LastAttackerWeapon as BreakfloorWeapon).GetKilledByText();

				if ( string.IsNullOrEmpty( killedByText ) )
				{
					killedByText = victimPawn.LastAttackerWeapon.ClassName;
				}

				OnKilledClient( To.Everyone, victimPawn.LastAttacker.Client.PlayerId, victimPawn.LastAttacker.Client.Name,
					victimClient.PlayerId,
					victimClient.Name,
					killedByText );
			}

		}

		[ClientRpc]
		public void OnKilledClient( long killerId, string killerName,
			long victimId, string victimName,
			string method )
		{
			KillFeed.Current.AddEntry( killerId, killerName, victimId, victimName, method );
		}
	}
}
