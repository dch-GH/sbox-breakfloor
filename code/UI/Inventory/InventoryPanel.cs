using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	[UseTemplate]
	public partial class InventoryPanel : Panel
	{

		public override void Tick()
		{
			base.Tick();

			var ply = Local.Pawn as BreakfloorPlayer;
			if ( ply == null || ply.LifeState == LifeState.Dead ) return;
			var inv = ply.Inventory;
			
		}
	}
}
