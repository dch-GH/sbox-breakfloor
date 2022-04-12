using Sandbox;

namespace Breakfloor.Events
{
	public static class Event
	{
		public class BlockBreakAttribute : EventAttribute
		{
			public BlockBreakAttribute() : base( "bf.block.break" )
			{

			}
		}
	}

}
