using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Data;

public class AuctionRepository : IAuctionRepository
{
  private readonly AuctionDbContext _dbContext;
  private readonly IMapper _mapper;

  public AuctionRepository(AuctionDbContext dbContext, IMapper mapper)
  {
    _dbContext = dbContext;
    _mapper = mapper;
  }

  public void AddAuction(Auction auction)
  {
    _dbContext.Auctions.Add(auction);
  }

  public async Task<AuctionDto> GetAuctionByIdAsync(Guid id)
  {
    return await _dbContext.Auctions
      .ProjectTo<AuctionDto>(_mapper.ConfigurationProvider)
      .FirstOrDefaultAsync(x => x.Id == id);
  }

  public async Task<List<AuctionDto>> GetAuctionsAsync(string date)
  {
    var query = _dbContext.Auctions.OrderBy(x => x.Item.Make).AsQueryable();

    if (!string.IsNullOrWhiteSpace(date))
    {
      query = query.Where(x => x.UpdatedAt.CompareTo(DateTime.Parse(date).ToUniversalTime()) > 0);
    }

    return await query.ProjectTo<AuctionDto>(_mapper.ConfigurationProvider).ToListAsync();
  }

  public async Task<Auction> GetAuctionEntityByIdAsync(Guid id)
  {
    return await _dbContext.Auctions
        .Include(x => x.Item)
        .FirstOrDefaultAsync(x => x.Id == id);
  }

  public void RemoveAuction(Auction auction)
  {
    _dbContext.Auctions.Remove(auction);
  }

  public async Task<bool> SaveChangesAsync()
  {
    return await _dbContext.SaveChangesAsync() > 0;
  }
}