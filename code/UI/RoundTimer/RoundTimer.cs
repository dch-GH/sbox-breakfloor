using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	[UseTemplate]
	public class RoundTimer : Panel
	{
		Label value { get; set; }
		public RoundTimer()
		{
			AddClass( "round-timer" );
		}

		public override void Tick()
		{
			base.Tick();
			var game = Game.Current as BreakfloorGame;

			var span = TimeSpan.FromSeconds( (game.RoundTimer * 60).Clamp( 0, float.MaxValue ));

			value.Text = span.ToString( @"h\:mm" );
		}
	}
}
