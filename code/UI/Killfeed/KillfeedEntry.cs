using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Threading.Tasks;

namespace Breakfloor.UI;

public partial class KillfeedEntry
{
	public KillfeedEntry()
	{
		Killer = Add.Label( "", "left" );
		Method = Add.Label( "", "method" );
		Victim = Add.Label( "", "right" );

		//lazy
		Method.Style.FontStyle = FontStyle.Italic;
		Method.Style.FontColor = Color.White;
		Method.Style.Padding = Length.Pixels( 3 );

		_ = RunAsync();
	}

	async Task RunAsync()
	{
		await Task.Delay( 10000 );
		Delete();
	}
}
