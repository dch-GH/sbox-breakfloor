using Sandbox;
using System.Collections.Generic;
using System.Linq;
using Breakfloor.UI;
using Breakfloor.Events;

namespace Breakfloor;

partial class BreakfloorGame : GameManager
{
	public static BreakfloorGame Instance { get; private set; }

	public static readonly float StandardBlockSize = 64;
	public static readonly float StandardHalfBlockSize = StandardBlockSize / 2;
	public static readonly Vector3 StandardBlockDimensions = Vector3.One * StandardBlockSize;

	[Net] public RealTimeUntil RoundTimer { get; private set; } = 0f;

	// I don't plan on tracking gamestate any more complicated than this.
	[Net] public bool RoundActive { get; private set; } = false;

	private MapRules gameRules;

	public BreakfloorGame()
	{
		if ( Game.IsServer )
		{
			// This is really silly, I know. I assume we'll be able to store 
			// at the very least a JSON/.txt file or something on the s&works addon page for secrets some day.
			Admins = new List<long>() { 76561197998255119 };

			RoundTimer = RoundTimeCvar; // So the timer can be frozen at the roundtimecvar.
		}

		if ( Game.IsClient )
		{
			_ = new Hud();
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
		// Chat.AddInformation( To.Everyone, $"{cl.Name} has joined", $"avatar:{cl.SteamId}", isAdmin );

		var player = new Breakfloor.Player();
		cl.Pawn = player;

		var resultingTeam = HandleTeamAssign( cl );
		player.Team = resultingTeam;
		cl.SetValue( "team", (int)resultingTeam );

		player.UpdateClothes( cl );
		player.Respawn();

		// Chat.AddInformation( To.Single( cl ),
		// 	$"Welcome to Breakfloor! You can toggle auto reloading by typing \"bf_auto_reload true\" into the console." );

		//Update the status of the round timer AFTER the joining client's
		//team is set.
		if ( !RoundActive && (GetTeamCount( Team.RED ) >= 1 && GetTeamCount( Team.BLUE ) >= 1) )
		{
			RestartRound();
			RoundTimer = RoundTimeCvar;
			RoundActive = true;
			Log.Info( "Starting round." );
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
			RoundActive = false;
			RestartRound();
		}

	}

	[Event.Tick.Server]
	public void ServerTick()
	{
		if ( RoundActive )
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
		Event.Run( BFEVents.Reset );

		Game.WorldEntity.RemoveAllDecals();

		foreach ( var block in Entity.All.OfType<BreakFloorBlock>() )
		{
			block.Reset();
		}

		foreach ( var c in Game.Clients )
		{
			if ( c.Pawn is Player ply )
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

		var vicPlayer = (Player)victimPawn;

		if ( victimPawn.LastAttacker == null ) return;

		// First check if we died to a map hurt trigger
		if ( victimPawn.LastAttacker.GetType() == typeof( HurtVolumeEntity ) )
		{
			var block = vicPlayer.LastBlockStoodOn;
			if ( block != null && block.Broken )
			{
				// Player caused their own downfall
				if ( block.LastAttacker == vicPlayer )
				{
					var suicideText = new string[3] { "died self", "played themself", "dug straight down" };
					OnKilledClient( To.Everyone,
						victimClient,
						null,
						Game.Random.FromArray<string>( suicideText ) );
				}
				else
				{
					// Other player caused it
					OnKilledClient( To.Everyone,
						block.LastAttacker.Client,
						victimClient,
						"BREAKFLOOR'D" );

					block.LastAttacker.Client.AddInt( "kills", 2 );
				}
				return;
			}

			// Otherwise, they just fell through a hole in the blocks and died
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
			var killedByText = (victimPawn.LastAttackerWeapon as Gun)
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
		Killfeed.Current.AddEntry( killer, victim, method );
	}
}
