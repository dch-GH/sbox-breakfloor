using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI;

public partial class BreakfloorHud : HudEntity<RootPanel>
{
	public BreakfloorHud()
	{
		if ( !Game.IsClient )
			return;


	}
}