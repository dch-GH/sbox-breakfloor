using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;
using System.Threading.Tasks;

namespace Breakfloor.UI
{
	[UseTemplate]
	public partial class KillFeed : Panel
	{
		public static KillFeed Current;

		public KillFeed()
		{
			if ( !Host.IsClient ) return;
			Current = this;
		}

		public partial class KillFeedEntry : Panel
		{
			public Label Killer { get; internal set; }
			public Label Victim { get; internal set; }
			public Label Method { get; internal set; }

			public KillFeedEntry()
			{
				Killer = Add.Label( "", "left" );
				Method = Add.Label( "", "method" );
				Victim = Add.Label( "", "right" );

				//lazy
				Method.Style.FontStyle = FontStyle.Italic;
				Method.Style.FontColor = Color.White;
				Method.Style.Padding = Length.Pixels( 3 );
				_ = RunAsync();
			}

			async Task RunAsync()
			{
				await Task.Delay( 10000 );
				Delete();
			}
		}

		public Panel AddEntry( Client killer, Client victim, string method )
		{
			var e = Current.AddChild<KillFeedEntry>();

			e.AddClass( method );

			if ( killer != null )
			{
				e.Killer.Text = $"{killer.Name} ";
				e.Killer.SetClass( "me", killer.Id == Local.PlayerId );
				var colors = new Color[] { BreakfloorGame.GetTeamColor( killer.GetValue<int>( "team" ) ), Color.White };
				e.Killer.Style.FontColor = Color.Average( colors );
			}


			e.Method.Text = $"{method} ";

			if ( victim != null )
			{
				e.Victim.Text = $"{victim.Name} ";
				e.Victim.SetClass( "me", victim.Id == Local.PlayerId );
				var colors = new Color[] { BreakfloorGame.GetTeamColor( victim.GetValue<int>( "team" ) ), Color.White };
				e.Victim.Style.FontColor = Color.Average( colors );
			}

			//dumb
			if ( killer != null && victim != null )
			{
				Log.Info( $"{killer.Name} {method} {victim.Name}" );
			}

			return e;
		}
	}
}
