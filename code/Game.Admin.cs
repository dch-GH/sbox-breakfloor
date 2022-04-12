using Breakfloor.UI;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Breakfloor
{
	partial class BreakfloorGame : Game
	{
		[Net]
		public static List<long> Devs { get; private set; }

		public override void DoPlayerDevCam( Client client )
		{
			if ( !Devs.Contains( client.PlayerId ) )
				return;

			base.DoPlayerDevCam( client );
		}

		public override void DoPlayerNoclip( Client client )
		{
			if ( !Devs.Contains( client.PlayerId ) )
				return;

			base.DoPlayerNoclip( client );
		}

		[ServerCmd( "bf_status" )]
		public static void AdminStatus()
		{
			var caller = ConsoleSystem.Caller;

			if ( !Devs.Contains( caller.PlayerId ) )
				return;

			if ( caller.IsListenServerHost ) //Caller is a server host admin.
			{
				Log.Info( "Printing status on Host..." );
				foreach ( var c in Client.All )
				{
					Log.Info( $"{c.Name} : {c.PlayerId}" );
				}
			}
			else //Caller is a client admin.
			{
				ClientLog( To.Single( caller ), "Printing status on Client..." );
				foreach ( var c in Client.All )
				{
					ClientLog( To.Single( caller ), $"{c.Name} : {c.PlayerId}" );
				}

			}

		}

		[ServerCmd( "bf_kick" )]
		public static void AdminKick( string id, string reason = null )
		{
			if ( !Devs.Contains( ConsoleSystem.Caller.PlayerId ) )
				return;

			if ( string.IsNullOrEmpty( id ) ) return;

			foreach ( var c in Client.All )
			{
				if ( c.PlayerId == long.Parse( id ) )
				{
					c.Pawn.Delete();
					Log.Info( $"name:{c.Name}, id:{c.PlayerId} kicked by admin." );
					var kickedText = string.IsNullOrEmpty( reason ) ? "was kicked by admin." : $"was kicked by admin. Reason: {reason}";
					BFChatbox.AddInformation( To.Everyone, $"{c.Name} ({c.PlayerId}) {kickedText}", null, true );
				}
			}
		}

		[ServerCmd( "bf_gag" )]
		public static void AdminGag( string id, bool enabled )
		{
			var caller = ConsoleSystem.Caller;
			if ( !Devs.Contains( caller.PlayerId ) )
				return;

			if ( string.IsNullOrEmpty( id ) ) return;
			var parsedId = long.Parse( id );
			var everyoneElse = Client.All.Where( x => x.PlayerId != parsedId );

			foreach ( var c in Client.All )
			{
				//found the target
				if ( c.PlayerId == parsedId )
				{
					c.SetValue( "gagged", enabled ); //gag flag them

					if(enabled)
					{
						//announce to the gagged user and to everyone
						BFChatbox.AddInformation( To.Multiple( everyoneElse ), $"{c.Name} was gagged by an admin.", null, false );
						BFChatbox.AddInformation( To.Single( c ), "You were gagged by an admin.", null, false );
					}
					else
					{
						//announce to the un-gagged user and to everyone
						BFChatbox.AddInformation( To.Multiple( everyoneElse ), $"{c.Name} was un-gagged by an admin.", null, false );
						BFChatbox.AddInformation( To.Single( c ), "You were un-gagged by an admin. You can now chat again.", null, false );
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
			Host.AssertClient();
			Log.Info( message );
		}

	}
}
