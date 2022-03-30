using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	public partial class BreakfloorHud : HudEntity<RootPanel>
	{
		public BreakfloorHud()
		{
			if ( !IsClient )
				return;

			RootPanel.StyleSheet.Load( "/UI/BreakfloorHud.scss" );

			RootPanel.AddChild<NameTags>();
			RootPanel.AddChild<CrosshairCanvas>();
			RootPanel.AddChild<ChatBox>();
			RootPanel.AddChild<VoiceList>();
			RootPanel.AddChild<KillFeed>();
			RootPanel.AddChild<Scoreboard<ScoreboardEntry>>();
			RootPanel.AddChild<Health>();
			RootPanel.AddChild<Ammo>();
			RootPanel.AddChild<RoundTimer>();
		}
	}
}

