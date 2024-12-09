namespace SampleCode
{
    public class CartProcessClass : ICartProcessClass
    {
        private ILogger _logger;
        private Config _config;

        private string CNCAccessToken;
        private CNCClientInfo CNCClient;
        private UserEntity user;

        public CartProcessClass(ILogger logger, ICartMenuItemRepository cartMenuItemRepository, IMasterItemRepository masterItemRepository,
            IDefinitionRepository definitionRepository, IPriceRepository priceRepository, IBarcodeRepository barcodeRepository, ILocationHierarchyRepository locationHierarchyRepository,
            IMenuItemChangeDetailRepository menuItemChangeDetailRepository, IAPIRequestRepository aPIRequestRepository, IFamilyGroupRepository familyGroupRepository
            , ISmtpClient smtpClient, ICNCClientInfoRepository cNCClientInfoRepository, ITaxRepository taxRepository, AppSettingsProviders appSettingsProviders)
        {

        }

        private void SetOwnerUser(UserEntity ownerUser)
        {
            user = ownerUser;
        }

        private async Task SetCNCAccessToken()
        {
            CNCAuth CNCAuthResponse = await _cncService.GetCNCAuthToken();
            CNCAccessToken = CNCAuthResponse.AuthToken;
        }

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

                list = await CNCProcessCartItems(newCPGCartItems);


            }
            catch (Exception ex)
            {
                _logger.Error($"BeginCNCImportProcess Error processing cart with headerId: {headerId} ex: {ex}", null);
                _logger.LogMicrosImportExportToProccessLogTable(
                        message: string.Format("{0}- BeginCNCImportProcess Error processing cart with headerId: {1} ex: {2}",
                           CNCServiceLoggingConstant.ProcessName, headerId, ex),
                        emailAddress: user.UserEmail,
                        eventName: EventEnum.Name.CNCServiceProcessing,
                        taskName: CNCServiceLoggingConstant.ProcessName,
                        oracleRequestId: null,
                        headerId: headerId.ToString(),
                        propertyId: null,
                        locationId: null,
                        masterItemId: null);
            }
            return list;
        }
    }
}
