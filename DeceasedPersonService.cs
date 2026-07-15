using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CEMETRIX.Application.Common;
using CEMETRIX.Application.DTOs.Deceased;
using CEMETRIX.Application.Interfaces.Repositories;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CEMETRIX.Application.Services;

public class DeceasedPersonService : IDeceasedPersonService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public DeceasedPersonService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PagedResult<DeceasedPersonDto>> GetPagedAsync(DeceasedFilterDto filter)
    {
        var query = _uow.Deceased.Query()
            .Include(d => d.Grave)
            .Where(d => !d.IsDeleted);

        if (filter.GraveId.HasValue)
            query = query.Where(d => d.GraveId == filter.GraveId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Section))
            query = query.Where(d => d.Grave!.Section == filter.Section);
        if (filter.BurialYear.HasValue)
            query = query.Where(d => d.DOD.Year == filter.BurialYear.Value);
        if (filter.DOBFrom.HasValue) query = query.Where(d => d.DOB >= filter.DOBFrom.Value);
        if (filter.DOBTo.HasValue) query = query.Where(d => d.DOB <= filter.DOBTo.Value);
        if (filter.DODFrom.HasValue) query = query.Where(d => d.DOD >= filter.DODFrom.Value);
        if (filter.DODTo.HasValue) query = query.Where(d => d.DOD <= filter.DODTo.Value);
        if (filter.BurialType.HasValue) query = query.Where(d => d.BurialType == filter.BurialType.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(d => d.FullName.Contains(s) || (d.Grave != null && d.Grave.GraveNumber.Contains(s)));
        }

        query = (filter.SortBy?.ToLower()) switch
        {
            "dod" => filter.SortDescending ? query.OrderByDescending(x => x.DOD) : query.OrderBy(x => x.DOD),
            "dob" => filter.SortDescending ? query.OrderByDescending(x => x.DOB) : query.OrderBy(x => x.DOB),
            "gravenumber" => filter.SortDescending ? query.OrderByDescending(x => x.Grave!.GraveNumber) : query.OrderBy(x => x.Grave!.GraveNumber),
            "createdat" => filter.SortDescending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt),
            "name" => filter.SortDescending ? query.OrderByDescending(x => x.FullName) : query.OrderBy(x => x.FullName),
            _ => query.OrderByDescending(x => x.CreatedAt),
        };

        var total = await query.CountAsync();
        var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
        return new PagedResult<DeceasedPersonDto>
        {
            Items = _mapper.Map<List<DeceasedPersonDto>>(items),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<DeceasedPersonDto?> GetByIdAsync(int id)
    {
        var entity = await _uow.Deceased.Query().Include(d => d.Grave).FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);
        return entity == null ? null : _mapper.Map<DeceasedPersonDto>(entity);
    }

    public async Task<DeceasedPersonDto?> GetByGraveIdAsync(int graveId)
    {
        var entity = await _uow.Deceased.Query().Include(d => d.Grave)
            .Where(d => d.GraveId == graveId && !d.IsDeleted)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync();
        return entity == null ? null : _mapper.Map<DeceasedPersonDto>(entity);
    }

    public async Task<DeceasedPersonDto> CreateAsync(CreateDeceasedPersonDto dto, string? userId = null)
    {
        var entity = _mapper.Map<DeceasedPerson>(dto);
        entity.CreatedBy = userId;
        await _uow.Deceased.AddAsync(entity);

        var grave = await _uow.Graves.GetByIdAsync(dto.GraveId);
        if (grave != null)
        {
            grave.Status = Domain.Enums.GraveStatus.Occupied;
            grave.BurialDate = entity.DOD;
            grave.ExpirationDate ??= entity.DOD.AddYears(7);
            _uow.Graves.Update(grave);
        }
        await _uow.SaveChangesAsync();
        var saved = await _uow.Deceased.Query().Include(d => d.Grave).FirstOrDefaultAsync(d => d.Id == entity.Id);
        return _mapper.Map<DeceasedPersonDto>(saved ?? entity);
    }

    public async Task<DeceasedPersonDto?> UpdateAsync(UpdateDeceasedPersonDto dto, string? userId = null)
    {
        var entity = await _uow.Deceased.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return null;
        entity.FullName = dto.FullName;
        entity.DOB = dto.DOB;
        entity.DOD = dto.DOD;
        entity.Gender = dto.Gender;
        entity.Religion = dto.Religion;
        entity.BurialType = dto.BurialType;
        entity.PhotoUrl = dto.PhotoUrl;
        entity.FamilyContact = dto.FamilyContact;
        entity.MemorialNotes = dto.MemorialNotes;
        entity.Nationality = dto.Nationality;
        entity.CauseOfDeath = dto.CauseOfDeath;
        entity.GraveId = dto.GraveId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        _uow.Deceased.Update(entity);
        await _uow.SaveChangesAsync();
        return _mapper.Map<DeceasedPersonDto>(entity);
    }

    public async Task<bool> DeleteAsync(int id, string? userId = null)
    {
        var entity = await _uow.Deceased.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        _uow.Deceased.Update(entity);
        await _uow.SaveChangesAsync();
        return true;
    }
}
