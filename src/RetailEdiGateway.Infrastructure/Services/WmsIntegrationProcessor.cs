using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using RetailEdiGateway.Application.Common.Interfaces;
using RetailEdiGateway.Core.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RetailEdiGateway.Infrastructure.Services
{
 /// <summary>
 /// Background service to synchronize warehouse slot bookings with the external WMS system.
 /// This fulfills the "GW -> WMS" integration flow described in the project documentation.
 /// </summary>
 public class WmsIntegrationProcessor : BackgroundService
 {
 private readonly IServiceScopeFactory _scopeFactory;
 private readonly ILogger<WmsIntegrationProcessor> _logger;

 /// <summary>
 /// Initializes a new instance of the <see cref="WmsIntegrationProcessor"/> class.
 /// </summary>
 public WmsIntegrationProcessor(IServiceScopeFactory scopeFactory, ILogger<WmsIntegrationProcessor> logger)
 {
 _scopeFactory = scopeFactory;
 _logger = logger;
 }

 /// <inheritdoc />
 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
 _logger.LogInformation("WMS Integration Processor starting.");

 while (!stoppingToken.IsCancellationRequested)
 {
 try
 {
 await SynchronizeSlotsWithWmsAsync(stoppingToken);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error occurred during WMS synchronization.");
 }

 await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
 }

 _logger.LogInformation("WMS Integration Processor stopping.");
 }

 /// <summary>
 /// Identifies unsynchronized warehouse slots and "transmits" them to the external WMS.
 /// </summary>
 private async Task SynchronizeSlotsWithWmsAsync(CancellationToken cancellationToken)
 {
 using var scope = _scopeFactory.CreateScope();
 var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

 var unsyncedSlots = await context.WarehouseSlots
 .Include(s => s.PurchaseOrderLine)
 .ThenInclude(l => l.PurchaseOrder)
 .Where(s => !s.IsSyncedToWms && s.Status == WarehouseSlotStatus.Booked)
 .ToListAsync(cancellationToken);

 if (!unsyncedSlots.Any()) return;

 _logger.LogInformation("Found {Count} warehouse slots to synchronize with WMS.", unsyncedSlots.Count);

 foreach (var slot in unsyncedSlots)
 {
 _logger.LogInformation("Syncing Slot {SlotId} (PO: {Po}) to WMS at {DcCode} Bay {Bay}.", 
 slot.Id, slot.PurchaseOrderLine.PurchaseOrder.ErpOrderNumber, slot.DcCode, slot.BayNumber);

 // Simulation: External WMS REST API Call
 // In production, this would be: await _wmsClient.CreateBooking(slot);
 
 // Simulate success
 slot.IsSyncedToWms = true;
 _logger.LogInformation("Slot {SlotId} successfully registered in WMS.", slot.Id);
 }

 await context.SaveChangesAsync(cancellationToken);
 }
 }
}
