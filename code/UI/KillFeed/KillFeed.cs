using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Threading.Tasks;

namespace Breakfloor.UI
{
	public partial class KillFeed : Panel
	{
		public static KillFeed Current;

		public partial class KillFeedEntry : Panel
		{
			public Label Killer { get; internal set; }
			public Label Victim { get; internal set; }
			public Panel Icon { get; internal set; }

			public KillFeedEntry()
			{
				Killer = Add.Label( "", "left" );
				Icon = Add.Panel( "icon" );
				Victim = Add.Label( "", "right" );

				_ = RunAsync();
			}

			async Task RunAsync()
			{
				await Task.Delay( 4000 );
				Delete();
			}
		}

		public Panel AddEntry( 
			long killerId, string killerName, 
			long victimId, string victimName, 
			string method)
		{
			Log.Info( $"{killerName} killed {victimName} using {method}" );

			var e = Current.AddChild<KillFeedEntry>();

			e.AddClass( method );

			e.Killer.Text = killerName;
			e.Killer.SetClass( "me", killerId == Local.PlayerId );

			e.Victim.Text = victimName;
			e.Victim.SetClass( "me", victimId == Local.PlayerId );

			return e;
		}
	}
}
