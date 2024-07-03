using AuctionService.Entities;

namespace AuctionService.UnitTests;

public class AuctionEntityTests
{
  [Fact]
  public void HasReservePrice_ReservePriceGreaterZero_True()
  {
    var auction = new Auction { Id = Guid.NewGuid(), ReservePrice = 10 };

    var result = auction.HasReservePrice();

    Assert.True(result);
  }

  [Fact]
  public void HasReservePrice_ReservePriceIsZero_False()
  {
    var auction = new Auction { Id = Guid.NewGuid(), ReservePrice = 0 };

    var result = auction.HasReservePrice();

    Assert.False(result);
  }
}