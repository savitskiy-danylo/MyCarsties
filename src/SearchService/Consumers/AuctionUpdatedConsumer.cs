using AutoMapper;
using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;

namespace SearchService.Consumers;

public class AuctionUpdatedConsumer : IConsumer<AuctionUpdated>
{
  private readonly IMapper _mapper;

  public AuctionUpdatedConsumer(IMapper mapper)
  {
    _mapper = mapper;
  }
  public async Task Consume(ConsumeContext<AuctionUpdated> context)
  {
    System.Console.WriteLine($"--> Consuming auction updated: {context.Message.Id}");

    await DB.Update<Item>()
      .MatchID(context.Message.Id)
      .ModifyOnly(b => new { b.Make, b.Model, b.Color, b.Mileage, b.Year }, _mapper.Map<Item>(context.Message))
      .ExecuteAsync();
  }
}