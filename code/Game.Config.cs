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

		[ConVar.Server( GamePrefix + "block_hp" )]
		public static float BlockHealthCvar { get; set; } = 40f;

		/// <summary>
		/// Round time in seconds.
		/// </summary>
		[ConVar.Replicated( GamePrefix + "round_time" )]
		public static float RoundTimeCvar { get; set; } = 210;

		[ConVar.ClientData( BF_AUTO_RELOAD_KEY, Help = "set with true/false to toggle automatic reload when magazine is empty.")]
		public static bool DoAutoReload { get; set; }

		[ConVar.Client( "bf_damage_indicator")]
		public static bool DoDamageIndicator { get; set; } = true;
	}
}
