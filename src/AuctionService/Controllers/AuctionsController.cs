using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuctionService.Controllers
{
  [ApiController]
  [Route("api/auctions")]
  public class AuctionsController : ControllerBase
  {
    private readonly IAuctionRepository _auctionRepository;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint _publishEndpoint;

    public AuctionsController(IAuctionRepository auctionRepository, IMapper mapper, IPublishEndpoint publishEndpoint)
    {
      _auctionRepository = auctionRepository;
      _mapper = mapper;
      _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions(string date)
    {
      return await _auctionRepository.GetAuctionsAsync(date);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
    {
      var auction = await _auctionRepository.GetAuctionByIdAsync(id);

      if (auction == null) return NotFound();

      return auction;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
    {
      var auction = _mapper.Map<Auction>(auctionDto);

      auction.Seller = User.Identity.Name;

      _auctionRepository.AddAuction(auction);

      var newAuction = _mapper.Map<AuctionDto>(auction);

      await _publishEndpoint.Publish(_mapper.Map<AuctionCreated>(newAuction));

      var result = await _auctionRepository.SaveChangesAsync();

      if (!result) return BadRequest("Could not save changes to the DB");

      return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, newAuction);

    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
    {
      var auction = await _auctionRepository.GetAuctionEntityByIdAsync(id);

      if (auction == null) return NotFound();

      if (auction.Seller != User.Identity.Name) return Forbid();

      auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
      auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
      auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
      auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
      auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

      await _publishEndpoint.Publish(_mapper.Map<AuctionUpdated>(auction));

      var result = await _auctionRepository.SaveChangesAsync();

      if (result) return Ok();
      return BadRequest("Problem saving changes");
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAuction(Guid id)
    {
      var auction = await _auctionRepository.GetAuctionEntityByIdAsync(id);

      if (auction == null) return NotFound();

      if (auction.Seller != User.Identity.Name) return Forbid();

      _auctionRepository.RemoveAuction(auction);

      await _publishEndpoint.Publish(_mapper.Map<AuctionDeleted>(auction));

      var result = await _auctionRepository.SaveChangesAsync();

      if (!result) return BadRequest("Could not update DB");

      return Ok();
    }
  }
}