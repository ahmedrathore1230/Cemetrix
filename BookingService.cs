using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CEMETRIX.Application.Common;
using CEMETRIX.Application.DTOs.Bookings;
using CEMETRIX.Application.Interfaces.Repositories;
using CEMETRIX.Application.Interfaces.Services;
using CEMETRIX.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CEMETRIX.Application.Services;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public BookingService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<PagedResult<BookingDto>> GetPagedAsync(BookingFilterDto filter)
    {
        var query = _uow.Bookings.Query()
            .Include(b => b.User)
            .Include(b => b.Grave)
            .Where(b => !b.IsDeleted);

        if (filter.PaymentStatus.HasValue) query = query.Where(b => b.PaymentStatus == filter.PaymentStatus.Value);
        if (filter.FromDate.HasValue) query = query.Where(b => b.BookingDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(b => b.BookingDate <= filter.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.UserId)) query = query.Where(b => b.UserId == filter.UserId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            query = query.Where(b =>
                (b.ReferenceNumber != null && b.ReferenceNumber.Contains(s)) ||
                (b.ContactPerson != null && b.ContactPerson.Contains(s)) ||
                (b.Grave != null && b.Grave.GraveNumber.Contains(s)));
        }

        query = (filter.SortBy?.ToLower()) switch
        {
            "amount" => filter.SortDescending ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            "status" => filter.SortDescending ? query.OrderByDescending(x => x.PaymentStatus) : query.OrderBy(x => x.PaymentStatus),
            _ => filter.SortDescending ? query.OrderByDescending(x => x.BookingDate) : query.OrderBy(x => x.BookingDate),
        };

        var total = await query.CountAsync();
        var items = await query.Skip((filter.PageNumber - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();
        return new PagedResult<BookingDto>
        {
            Items = _mapper.Map<List<BookingDto>>(items),
            TotalCount = total,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<BookingDto?> GetByIdAsync(int id)
    {
        var entity = await _uow.Bookings.Query().Include(b => b.User).Include(b => b.Grave).FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);
        return entity == null ? null : _mapper.Map<BookingDto>(entity);
    }

    public async Task<BookingDto> CreateAsync(CreateBookingDto dto, string? userId = null)
    {
        var entity = _mapper.Map<Booking>(dto);
        entity.UserId = dto.UserId ?? userId ?? string.Empty;
        entity.ReferenceNumber = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        entity.CreatedBy = userId;
        await _uow.Bookings.AddAsync(entity);
        await _uow.SaveChangesAsync();
        var saved = await _uow.Bookings.Query().Include(b => b.User).Include(b => b.Grave).FirstOrDefaultAsync(b => b.Id == entity.Id);
        return _mapper.Map<BookingDto>(saved ?? entity);
    }

    public async Task<BookingDto?> UpdateAsync(UpdateBookingDto dto, string? userId = null)
    {
        var entity = await _uow.Bookings.GetByIdAsync(dto.Id);
        if (entity == null || entity.IsDeleted) return null;
        entity.GraveId = dto.GraveId;
        entity.BookingDate = dto.BookingDate;
        entity.PaymentStatus = dto.PaymentStatus;
        entity.Amount = dto.Amount;
        entity.Notes = dto.Notes;
        entity.ContactPerson = dto.ContactPerson;
        entity.ContactPhone = dto.ContactPhone;
        entity.ContactEmail = dto.ContactEmail;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        _uow.Bookings.Update(entity);
        await _uow.SaveChangesAsync();
        return _mapper.Map<BookingDto>(entity);
    }

    public async Task<bool> DeleteAsync(int id, string? userId = null)
    {
        var entity = await _uow.Bookings.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = userId;
        _uow.Bookings.Update(entity);
        await _uow.SaveChangesAsync();
        return true;
    }
}
