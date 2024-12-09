// Imports

namespace SampleCode
{
    public class ServiceClass : IServiceClass
    {
        private ILogger _logger;
        private Config _config;
        private ICartHeaderRepository _headerRepo;
        private ICartDetailRepository _detailRepo;
        private List<CartItems> cartItemsProcessed = new List<CartItems>();

        public ServiceClass(ServiceCollection services, AppSettingsProviders appSettingsProviders)
        {
            _logger = (Logger)services.BuildServiceProvider().GetService<ILogger>();
            _config = appSettingsProviders.appSettings;
            _headerRepo = (CartHeaderRepository)services.BuildServiceProvider().GetService<ICartHeaderRepository>();
            _detailRepo = (CartDetailRepository)services.BuildServiceProvider().GetService<ICartDetailRepository>();
        }

        public async Task ProcessQueue()
        {
            try
            {
                _logger.LogToProccessLogTable($"Sample Service Started",
                emailAddress: null, eventName: EventEnum.Name.ServiceStarted, taskName: "ProcessQueue", oracleRequestId: null,
                headerId: null, propertyId: null, locationId: null, masterItemId: null, logToFile: true);
                
                // Pick up all carts headers that need processing
                var headersToProcess = _headerRepo.GetAllQueued().OrderBy(h => h.ModifiedDate).ToList();

                if (headersToProcess?.Count > 0)
                {
                    _logger.LogToFile($"ProcessQueue - Carts in queue: {headersToProcess.Count}");

                    foreach (var header in headersToProcess)
                    {
                        try
                        {
                            // Skip Header if it scheduled for later
                            if (header.IsCartScheduled.HasValue && (bool)header.IsCartScheduled && header.ScheduleTimeStamp.HasValue &&
                                DateTimeHelper.ConvertTimestampToDateTime(header.ScheduleTimeStamp) > DateTime.UtcNow)
                                continue;

                            // set all cartItems IsProcessing true
                            _detailRepo.SetIsProcessingByHeaderId(header.Id, true);

                            _logger.LogMicrosImportExportToProccessLogTable($"Sample service start processing cart {header.Id} - {ownerUser.AnetUserId}",
                                emailAddress: submitUser.AnetUserId, eventName: EventEnum.Name.QueueCartBegin, taskName: "ProcessQueue", oracleRequestId: null,
                                headerId: header.Id.ToString(), propertyId: null, locationId: null, masterItemId: null, logToFile: true);

                            await ProcessCart(header.Id, ownerUser: ownerUser, submitUser: submitUser);

                            _logger.LogMicrosImportExportToProccessLogTable($"Sample service finished processing cart {header.Id} - {ownerUser.AnetUserId}",
                                emailAddress: submitUser.AnetUserId, eventName: EventEnum.Name.QueueCartComplete, taskName: "ProcessQueue", oracleRequestId: null,
                                 headerId: header.Id.ToString(), propertyId: null, locationId: null, masterItemId: null, logToFile: true);

                        }
                        catch (Exception exception)
                        {

                            _logger.Error($"Error processing cart with headerId: {header.Id} ex: {exception}", null, arguments: null);
                            continue;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Error($"There was an error in ImportExport.RunImportExportJob : {exception}", null);
                throw;
            }
        }

        public async Task ProcessCart(int headerId, UserEntity ownerUser, UserEntity submitUser)
        {
            try
            {
                var headerInfo = _headerRepo.FindById(headerId);

                // Add any other checks based on versions, client or cart type
                var newItemsProcessed = await _cartProcessClass.BeginCartProcess(headerId, ownerUser);
                if (newItemsProcessed != null)
                {
                    cartItemsProcessed.AddRange(newItemsProcessed);
                }
            }

            catch (Exception ex)
            {
                // set all cartItems to failed
                _detailRepo.SetSubmitFailedByHeaderId(headerId);

                _logger.LogMicrosImportExportToProccessLogTable($"ProcessCart call to _cartProcessClass.BeginCartProcess was not successful for {ownerUser?.AnetUserId} - {headerId}, Error: {ex}",
                    userEntity?.AnetUserId, EventEnum.Name.QueueCartBegin, "ProcessCart", null, headerId.ToString(), null, null, null);
            }

            // Update status to reflect cart complete and ready to send email then move to History/Archive  (old step 7.)
            if (_headerRepo.SubmitStatusUpdate(ownerUser, headerId, CartHeaderStatus.COMPLETE, submitUser) < 1)
            {
                _logger.Debug($"ProcessCart call to headerRepo.SubmitStatusUpdate Complete was not successful for {ownerUser?.AnetUserId} - {headerId}", userEntity?.AnetUserId);
            }

            // Send email to user
            SendCartProcessingCompleteEmail(headerId, submitUser: submitUser, ownerUser: ownerUser);

            // Archive Cart
            _logger.Info("Archiving Cart for {0} - {1}", ownerUser.AnetUserId, headerId);
            _headerRepo.Archive(ownerUser);
            _logger.Info("Completed Archiving for {0} - {1}", ownerUser.AnetUserId, headerId);
        }

        private void SendCartProcessingCompleteEmail(int headerId, UserEntity submitUser, UserEntity ownerUser)
        {
            //Logic to send out emails to users 
        }
    }
}