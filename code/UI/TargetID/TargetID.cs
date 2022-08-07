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
			TargetHealth.AddClass( "target-health" );
		}

		public override void Tick()
		{
			var ply = (BreakfloorPlayer)Local.Pawn;
			if ( ply == null ) return;

			var tr = Trace.Ray( ply.EyePosition, ply.EyePosition + ply.EyeRotation.Forward * 1500f ).Ignore( ply ).UseHitboxes().Run();
			if ( tr.Hit && tr.Entity is BreakfloorPlayer target )
			{
				var isTargetEnemy = target.Team != ply.Team;
				var teamText = isTargetEnemy ? "ENEMY:" : "FRIEND:";
				TargetName.Text = $"{teamText} {target.Client.Name}";
				TargetHealth.Text = $"HEALTH: {target.Health.FloorToInt()}%";

				SetClass( "active", true );
				SetClass( isTargetEnemy
					? "enemy"
					: "friend", true );
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
