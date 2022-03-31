using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	[UseTemplate]
	public class Ammo : Panel
	{
		Image icon { get; set; }
		public Ammo()
		{
			//var texture = Texture.Load( FileSystem.Mounted, "/textures/ui_ammo.png", true );
			//icon.Texture = texture;
			//icon.Style.Width = texture.Width / 8;
			//icon.Style.Height = texture.Height / 8;
			AddClass( "ammo" );
		}

	}
}
