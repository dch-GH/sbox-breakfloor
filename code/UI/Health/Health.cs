using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	[UseTemplate]
	public class Health : Panel
	{
		public static Health Current;
		private TimeSince sinceHurt;

		Label value { get; set; }

		public Health()
		{
			AddClass( "health" );
			Current = this;
		}

		public void Hurt()
		{
			sinceHurt = 0;
			SetClass( "hurt", true );
		}

		public override void Tick()
		{
			base.Tick();
			var pawn = Game.LocalPawn;
			if ( pawn == null ) return;
			value.Text = pawn.Health.FloorToInt().ToString();

			if(sinceHurt >= 0.35f)
			{
				SetClass("hurt", false);
			}
		}
	}
}
