//Import

namespace SampleCode
{
	public class CNCService : ICNCService
	{
		/**
		 * Constructor: Setups the cncAuthClient, httpClient, baseUrl etc
		**/
		public CNCService(ILogger logger, ICNCClientInfoRepository cNCClientInfoRepository, AppSettingsProviders appSettingsProviders)
		{
			_smtpClient = smtpClient;
			_logger = logger;
			_cNCConfig = new CNCConfiguration(appSettingsProviders);
			_iCNCClientInfoRepository = cNCClientInfoRepository;
			_cncClient = _iCNCClientInfoRepository.GetCNCClientInfo(_cNCConfig.Settings.CNCEnvironment);
			_cncAuthService = new CNCAuthClient(_smtpClient, _logger, _iCNCClientInfoRepository, appSettingsProviders);
			_httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(2000) };
			_httpClient.DefaultRequestHeaders.Add("Simphony-OrgShortName", _cncClient.APIOrgCode);
			_baseApiURL = $"{_cNCConfig.Settings.CNCServiceURL}/config/sim/v{_cncClient.APIVersion}";
		}

		/**
		 * Returns Auth Token for CNC service
		**/
		public async Task<CNCAuth> GetCNCAuthToken() => await _cncAuthService.GetCNCAPIAccessToken(_cncClient);

		/**
		 * Fetches all Price data from CNC service
		**/
		public async Task<List<CNCMenuItemPrice>> GetAllMenuPrice(string accessKey)
		{
			return await GetAllDataWithPagination<CNCMenuItemPrice>(accessKey, "items/getMenuItemPrices", DataSyncConstant.FetchAllMenuItemPriceDataFailed);
		}

		/**
		 * Fetches all Definition Data from CNC service
		**/
		public async Task<List<CNCMenuItemDefinitionVM>> GetAllMenuDefinition(string accessKey)
		{
			return await GetAllDataWithPagination<CNCMenuItemDefinitionVM>(accessKey, "items/getMenuItemDefinitions", DataSyncConstant.FetchAllDefinitionDataFailed);
		}

		/**
		 * Breaks down the data to be fetched in Batches
		**/
		private async Task<List<T>> GetAllDataWithPagination<T>(string accessKey, string endpoint, string errorEventName)
		{
			var allItems = new List<T>();
			int limit = _cNCConfig.Settings.CNCFetchLimit;
			int offset = 0;
			bool hasMore = true;
			int totalCount = 0;
			while (hasMore)
			{
				var paginatedItems = await GetPaginatedData<T>(accessKey, endpoint, limit, offset, errorEventName);
				if (paginatedItems == null || !paginatedItems.success)
				{
					throw new Exception("Failed to fetch CNC data.");
				}
				allItems.AddRange(paginatedItems.items);
				offset += limit;
				hasMore = paginatedItems.hasMore;
				totalCount = paginatedItems.totalResults;
			}

			return allItems.Count > 0 && totalCount == allItems.Count ? allItems : new List<T>();
		}

		/**
		 * Function to do API calls and get the Data
		 * In case of any error in that may occur during the API call, 
		 *	the logic would retry the call upto 'maxRetries' times before throwing an error. 
		 *	Each api call would be deplayed by 1 sec.
		**/
		private async Task<CNCGenericSearchModel<T>> GetPaginatedData<T>(string accessKey, string endpoint, int limit,
			int offset, string errorEventName, int maxRetries = 2)
		{
			int retryCount = 0;
			// Logic to retry api calls
			while (retryCount < maxRetries)
			{
				try
				{
					string apiUrl = $"{_baseApiURL}/{endpoint}";
					var request = new { Limit = limit, Offset = offset };
					var requestObject = JsonConvert.SerializeObject(request);
					var content = new StringContent(requestObject, Encoding.UTF8, "application/json");
					_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessKey);
					HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, content);
					string responseBody = await response.Content.ReadAsStringAsync();
					if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(responseBody))
					{
						var result = JsonConvert.DeserializeObject<CNCGenericSearchModel<T>>(responseBody);
						result.success = true;
						return result;
					}
					else
					{
						_logger.LogToDataSyncDatabase(DataSyncEventEnum.Name.Error,
							$"Error in Fetching CNC Data. Response- {responseBody}.", DataSyncEventEnum.Name.Error,
						errorEventName, false, null);
						break;
					}
				}
				catch (HttpRequestException ex)
				{
					if (retryCount == maxRetries - 1)
					{
						return new CNCGenericSearchModel<T> { success = false, errorDetails = ex.ToString() };
					}
				}
				retryCount++;
				// Delay each call to prevent network from being flooded.
				await Task.Delay(1000);
			}


			return new CNCGenericSearchModel<T> { success = false };
		}


	}
}