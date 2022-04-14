using System;
using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	public class TargetID : Panel
	{
		Label TargetName { get; set; }
		Label TargetHealth { get; set; }

		public TargetID()
		{
			StyleSheet.Load( "/ui/BreakfloorHud.scss" );
			TargetName = AddChild<Label>();
			TargetHealth = AddChild<Label>();
		}

		public override void Tick()
		{
			var ply = (BreakfloorPlayer)Local.Pawn;
			if ( ply == null ) return;

			var tr = Trace.Ray( ply.EyePosition, ply.EyePosition + ply.EyeRotation.Forward * 1500f ).Ignore( ply ).UseHitboxes().Run();
			if ( tr.Hit && tr.Entity is BreakfloorPlayer target )
			{
				var isTargetEnemy = target.Client.GetValue<int>( "team" ) != Local.Client.GetValue<int>( "team" );
				var teamText = isTargetEnemy ? "ENEMY:" : "FRIEND:";
				TargetName.Text = $"{teamText} {target.Client.Name}";
				TargetHealth.Text = $"Health: {target.Health.FloorToInt()}%";

				var whichClass =
					isTargetEnemy
					? "enemy"
					: "friend";
				SetClass( "active", true );
				SetClass( whichClass, true );
			}
			else
			{
				SetClass( "active", false );
				SetClass( "friend", false );
				SetClass( "enemy", false );
			}

			base.Tick();
		}
	}
}
