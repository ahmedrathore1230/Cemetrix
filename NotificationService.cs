using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CEMETRIX.Application.Common;
using CEMETRIX.Application.DTOs.Notifications;
using CEMETRIX.Application.Interfaces.Repositories;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CEMETRIX.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public NotificationService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PagedResult<NotificationDto>> GetPagedAsync(NotificationFilterDto filter)
    {
        var query = _uow.Notifications.Query().Where(n => !n.IsDeleted);

        if (filter.IsRead.HasValue) query = query.Where(n => n.IsRead == filter.IsRead.Value);
        if (filter.Type.HasValue) query = query.Where(n => n.Type == filter.Type.Value);
        if (!string.IsNullOrWhiteSpace(filter.UserId))
            query = query.Where(n => n.UserId == null || n.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(n => n.Title.Contains(s) || n.Message.Contains(s));
        }

        query = query.OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
        return new PagedResult<NotificationDto>
        {
            Items = _mapper.Map<List<NotificationDto>>(items),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<NotificationDto>> GetRecentAsync(string? userId = null, int take = 10)
    {
        var query = _uow.Notifications.Query().Where(n => !n.IsDeleted);
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(n => n.UserId == null || n.UserId == userId);
        var entities = await query.OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync();
        return _mapper.Map<List<NotificationDto>>(entities);
    }

    public async Task<int> GetUnreadCountAsync(string? userId = null)
    {
        var query = _uow.Notifications.Query().Where(n => !n.IsDeleted && !n.IsRead);
        if (!string.IsNullOrWhiteSpace(userId))
            query = query.Where(n => n.UserId == null || n.UserId == userId);
        return await query.CountAsync();
    }

    public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto)
    {
        var entity = _mapper.Map<Notification>(dto);
        await _uow.Notifications.AddAsync(entity);
        await _uow.SaveChangesAsync();
        return _mapper.Map<NotificationDto>(entity);
    }

    public async Task<bool> MarkAsReadAsync(int id)
    {
        var entity = await _uow.Notifications.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsRead = true;
        _uow.Notifications.Update(entity);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAllAsReadAsync(string? userId = null)
    {
        var query = _uow.Notifications.Query().Where(n => !n.IsRead);
        if (!string.IsNullOrWhiteSpace(userId)) query = query.Where(n => n.UserId == null || n.UserId == userId);
        var items = await query.ToListAsync();
        foreach (var item in items) item.IsRead = true;
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _uow.Notifications.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsDeleted = true;
        _uow.Notifications.Update(entity);
        await _uow.SaveChangesAsync();
        return true;
    }
}
