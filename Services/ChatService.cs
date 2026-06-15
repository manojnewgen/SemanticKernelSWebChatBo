using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using SemanticKernelSWebChatBot.Data;
using SemanticKernelSWebChatBot.Models;

namespace SemanticKernelSWebChatBot.Services
{
    public class ChatService(AppDbContext _dbContext)
    {
        public async Task AddMessageAsync(string sessionid, string sender, string message)
        {
            var chatMessage = new ChatMessage
            {
                SessionId = sessionid,
                Message = message,
                Sender = sender
            };
            _dbContext.ChatMessages.Add(chatMessage);
            await _dbContext.SaveChangesAsync();

        }

        public async Task<IEnumerable<ChatMessage>> GetChatsAsync(int id)
        {
            return await _dbContext.ChatMessages.Where(x=>x.Id==id).OrderBy(m=>m.Timestamp).ToListAsync();
        }
    }
}