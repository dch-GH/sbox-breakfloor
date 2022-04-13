using Sandbox.UI.Construct;
using Sandbox;
using Sandbox.UI;

namespace Breakfloor.UI
{
	public partial class BFChatEntry : Panel
	{
		public Label DevLabel { get; internal set; }
		public Label NameLabel { get; internal set; }
		public Label Message { get; internal set; }
		public Image Avatar { get; internal set; }

		public RealTimeSince TimeSinceBorn = 0;

		public BFChatEntry()
		{
			DevLabel = Add.Label( "(ADMIN)", "devlabel" );
			Avatar = Add.Image();
			NameLabel = Add.Label( "Name", "name" );
			Message = Add.Label( "Message", "message" );
		}

		public override void Tick()
		{
			base.Tick();

			if ( TimeSinceBorn > 25 )
			{
				Delete();
			}
		}
	}
}
