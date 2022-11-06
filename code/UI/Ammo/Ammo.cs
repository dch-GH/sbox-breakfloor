using Sandbox;
using Sandbox.UI;
using Breakfloor.Weapons;

namespace Breakfloor.UI
{
	[UseTemplate]
	public class Ammo : Panel
	{
		Label Count { get; set; }

		public override void Tick()
		{
			base.Tick();
			if ( Local.Pawn == null ) return;
			var ply = (BreakfloorPlayer)Local.Pawn;

			if(ply.ActiveChild == null) return;
			var wep = (BreakfloorGun)ply.ActiveChild;

			Count.Text = $"{wep.ClipAmmo} / {wep.MaxClip}";

		}
	}
}
