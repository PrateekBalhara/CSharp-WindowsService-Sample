namespace SampleCode
{
    public class CartProcessClass : ICartProcessClass
    {
        // Global Varibales
        private ILogger _logger;
        private IDefinitionRepository_definitionRepo;
        private ICNCService _cncService;

        private string CNCAccessToken;
        private CNCClientInfo CNCClient;
        private UserEntity user;

        public CartProcessClass(ILogger logger, IDefinitionRepository definitionRepository, AppSettingsProviders appSettingsProviders)
        {
            _logger = logger;
            _definitionRepo = definitionRepository;
            _cNCClientInfoRepository = cNCClientInfoRepository;
            ..
            _cncService = new CNCService(_smtpClient, _logger, _cNCClientInfoRepository, appSettingsProviders);
        }

        private void SetOwnerUser(UserEntity ownerUser)
        {
            user = ownerUser;
        }

        /**
         * Set Access Token required to call CNC service. The token can be reused
        **/
        private async Task SetCNCAccessToken()
        {
            CNCAuth CNCAuthResponse = await _cncService.GetCNCAuthToken();
            CNCAccessToken = CNCAuthResponse.AuthToken;
        }

        /**
         * Checks if Definition Needs to be Updated
        **/
        private bool DoesDefinitionNeedsUpdation(CartItem cartItem)
        {
            bool definitionNeedsUpdation = cartItem.OriginalOnMenu != cartItem.OnMenu;
            if (cartItem.IsCPG)
            {
                definitionNeedsUpdation = definitionNeedsUpdation || (cartItem.OriginalSLUId != cartItem.SLUId)
                    || cartItem.OriginalSLU3Id != cartItem.SLU3Id || cartItem.OriginalIsSLB != cartItem.IsSLB;
            }
            return definitionNeedsUpdation;
        }

        /**
         * Process an individual cart submitted by an user
        **/
        public async Task<List<CartItem>> BeginCartProcess(int headerId, UserEntity user)
        {
            var list = new List<CartItem>();
            try
            {
                SetOwnerUser(user);

                var cartItems = _cartItemRepo.GetAllByUserDetailStatus(user.AnetUserId, cartId: headerId, detailStatus: MenuItemChangeDetailStatus.SUBMIT);

                if (cartItems == null || !cartItems.Any())
                {
                    _logger.LogMicrosImportExportToProccessLogTable($"{ServiceLoggingConstant.ProcessName}- No cartItems found for HeaderId: {headerId}",
                        user?.UserEmail, EventEnum.Name.ServiceProcessing, $"{ServiceLoggingConstant.ProcessName}- Cart Items Not Found", null, headerId.ToString(), null, null, null);
                    return null;
                }

                // Set CNC Access Token
                if (string.IsNullOrEmpty(CNCAccessToken)) await SetCNCAccessToken();

                list = await ProcessCartItems(newCPGCartItems);


            }
            catch (Exception ex)
            {
                _logger.Error($"BeginCartProcess Error processing cart with headerId: {headerId} ex: {ex}", null);
                _logger.LogMicrosImportExportToProccessLogTable(
                        message: string.Format("{0}- BeginCartProcess Error processing cart with headerId: {1} ex: {2}",
                           ServiceLoggingConstant.ProcessName, headerId, ex),
                        emailAddress: user.UserEmail,
                        eventName: EventEnum.Name.ServiceProcessing,
                        taskName: ServiceLoggingConstant.ProcessName,
                        oracleRequestId: null,
                        headerId: headerId.ToString(),
                        propertyId: null,
                        locationId: null,
                        masterItemId: null);
            }
            return list;
        }

        /**
         * Loops over each cart item and process them
        **/
        private async Task ProcessCartItems(int headerId, List<CartItem> cartItems)
        {
            // Fetch master data from DB
            List<long> definitionIds = cartItems
                .Where(cartItem => cartItem.Id > 0)
                .Select(cartItem => cartItem.Id).ToList();
            IEnumerable<Definition> definitions = _definitionRepo.FindAll(definitionIds);

            List<long> priceIds = cartItems.Where(cartItem => cartItem.priceId > 0)
                .Select(cartItem => cartItem.priceId).ToList();
            IEnumerable<CNCMenuItemsPriceObject> cncPrices = await _cncService.GetCNCMenuItemPriceObjectByIds(priceIds, CNCAccessToken);

            foreach (var cartItem in cartItems)
            {
                try
                {
                    // Process relevant data
                    Definition? definition = GetMatchingDefinitionExtRecord(definitions, cartItem);
                    if (definition == null)
                        throw new Exception("Cart item could not be matched to any Menu Item Definitions.");
                    if (DoesDefinitionNeedsUpdation(definition))
                        await ProcessCartDefinition(definition);

                    // Process price in similar fashion

                    //CNCMenuItemsPriceObject? cncPrice = cncPrices.FirstOrDefault(p => p.menuItemPriceID == cartItem.priceId);
                    //if(DoesPriceNeedsUpdation(cncPrice))
                        //await ProcessCartPrice(cncPrice)
                    
                    
                }
                catch (Exception ex)
                {
                    _logger.Error($"{ServiceLoggingConstant.ProcessName} -Error while processing existing CNC cartItem with headerId: {cartItem.HeaderId}, detailId:{cartItem.DetailId} ex: {ex}", null);
                    Dictionary<string, object> logObject = new Dictionary<string, object> {
                        { "headerId", headerId },
                        { "definitions", definitions },
                        { "cartItem", cartItem },
                        };
                    _logger.LogMoreInfoToDatabase($"{ServiceLoggingConstant.ProcessName} - Error while updating existing CNC cart item with DetailId: {cartItem.DetailId}. Exception: {ex}",
                        logObject, user.UserEmail, EventEnum.Name.CNCServiceProcessing, ServiceLoggingConstant.ProcessName, cartItem.PropertyId.ToString(),
                        cartItem.LocationId.ToString(), cartItem.ProductId?.ToString(), cartItem.HeaderId.ToString());
                    _menuItemChangeDetailRepo.SetSubmitFailed(cartItem.DetailId);
                    continue;
                }
                _menuItemChangeDetailRepo.SetIsProcessing(cartItem.DetailId, false);
            }

        }

        /**
         * Process cart item definition and Syncs the data with CNC service
        **/
        private async Task<Definition> ProcessCartDefinition(Definition definition)
        {
            // PreProcess the definition 
            CNCMenuItemsDefinitionObject requestObject = CreateCNCDefinitionUpdateRequest(cartItem, cncDefinition);

            // Update/Insert definition in the CNC service
            MenuItemsDefinitionResponse response = await _cncService.UpdateMenuItemDefinition(requestObject, CNCAccessToken);
            if (response == null || !response.success)
                throw new Exception($"Error in Update Definition Record for {requestObject.menuItemDefinitionId}. exception: {response.title} -{response.errorDetails}");

            // Update data in our DB
            definition = UpdateDefinitionInDBFromCNCRequestObject(definition, requestObject);
            
            // Add relevant logs for processing, request, response and successful processing

            return definition;
        }

    }
}
