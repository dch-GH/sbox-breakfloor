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
		public const string BF_AUTO_RELOAD_KEY = "bf_auto_reload";

		[ServerVar( GamePrefix + "block_hp" )]
		public static float BlockHealthCvar { get; set; } = 20f;

		/// <summary>
		/// Round time in minutes.
		/// </summary>
		[ConVar.Replicated( GamePrefix + "round_time" )]
		public static int RoundTimeCvar { get; set; } = 5;

		[ConVar.ClientData( BF_AUTO_RELOAD_KEY, Help = "set with true/false to toggle automatic reload when magazine is empty.")]
		public static bool DoAutoReload { get; set; }

		[ClientVar( "bf_damage_indicator")]
		public static bool DoDamageIndicator { get; set; } = true;
	}
}
