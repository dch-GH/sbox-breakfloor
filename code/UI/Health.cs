using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	[UseTemplate]
	public class Health : Panel
	{
		Label value { get; set; }
		public Health()
		{
			AddClass( "health" );
		}

		public override void Tick()
		{
			base.Tick();
			var pawn = Local.Pawn;
			if ( pawn == null ) return;
			value.Text = pawn.Health.FloorToInt().ToString();
		}
	}
}