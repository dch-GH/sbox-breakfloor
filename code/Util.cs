using Sandbox;

namespace Breakfloor
{
	public static class Vector3Ext
	{
		public static Vector3 Normalize( in Vector3 self )
		{
			var x = System.MathF.Pow( self.x, 2 );
			var y = System.MathF.Pow( self.y, 2 );
			var z = System.MathF.Pow( self.z, 2 );

			float length = System.MathF.Sqrt( x + y + z );
			return new Vector3( self.x / length, self.y / length, self.z / length );
		}
	}

}