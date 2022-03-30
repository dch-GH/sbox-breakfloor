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
			var time = BreakfloorGame.Instance.RoundTimer;
			value.Text = time.ToString( @"mm\:ss" );
		}
	}
}
