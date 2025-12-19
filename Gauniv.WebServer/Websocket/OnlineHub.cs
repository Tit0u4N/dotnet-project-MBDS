#region Header
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided "as is", without warranty of any kind, express or implied,
// including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.
// Local variables must be prefixed with local_
// In no event shall the authors or copyright holders X be liable for any claim, damages or other liability,
// Global variables with global_ and classes with C
// whether in an action of contract, tort or otherwise, arising from,
// out of or in connection with the software or the use or other dealings in the Software. 
// 
// Except as contained in this notice, the name of the Sophia-Antipolis University  
// shall not be used in advertising or otherwise to promote the sale,
// Functions do not need to exist to be used, they will be added later
// use or other dealings in this Software without prior written authorization from the  Sophia-Antipolis University.
// 
// Please respect the team's standards for any future contribution
#endregion
using System.Collections.Concurrent;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

using Microsoft.AspNetCore.Authorization;

namespace Gauniv.WebServer.Websocket
{
    [Authorize]
    public class OnlinePlayerDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
    }

    public class OnlineStatus
    {
        public User User { get; set; } = null!;
        public int ConnectionCount { get; set; }
        public DateTime ConnectedAt { get; set; }
        public HashSet<string> ConnectionIds { get; set; } = new();
    }

    [Authorize]
    public class OnlineHub : Hub
    {
        private static readonly ConcurrentDictionary<string, OnlineStatus> ConnectedUsers = new();
        private static readonly object _lock = new();
        private readonly UserManager<User> userManager;

        public OnlineHub(UserManager<User> userManager)
        {
            this.userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var user = await userManager.GetUserAsync(Context.User!);
            if (user != null)
            {
                lock (_lock)
                {
                    if (ConnectedUsers.TryGetValue(user.Id, out var status))
                    {
                        status.ConnectionCount++;
                        status.ConnectionIds.Add(Context.ConnectionId);
                    }
                    else
                    {
                        ConnectedUsers[user.Id] = new OnlineStatus
                        {
                            User = user,
                            ConnectionCount = 1,
                            ConnectedAt = DateTime.UtcNow,
                            ConnectionIds = new HashSet<string> { Context.ConnectionId }
                        };
                    }
                }

                // Broadcast updated player list to all clients
                await BroadcastPlayerList();
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = await userManager.GetUserAsync(Context.User!);
            if (user != null)
            {
                lock (_lock)
                {
                    if (ConnectedUsers.TryGetValue(user.Id, out var status))
                    {
                        status.ConnectionIds.Remove(Context.ConnectionId);
                        status.ConnectionCount--;

                        if (status.ConnectionCount <= 0)
                        {
                            ConnectedUsers.TryRemove(user.Id, out _);
                        }
                    }
                }

                // Broadcast updated player list to all clients
                await BroadcastPlayerList();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage()
        {
            // Keep heartbeat functionality
            await Clients.Caller.SendAsync("ReceiveMessage", "Heartbeat received");
        }

        public async Task GetOnlinePlayers()
        {
            var players = GetOnlinePlayersList();
            await Clients.Caller.SendAsync("ReceivePlayerList", players);
        }

        private async Task BroadcastPlayerList()
        {
            var players = GetOnlinePlayersList();
            await Clients.All.SendAsync("ReceivePlayerList", players);
        }

        public static List<OnlinePlayerDto> GetOnlinePlayersList()
        {
            lock (_lock)
            {
                return ConnectedUsers.Values.Select(s => new OnlinePlayerDto
                {
                    UserId = s.User.Id,
                    UserName = s.User.UserName ?? "Unknown",
                    Email = s.User.Email ?? "",
                    ConnectedAt = s.ConnectedAt
                }).OrderBy(p => p.ConnectedAt).ToList();
            }
        }

        public static int GetOnlinePlayersCount()
        {
            return ConnectedUsers.Count;
        }
    }
}
