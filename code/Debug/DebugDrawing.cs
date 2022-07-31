using System;
using System.Linq;
using Breakfloor.HammerEnts;
using Sandbox;
using SandboxEditor;

namespace Breakfloor.Debug
{
	public class DebugDrawing
	{
		//[DebugOverlay( "playerspawn", "PlayerSpawn", "perm_media" )]
		//public static void Draw()
		//{
		//	if ( !Host.IsClient ) return;

		//	var ents = Entity.All.Where(
		//		x =>
		//		x.GetType() == typeof( BreakfloorSpawnPoint ) );


		//	foreach ( BreakfloorSpawnPoint e in ents )
		//	{
		//		Render.Draw2D.Color = BreakfloorGame.GetTeamColor( e.Index );
		//		Render.Draw2D.Circle( (Vector2)e.Position.ToScreen( Screen.Size ), 8f, 4 );
		//		Render.Draw2D.Line( 0, (Vector2)e.Position.ToScreen( Screen.Size ), (Vector2)(e.Position + e.Rotation.Forward * 100).ToScreen( Screen.Size ) );
		//	}

		//}

	}
}
