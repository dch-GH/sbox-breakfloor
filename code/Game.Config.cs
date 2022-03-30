using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Breakfloor
{
	public partial class BreakfloorGame : Sandbox.Game
	{
		public const string GamePrefix = "bf_";

		[ConVar.Replicated( GamePrefix + "block_hp" )]
		public static float BlockHealth { get; set; } = 20f;

		/// <summary>
		/// Round time in minutes.
		/// </summary>
		[ConVar.Replicated( GamePrefix + "round_time" )]
		public static int RoundTimeCvar { get; set; } = 7;
	}
}
