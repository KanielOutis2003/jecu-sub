using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Models;
using SubdivisionWebsite.Data;

namespace SubdivisionWebsite.Services
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(string description, string module, string action, string userId, 
            string status = "Completed", string? relatedEntityId = null, string? relatedEntityType = null);
        Task<List<ActivityLog>> GetRecentActivitiesAsync(int count = 10);
        Task<List<ActivityLog>> GetModuleActivitiesAsync(string module, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, int>> GetModuleStatisticsAsync(string module, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;

        public ActivityLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(string description, string module, string action, string userId,
            string status = "Completed", string? relatedEntityId = null, string? relatedEntityType = null)
        {
            var statusColor = GetStatusColor(status);

            var activity = new ActivityLog
            {
                Description = description,
                Module = module,
                Action = action,
                UserId = userId,
                Status = status,
                StatusColor = statusColor,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Set<ActivityLog>().AddAsync(activity);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ActivityLog>> GetRecentActivitiesAsync(int count = 10)
        {
            return await _context.Set<ActivityLog>()
                .Include(a => a.User)
                .Where(a => a.IsVisible)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetModuleActivitiesAsync(string module, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<ActivityLog>()
                .Include(a => a.User)
                .Where(a => a.IsVisible && a.Module == module);

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetModuleStatisticsAsync(string module, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Set<ActivityLog>()
                .Where(a => a.IsVisible && a.Module == module);

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value);

            return await query
                .GroupBy(a => a.Status)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        private string GetStatusColor(string status)
        {
            return status.ToLower() switch
            {
                "completed" => "success",
                "in progress" => "primary",
                "pending" => "warning",
                "failed" => "danger",
                "cancelled" => "secondary",
                _ => "secondary"
            };
        }
    }
} 