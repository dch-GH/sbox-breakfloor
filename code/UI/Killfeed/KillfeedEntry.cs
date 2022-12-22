using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System.Threading.Tasks;

namespace Breakfloor.UI;

public partial class KillfeedEntry
{
	public KillfeedEntry()
	{
		_ = RunAsync();
	}

	async Task RunAsync()
	{
		await Task.Delay( 10000 );
		Delete();
	}
}
