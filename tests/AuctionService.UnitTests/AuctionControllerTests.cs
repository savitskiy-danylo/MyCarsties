using AuctionService.Controllers;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AuctionService.RequestHelpers;
using AutoFixture;
using AutoMapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuctionService.UnitTests;

public class AuctionControllerTests
{
  private readonly Mock<IAuctionRepository> _auctionRepo;
  private readonly Mock<IPublishEndpoint> _publishEndpoint;
  private readonly Fixture _fixture;
  private readonly AuctionsController _controller;
  private readonly IMapper _mapper;

  public AuctionControllerTests()
  {
    _fixture = new Fixture();
    _auctionRepo = new Mock<IAuctionRepository>();
    _publishEndpoint = new Mock<IPublishEndpoint>();

    var mapperConf = new MapperConfiguration(mc =>
    {
      mc.AddMaps(typeof(MappingProfiles).Assembly);
    }).CreateMapper().ConfigurationProvider;

    _mapper = new Mapper(mapperConf);
    _controller = new AuctionsController(_auctionRepo.Object, _mapper, _publishEndpoint.Object)
    {
      ControllerContext = new ControllerContext
      {
        HttpContext = new DefaultHttpContext { User = Helpers.GetClaimsPrincipal() }
      }
    };
  }

  [Fact]
  public async Task GetAuctions_WithNoParams_Returns10Auctions()
  {
    var auctions = _fixture.CreateMany<AuctionDto>(10).ToList();
    _auctionRepo.Setup(repo => repo.GetAuctionsAsync(null)).ReturnsAsync(auctions);

    var result = await _controller.GetAllAuctions(null);

    Assert.Equal(10, result.Value.Count);
    Assert.IsType<ActionResult<List<AuctionDto>>>(result);
  }


  [Fact]
  public async Task GetAuctionById_WithValidGuid_ReturnsAuction()
  {
    var auction = _fixture.Create<AuctionDto>();
    _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(auction.Id)).ReturnsAsync(auction);

    var result = await _controller.GetAuctionById(auction.Id);

    Assert.Equal(auction.Make, result.Value.Make);
    Assert.IsType<ActionResult<AuctionDto>>(result);
  }

  [Fact]
  public async Task GetAuctionById_WithInvalidGuid_ReturnsNotFound()
  {
    var auction = _fixture.Create<AuctionDto>();
    _auctionRepo.Setup(repo => repo.GetAuctionByIdAsync(auction.Id)).ReturnsAsync(auction);

    var result = await _controller.GetAuctionById(Guid.NewGuid());

    Assert.Null(result.Value);
    Assert.IsType<NotFoundResult>(result.Result);
  }

  [Fact]
  public async Task CreateAuction_WithValidCreateAuctionDto_ReturnsCreatedAtAction()
  {
    var auction = _fixture.Create<CreateAuctionDto>();

    _auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
    _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

    var result = await _controller.CreateAuction(auction);
    var createdResult = result.Result as CreatedAtActionResult;


    Assert.NotNull(createdResult);
    Assert.Equal("GetAuctionById", createdResult.ActionName);
    Assert.IsType<AuctionDto>(createdResult.Value);
  }

  [Fact]
  public async Task CreateAuction_FailedSave_Returns400BadRequest()
  {
    var auction = _fixture.Create<CreateAuctionDto>();

    _auctionRepo.Setup(repo => repo.AddAuction(It.IsAny<Auction>()));
    _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(false);

    var result = await _controller.CreateAuction(auction);

    Assert.IsType<BadRequestObjectResult>(result.Result);
  }

  [Fact]
  public async Task UpdateAuction_WithUpdateAuctionDto_ReturnsOkResponse()
  {
    var auction = _fixture.Build<Auction>().Without(auction => auction.Item).Create();
    auction.Item = _fixture.Build<Item>().Without(item => item.Auction).Create();
    auction.Seller = "test";

    var auctionDto = _fixture.Create<UpdateAuctionDto>();

    _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(auction.Id)).ReturnsAsync(auction);
    _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

    var result = await _controller.UpdateAuction(auction.Id, auctionDto);

    Assert.IsType<OkResult>(result);
  }

  [Fact]
  public async Task UpdateAuction_WithInvalidUser_Returns403Forbid()
  {
    var auction = _fixture.Build<Auction>().Without(auction => auction.Item).Create();
    auction.Seller = "SomeoneElse";

    var auctionDto = _fixture.Create<UpdateAuctionDto>();

    _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(auction.Id)).ReturnsAsync(auction);

    var result = await _controller.UpdateAuction(auction.Id, auctionDto);

    Assert.IsType<ForbidResult>(result);
  }

  [Fact]
  public async Task UpdateAuction_WithInvalidGuid_ReturnsNotFound()
  {
    var auction = _fixture.Build<Auction>().Without(auction => auction.Item).Create();
    var auctionDto = _fixture.Create<UpdateAuctionDto>();
    _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(auction.Id)).ReturnsAsync(value: null);

    var result = await _controller.UpdateAuction(auction.Id, auctionDto);

    Assert.IsType<NotFoundResult>(result);
  }

  [Fact]
  public async Task DeleteAuction_WithValidUser_ReturnsOkResponse()
  {
    var auction = _fixture.Build<Auction>().Without(auction => auction.Item).Create();
    auction.Seller = "test";
    _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(auction.Id)).ReturnsAsync(auction);
    _auctionRepo.Setup(repo => repo.SaveChangesAsync()).ReturnsAsync(true);

    var result = _controller.DeleteAuction(auction.Id);

    Assert.IsType<OkResult>(result.Result);
  }

  [Fact]
  public async Task DeleteAuction_WithInvalidGuid_Returns404Response()
  {
    var auction = _fixture.Build<Auction>().Without(auction => auction.Item).Create();

    _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(auction.Id)).ReturnsAsync(value: null);

    var result = _controller.DeleteAuction(auction.Id);

    Assert.IsType<NotFoundResult>(result.Result);
  }

  [Fact]
  public async Task DeleteAuction_WithInvalidUser_Returns403Response()
  {
    var auction = _fixture.Build<Auction>().Without(auction => auction.Item).Create();
    auction.Seller = "SomeoneElse";

    _auctionRepo.Setup(repo => repo.GetAuctionEntityByIdAsync(auction.Id)).ReturnsAsync(auction);

    var result = _controller.DeleteAuction(auction.Id);

    Assert.IsType<ForbidResult>(result.Result);
  }
}
