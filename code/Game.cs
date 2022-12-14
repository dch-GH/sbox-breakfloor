using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using Breakfloor.UI;
using Breakfloor.Weapons;
using Breakfloor.HammerEnts;

namespace Breakfloor
{
	partial class BreakfloorGame : GameManager
	{
		public static readonly float StandardBlockSize = 64;
		public static readonly float StandardHalfBlockSize = StandardBlockSize / 2;
		public static readonly Vector3 StandardBlockDimensions = Vector3.One * StandardBlockSize;

		public static BreakfloorGame Instance { get; private set; }

		[Net]
		public RealTimeUntil RoundTimer { get; private set; } = 0f;

		private bool roundTimerStarted = false;
		private MapRules gameRules;

		public BreakfloorGame()
		{
			//
			// Create the HUD entity. This is always broadcast to all clients
			// and will create the UI panels clientside. It's accessible 
			// globally via Hud.Current, so we don't need to store it.
			//
			if ( Game.IsServer )
			{
				// This is really silly, I know. I assume we'll be able to store 
				// at the very least a JSON/.txt file or something on the s&works addon page for secrets some day.
				Admins = new List<long>() { 76561197998255119 };

				_ = new BreakfloorHud();
				RoundTimer = RoundTimeCvar; //so the timer can be frozen at the roundtimecvar.
			}

			Instance = this;
		}

		public override void PostLevelLoaded()
		{
			base.PostLevelLoaded();

			gameRules = Entity.All.OfType<MapRules>().FirstOrDefault();

			if ( gameRules == null )
			{
				Log.Warning( "No map rules found for this map, using standard ruleset." );
				gameRules = new MapRules
				{
					TeamSetup = TeamMode.TwoOpposing,
					MaxTeamSize = 8
				};
			}
		}

		public override void ClientJoined( IClient cl )
		{
			var isAdmin = Admins.Contains( cl.SteamId );
			Log.Info( $"\"{cl.Name}\" has joined the game" );
			BFChatbox.AddInformation( To.Everyone, $"{cl.Name} has joined", $"avatar:{cl.SteamId}", isAdmin );

			var player = new BreakfloorPlayer();
			cl.Pawn = player;
			player.Team = HandleTeamAssign( cl );
			player.UpdateClothes( cl );
			player.Respawn();

			BFChatbox.AddInformation( To.Single( cl ),
				$"Welcome to Breakfloor! You can toggle auto reloading by typing \"bf_auto_reload true\" into the console." );

			//Update the status of the round timer AFTER the joining client's
			//team is set.
			if ( !roundTimerStarted && (GetTeamCount( Team.RED ) >= 1 && GetTeamCount( Team.BLUE ) >= 1) )
			{
				RestartRound();
				RoundTimer = RoundTimeCvar;
				roundTimerStarted = true;
				Log.Info( "Starting round!" );
			}
		}

		public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
		{
			base.ClientDisconnect( cl, reason );
			AfterClientDisconnect();
		}

		public async void AfterClientDisconnect()
		{
			await GameTask.DelayRealtimeSeconds( 2 );
			if ( Game.Clients.Count <= 1 )
			{
				roundTimerStarted = false;
				RestartRound();
			}

		}

		[Event.Tick.Server]
		public void ServerTick()
		{
			if ( roundTimerStarted )
			{
				if ( RoundTimer <= 0 )
				{
					//handle restart round and restart the timer, etc.
					RestartRound();
				}
			}
			else
			{
				RoundTimer = RoundTimeCvar + 1;
			}
		}

		[ConCmd.Admin( "bf_restart" )]
		public static void BfRestart()
		{
			Instance.RestartRound();
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

			foreach ( var c in Game.Clients )
			{
				if ( c.Pawn is BreakfloorPlayer ply )
				{
					ply.Respawn();
					c.SetInt( "kills", 0 );
					c.SetInt( "deaths", 0 );
				}

			}

			RoundTimer = RoundTimeCvar;
		}

		public override void OnKilled( IClient victimClient, Entity victimPawn )
		{
			//override the base.onkilled and tweak it instead of calling base.
			Game.AssertServer();

			var vicPlayer = (BreakfloorPlayer)victimPawn;

			if ( victimPawn.LastAttacker == null ) return;


			if ( victimPawn.LastAttacker.GetType() == typeof( HurtVolumeEntity ) ) // First check if we died to a map hurt trigger
			{
				var block = vicPlayer.LastBlockStoodOn;
				if ( block != null && block.Broken )
				{
					if ( block.LastAttacker == vicPlayer ) //Player caused their own downfall
					{
						var suicideText = new string[3] { "got themself killed", "played themself", "dug straight down" };
						OnKilledClient( To.Everyone,
							victimClient,
							null,
							Game.Random.FromArray<string>( suicideText ) );
					}
					else //Other player caused it
					{
						OnKilledClient( To.Everyone,
							block.LastAttacker.Client,
							victimClient,
							"BREAKFLOOR'D" );

						block.LastAttacker.Client.AddInt( "kills", 2 );
					}
					return;
				}

				//Otherwise, they just fell through a hole in the blocks and died
				OnKilledClient( To.Everyone,
					victimClient,
					null,
					"died" );
				return;
			}

			// Player didn't die from falling or a hurt trigger then.
			// They died to a gun, how boring.
			if ( vicPlayer.LastAttacker.Client != null )
			{
				var killedByText = (victimPawn.LastAttackerWeapon as BreakfloorGun)
					.GetKilledByText( vicPlayer.LastDamage );

				if ( string.IsNullOrEmpty( killedByText ) )
				{
					killedByText = vicPlayer.LastAttackerWeapon.ClassName;
				}

				OnKilledClient( To.Everyone, vicPlayer.LastAttacker.Client, victimClient, killedByText );
			}

		}

		[ClientRpc]
		public void OnKilledClient( IClient killer, IClient victim, string method )
		{
			KillFeed.Current.AddEntry( killer, victim, method );
		}
	}
}

