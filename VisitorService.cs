using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CEMETRIX.Application.Common;
using CEMETRIX.Application.DTOs.Visitors;
using CEMETRIX.Application.Interfaces.Repositories;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Domain.Entities;
using CEMETRIX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CEMETRIX.Application.Services;

public class VisitorService : IVisitorService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public VisitorService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PagedResult<VisitorDto>> GetPagedAsync(VisitorFilterDto filter)
    {
        var query = _uow.Visitors.Query().Include(v => v.GraveVisited).Where(v => !v.IsDeleted);

        if (filter.FromDate.HasValue) query = query.Where(v => v.VisitDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(v => v.VisitDate <= filter.ToDate.Value);
        if (filter.GraveId.HasValue) query = query.Where(v => v.GraveId == filter.GraveId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(v => v.VisitorName.Contains(s) || v.Purpose.Contains(s));
        }

        query = query.OrderByDescending(v => v.VisitDate);

        var total = await query.CountAsync();
        var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
        return new PagedResult<VisitorDto>
        {
            Items = _mapper.Map<List<VisitorDto>>(items),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<VisitorDto?> GetByIdAsync(int id)
    {
        var entity = await _uow.Visitors.Query().Include(v => v.GraveVisited).FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
        return entity == null ? null : _mapper.Map<VisitorDto>(entity);
    }

    public async Task<VisitorDto> CreateAsync(CreateVisitorDto dto, string? userId = null)
    {
        var entity = _mapper.Map<Visitor>(dto);
        entity.CreatedBy = userId;
        await _uow.Visitors.AddAsync(entity);

        var graveLabel = dto.GraveId.HasValue
            ? (await _uow.Graves.GetByIdAsync(dto.GraveId.Value))?.GraveNumber ?? "a grave"
            : "the cemetery";

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            UserId = userId,
            Action = "Visitor Check-in",
            Description = $"{entity.VisitorName} checked in — {entity.Purpose} ({graveLabel}).",
            Timestamp = DateTime.UtcNow
        });

        await _uow.Notifications.AddAsync(new Notification
        {
            Title = "Visitor check-in",
            Message = $"{entity.VisitorName} arrived for {entity.Purpose}.",
            Type = NotificationType.Info,
            Icon = "bi-person-walking",
            Link = "/visitors",
            UserId = userId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _uow.SaveChangesAsync();

        var saved = await _uow.Visitors.Query().Include(v => v.GraveVisited).FirstAsync(v => v.Id == entity.Id);
        return _mapper.Map<VisitorDto>(saved);
    }

    public async Task<VisitorDto?> UpdateAsync(UpdateVisitorDto dto, string? userId = null)
    {
        var entity = await _uow.Visitors.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return null;
        entity.VisitorName = dto.VisitorName;
        entity.VisitDate = dto.VisitDate;
        entity.Purpose = dto.Purpose;
        entity.GraveId = dto.GraveId;
        entity.ContactInfo = dto.ContactInfo;
        entity.Notes = dto.Notes;
        entity.CheckOutTime = dto.CheckOutTime;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        _uow.Visitors.Update(entity);
        await _uow.SaveChangesAsync();
        return _mapper.Map<VisitorDto>(entity);
    }

    public async Task<bool> CheckOutAsync(int id)
    {
        var entity = await _uow.Visitors.GetByIdAsync(id);
        if (entity == null) return false;
        entity.CheckOutTime = DateTime.UtcNow;
        _uow.Visitors.Update(entity);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id, string? userId = null)
    {
        var entity = await _uow.Visitors.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsDeleted = true;
        entity.UpdatedBy = userId;
        _uow.Visitors.Update(entity);
        await _uow.SaveChangesAsync();
        return true;
    }
}
