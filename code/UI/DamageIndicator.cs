using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	public class DamageIndicator : Panel
	{
		public static DamageIndicator Current;

		private TimeSince hit;

		public DamageIndicator()
		{
			StyleSheet.Load( "/ui/BreakfloorHud.scss" );
			Current = this;
		}

		public void Hit()
		{
			if ( !BreakfloorGame.DoDamageIndicator ) return;
			hit = 0;
			SetClass( "hit", true );
		}

		public override void Tick()
		{
			base.Tick();
			if ( hit >= 0.1f ) SetClass( "hit", false );
		}
	}
}
