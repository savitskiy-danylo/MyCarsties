using Contracts;
using MassTransit;
using MongoDB.Entities;
using SearchService.Models;
namespace SearchService.Consumers;

public class AuctionDeletedConsumer : IConsumer<AuctionDeleted>
{
  public async Task Consume(ConsumeContext<AuctionDeleted> context)
  {
    System.Console.WriteLine($"--> Consuming auciton deleted: {context.Message.Id}");

    await DB.DeleteAsync<Item>(context.Message.Id);
  }
}