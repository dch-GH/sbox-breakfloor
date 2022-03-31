using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Breakfloor
{
	partial class BreakfloorGame : Game
	{
		public static List<long> Devs = new List<long>() { 76561197998255119 };

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
	}
}
