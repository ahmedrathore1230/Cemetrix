using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CEMETRIX.Application.Common;
using CEMETRIX.Application.DTOs.Graves;
using CEMETRIX.Application.Interfaces.Repositories;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Domain.Entities;
using CEMETRIX.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CEMETRIX.Application.Services;

public class GraveService : IGraveService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GraveService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PagedResult<GraveDto>> GetPagedAsync(GraveFilterDto filter)
    {
        var query = _uow.Graves.Query()
            .Include(g => g.DeceasedPersons)
            .Where(g => !g.IsDeleted);

        if (filter.Status.HasValue)
            query = query.Where(g => g.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.Section))
            query = query.Where(g => g.Section == filter.Section);

        if (filter.IsReusable.HasValue)
            query = query.Where(g => g.IsReusable == filter.IsReusable.Value);

        if (filter.ExpiringOnly == true)
        {
            var cutoff = DateTime.UtcNow.AddDays(90);
            query = query.Where(g => g.ExpirationDate != null && g.ExpirationDate <= cutoff && g.ExpirationDate >= DateTime.UtcNow);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(g => g.GraveNumber.Contains(s) || g.Section.Contains(s) || (g.Notes != null && g.Notes.Contains(s)));
        }

        query = (filter.SortBy?.ToLower()) switch
        {
            "section" => filter.SortDescending ? query.OrderByDescending(x => x.Section) : query.OrderBy(x => x.Section),
            "status" => filter.SortDescending ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "burialdate" => filter.SortDescending ? query.OrderByDescending(x => x.BurialDate) : query.OrderBy(x => x.BurialDate),
            "expirationdate" => filter.SortDescending ? query.OrderByDescending(x => x.ExpirationDate) : query.OrderBy(x => x.ExpirationDate),
            _ => filter.SortDescending ? query.OrderByDescending(x => x.GraveNumber) : query.OrderBy(x => x.GraveNumber),
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<GraveDto>
        {
            Items = _mapper.Map<List<GraveDto>>(items),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<GraveDto?> GetByIdAsync(int id)
    {
        var entity = await _uow.Graves.Query()
            .Include(g => g.DeceasedPersons)
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);
        return entity == null ? null : _mapper.Map<GraveDto>(entity);
    }

    public async Task<GraveDto?> GetByNumberAsync(string graveNumber)
    {
        var entity = await _uow.Graves.Query()
            .Include(g => g.DeceasedPersons)
            .FirstOrDefaultAsync(g => g.GraveNumber == graveNumber && !g.IsDeleted);
        return entity == null ? null : _mapper.Map<GraveDto>(entity);
    }

    public async Task<GraveDto> CreateAsync(CreateGraveDto dto, string? userId = null)
    {
        var entity = _mapper.Map<Grave>(dto);
        entity.CreatedBy = userId;
        await _uow.Graves.AddAsync(entity);
        await _uow.SaveChangesAsync();
        return _mapper.Map<GraveDto>(entity);
    }

    public async Task<GraveDto?> UpdateAsync(UpdateGraveDto dto, string? userId = null)
    {
        var entity = await _uow.Graves.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return null;

        entity.GraveNumber = dto.GraveNumber;
        entity.Section = dto.Section;
        entity.Row = dto.Row;
        entity.Column = dto.Column;
        entity.Status = dto.Status;
        entity.BurialDate = dto.BurialDate;
        entity.ExpirationDate = dto.ExpirationDate;
        entity.Latitude = dto.Latitude;
        entity.Longitude = dto.Longitude;
        entity.Notes = dto.Notes;
        entity.IsReusable = dto.IsReusable;
        entity.Price = dto.Price;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;

        _uow.Graves.Update(entity);
        await _uow.SaveChangesAsync();
        return _mapper.Map<GraveDto>(entity);
    }

    public async Task<bool> DeleteAsync(int id, string? userId = null)
    {
        var entity = await _uow.Graves.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        _uow.Graves.Update(entity);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<List<GraveMapDto>> GetMapDataAsync(string? section = null)
    {
        var query = _uow.Graves.Query()
            .Include(g => g.DeceasedPersons)
            .Where(g => !g.IsDeleted);

        if (!string.IsNullOrWhiteSpace(section))
            query = query.Where(g => g.Section == section);

        var entities = await query.OrderBy(g => g.Section).ThenBy(g => g.Row).ThenBy(g => g.Column).ToListAsync();
        return _mapper.Map<List<GraveMapDto>>(entities);
    }

    public async Task<List<string>> GetSectionsAsync()
    {
        return await _uow.Graves.Query()
            .Where(g => !g.IsDeleted)
            .Select(g => g.Section)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();
    }

    public async Task<ApiResponse> MarkAvailableAsync(int id, string? userId = null)
    {
        var entity = await _uow.Graves.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted)
            return ApiResponse.Fail("Grave not found.");

        var ageYears = entity.BurialDate.HasValue
            ? (DateTime.UtcNow - entity.BurialDate.Value).TotalDays / 365.25
            : double.MaxValue;

        if (ageYears < 7)
            return ApiResponse.Fail("This grave cannot yet be reused. Minimum 7-year waiting period required.");

        entity.Status = GraveStatus.Available;
        entity.BurialDate = null;
        entity.ExpirationDate = null;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        _uow.Graves.Update(entity);
        await _uow.SaveChangesAsync();
        return ApiResponse.OkResult("Grave marked as available.");
    }

    public async Task<List<GraveDto>> GetExpiringAsync(int withinDays = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        var entities = await _uow.Graves.Query()
            .Include(g => g.DeceasedPersons)
            .Where(g => !g.IsDeleted && g.ExpirationDate != null
                && g.ExpirationDate <= cutoff && g.ExpirationDate >= DateTime.UtcNow)
            .OrderBy(g => g.ExpirationDate)
            .ToListAsync();
        return _mapper.Map<List<GraveDto>>(entities);
    }
}
