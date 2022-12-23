using Sandbox;
using Sandbox.UI;
using Breakfloor.Events;

namespace Breakfloor.UI;

partial class Health : Panel
{
	private TimeSince sinceHurt;

	[BFEVents.LocalPlayerHurtEvent]
	private void OnHurt()
	{
		sinceHurt = 0;
		SetClass( "hurt", true );
	}
}
