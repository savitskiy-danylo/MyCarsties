using System.Net;
using System.Net.Http.Json;
using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.IntegrationTests.Fixtures;
using AuctionService.IntegrationTests.Util;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionService.IntegrationTests;

[Collection("Shared collection")]
public class AuctionControllerTests : IAsyncLifetime
{
  private readonly CustomWebAppFactory _factory;
  private readonly HttpClient _httpClient;
  private const string GT_ID = "afbee524-5972-4075-8800-7d1f9d7b0a0c";

  public AuctionControllerTests(CustomWebAppFactory factory)
  {
    _factory = factory;
    _httpClient = factory.CreateClient();
  }

  [Fact]
  public async Task GetAuctions_Return10Auctions()
  {
    var response = await _httpClient.GetFromJsonAsync<List<AuctionDto>>("api/auctions");

    Assert.Equal(10, response.Count);
  }

  [Fact]
  public async Task GetAuctionById_WithValidId_ReturnAuction()
  {
    var response = await _httpClient.GetFromJsonAsync<AuctionDto>($"api/auctions/{GT_ID}");

    Assert.Equal("GT", response.Model);
  }

  [Fact]
  public async Task GetAuctionById_WithInvalidId_Return404()
  {
    var response = await _httpClient.GetAsync($"api/auctions/{Guid.NewGuid()}");

    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }

  [Fact]
  public async Task GetAuctionById_WithInvalidGUID_Return400()
  {
    var response = await _httpClient.GetAsync($"api/auctions/NOT-A-GUID");

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task CreateAuction_WithNoAuth_Return401()
  {
    var auction = new CreateAuctionDto { Make = "test" };

    var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

    Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task CreateAuction_WithAuth_Return201()
  {
    var auction = GetAuctionDto();
    _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

    var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

    response.EnsureSuccessStatusCode();
    Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
    var createdAuction = await response.Content.ReadFromJsonAsync<AuctionDto>();
    Assert.Equal("bob", createdAuction.Seller);
  }

  [Fact]
  public async Task CreateAuction_WithInvalidCreateAuctionDto_ShouldReturn400()
  {
    var auction = GetAuctionDto();
    auction.Make = null;
    _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

    var response = await _httpClient.PostAsJsonAsync($"api/auctions", auction);

    Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task UpdateAuction_WithValidUpdateDtoAndUser_ShouldReturn200()
  {
    var auction = new UpdateAuctionDto { Make = "Updated" };
    _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("bob"));

    var response = await _httpClient.PutAsJsonAsync($"api/auctions/{GT_ID}", auction);

    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task UpdateAuction_WithValidUpdateDtoAndInvalidUser_ShouldReturn403()
  {
    var auction = new UpdateAuctionDto { Make = "Updated" };
    _httpClient.SetFakeJwtBearerToken(AuthHelper.GetBearerForUser("SomeoneElse"));

    var response = await _httpClient.PutAsJsonAsync($"api/auctions/{GT_ID}", auction);

    Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
  }

  private CreateAuctionDto GetAuctionDto()
  {
    return new CreateAuctionDto
    {
      Make = "test",
      Model = "testModel",
      ImageUrl = "test",
      Color = "test",
      Mileage = 10,
      Year = 10,
      ReservePrice = 10
    };
  }

  public Task InitializeAsync() => Task.CompletedTask;
  public Task DisposeAsync()
  {
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
    DbHelper.ReinitDbForTest(db);
    return Task.CompletedTask;
  }

}