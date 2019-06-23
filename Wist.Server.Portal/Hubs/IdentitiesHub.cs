using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Wist.Server.Portal.Hubs
{
	public class IdentitiesHub : Hub
	{
		public async Task AddToGroup(string groupName)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
		}

		public async Task RemoveFromGroup(string groupName)
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
		}
	}
}
