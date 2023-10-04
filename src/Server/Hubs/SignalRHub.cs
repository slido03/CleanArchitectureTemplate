﻿using CleanArchitecture.Application.Interfaces.Chat;
using CleanArchitecture.Application.Models.Chat;
using CleanArchitecture.Shared.Constants.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CleanArchitecture.Server.Hubs
{
    [Authorize]
    public class SignalRHub : Hub
    {
        public async Task PingRequestAsync(string userId)
        {
            await Clients.All.SendAsync(ApplicationConstants.SignalR.PingRequest, userId);
        }
        public async Task PingResponseAsync(string userId, string requestedUserId)
        {
            await Clients.User(requestedUserId).SendAsync(ApplicationConstants.SignalR.PingResponse, userId);
        }
        public async Task OnConnectAsync(string userId)
        {
            await Clients.All.SendAsync(ApplicationConstants.SignalR.ConnectUser, userId);
        }

        public async Task OnDisconnectAsync(string userId)
        {
            await Clients.All.SendAsync(ApplicationConstants.SignalR.DisconnectUser, userId);
        }

        public async Task OnChangeRolePermissions(string userId, string roleId)
        {
            await Clients.All.SendAsync(ApplicationConstants.SignalR.LogoutUsersByRole, userId, roleId);
        }

        public async Task SendMessageAsync(ChatHistory<IChatUser> chatHistory, string userName)
        {
            await Clients.User(chatHistory.ToUserId).SendAsync(ApplicationConstants.SignalR.ReceiveMessage, chatHistory, userName);
            await Clients.User(chatHistory.FromUserId).SendAsync(ApplicationConstants.SignalR.ReceiveMessage, chatHistory, userName);
        }

        public async Task ChatNotificationAsync(string message, string receiverUserId, string senderUserId)
        {
            await Clients.User(receiverUserId).SendAsync(ApplicationConstants.SignalR.ReceiveChatNotification, message, receiverUserId, senderUserId);
        }

        public async Task UpdateDashboardAsync()
        {
            await Clients.All.SendAsync(ApplicationConstants.SignalR.ReceiveUpdateDashboard);
        }

        public async Task RegenerateTokensAsync()
        {
            await Clients.All.SendAsync(ApplicationConstants.SignalR.ReceiveRegenerateTokens);
        }
    }
}