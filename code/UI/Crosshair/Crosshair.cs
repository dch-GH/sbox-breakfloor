using System;
using System.Collections.Generic;
using Breakfloor.Weapons;
using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	[UseTemplate]
	public partial class Crosshair : Panel
	{
		Panel ReloadIndicator { get; set; }

		public Crosshair()
		{
		}

		public override void Tick()
		{
			base.Tick();

			if ( Game.LocalPawn == null ) return;

			var ply = (BreakfloorPlayer)Game.LocalPawn;

			SetClass( "hidden", ply.LifeState != LifeState.Alive );

			if ( ply.ActiveChild == null ) return;
			var wep = (BreakfloorGun)ply.ActiveChild;

			if ( !Game.LocalClient.GetValue<bool>( BreakfloorGame.BF_AUTO_RELOAD_KEY ) )
				ReloadIndicator.SetClass( "active", (wep.ClipAmmo <= 0) && !wep.IsReloading );
		}
	}
}
