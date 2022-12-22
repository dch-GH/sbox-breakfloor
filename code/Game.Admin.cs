using Breakfloor.UI;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Breakfloor
{
	partial class BreakfloorGame : GameManager
	{
		public static List<long> Admins { get; private set; }

		public override void DoPlayerDevCam( IClient client )
		{
			Game.AssertServer();

			if ( !Admins.Contains( client.SteamId ) )
				return;

			var camera = client.Components.Get<DevCamera>( true );

			if ( camera == null )
			{
				camera = new DevCamera();
				client.Components.Add( camera );
				return;
			}

			camera.Enabled = !camera.Enabled;
		}

		[ConCmd.Server("noclip")]
		public static void Noclip()
		{
			var caller = ConsoleSystem.Caller;

			if ( !Admins.Contains( caller.SteamId ) )
				return;

			if ( caller.Pawn is not Player ply )
				return;

			if ( ply.Controller.GetType() != typeof( NoclipController ) )
				ply.Controller = new NoclipController();
			else
				ply.Controller = new WalkController();
		}

		[ConCmd.Server( "bf_status" )]
		public static void AdminStatus()
		{
			var caller = ConsoleSystem.Caller;

			if ( !Admins.Contains( caller.SteamId ) )
				return;

			if ( caller.IsListenServerHost ) //Caller is a server host admin.
			{
				Log.Info( "Printing status on Host..." );
				foreach ( var c in Game.Clients )
				{
					Log.Info( $"{c.Name} : {c.SteamId}" );
				}
			}
			else //Caller is a client admin.
			{
				ClientLog( To.Single( caller ), "Printing status on IClient..." );
				foreach ( var c in Game.Clients )
				{
					ClientLog( To.Single( caller ), $"{c.Name} : {c.SteamId}" );
				}

			}
		}

		[ConCmd.Server( "bf_kick" )]
		public static void AdminKick( string id, string reason = null )
		{
			if ( !Admins.Contains( ConsoleSystem.Caller.SteamId ) )
				return;

			if ( string.IsNullOrEmpty( id ) ) return;

			foreach ( var c in Game.Clients )
			{
				if ( c.SteamId == long.Parse( id ) )
				{
					c.Pawn.Delete();
					Log.Info( $"name:{c.Name}, id:{c.SteamId} kicked by admin." );
					var kickedText = string.IsNullOrEmpty( reason ) ? "was kicked by admin." : $"was kicked by admin. Reason: {reason}";
					// Chat.AddInformation( To.Everyone, $"{c.Name} ({c.SteamId}) {kickedText}", null, true );
				}
			}
		}

		[ConCmd.Server( "bf_gag" )]
		public static void AdminGag( string id, bool enabled )
		{
			var caller = ConsoleSystem.Caller;
			if ( !Admins.Contains( caller.SteamId ) )
				return;

			if ( string.IsNullOrEmpty( id ) ) return;
			var parsedId = long.Parse( id );
			var everyoneElse = Game.Clients.Where( x => x.SteamId != parsedId );

			foreach ( var c in Game.Clients )
			{
				//found the target
				if ( c.SteamId == parsedId )
				{
					c.SetValue( "gagged", enabled ); //gag flag them

					if ( enabled )
					{
						//announce to the gagged user and to everyone
						// Chat.AddInformation( To.Multiple( everyoneElse ), $"{c.Name} was gagged by an admin.", null, false );
						// Chat.AddInformation( To.Single( c ), "You were gagged by an admin.", null, false );
					}
					else
					{
						//announce to the un-gagged user and to everyone
						// Chat.AddInformation( To.Multiple( everyoneElse ), $"{c.Name} was un-gagged by an admin.", null, false );
						// Chat.AddInformation( To.Single( c ), "You were un-gagged by an admin. You can now chat again.", null, false );
					}

					//Logging info
					if ( caller.IsListenServerHost ) //Caller is a server host admin.
					{
						Log.Info( $"Gagged: {c.Name}" );
					}
					else //Caller is a client admin.
					{
						ClientLog( To.Single( caller ), $"Gagged: {c.Name}" );
					}
				}
			}
		}

		[ClientRpc]
		public static void ClientLog( string message )
		{
			Game.AssertClient();
			Log.Info( message );
		}

	}
}
