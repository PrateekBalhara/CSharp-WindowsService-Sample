// Imports


namespace SampleCode
{
    public class SampleService : IHostedService, IDisposable
    {
        private Config _config;
        private ServiceCollection _serviceCollection;
        private AppSettingsProviders _appSettingsProviders;
        private Task processTask;
        private Timer timer;
        private Logger logger;
        private bool running;
        private int interval;

        public ImportExportService(ServiceCollection services, AppSettingsProviders appSettingsProviders)
        {

            _serviceCollection = services;
            _config = appSettingsProviders.appSettings;
            _appSettingsProviders = appSettingsProviders;
            logger = (Logger)services.BuildServiceProvider().GetService<ILogger>();
        }

        public void Dispose()
        {
        }

        private void LogToConsole(string message)
        {
            if (Environment.UserInteractive)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{DateTime.Now} | {message}");
                Console.ResetColor();
            }
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                running = false;
                logger.Debug($"Sample Service started", null, arguments: null);

                interval = _config.ImpExpFreq;
                double msInterval = interval * 60000;
                this.timer = new Timer(msInterval) { AutoReset = true };

                // run when interval has elapsed
                this.timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);

                // start timer 
                timer.Start();

                // run when service first starts
                processTask = Task.Run(() => Run()).ContinueWith(ProcessingComplete);

            }
            catch (Exception ex)
            {
                logger.Error("Error starting Sample Service: " + ex.ToString(), ex, null, null);
                throw;
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            processTask = Task.Run(() => Run()).ContinueWith(ProcessingComplete);
        }

        private void ProcessingComplete(Task completedTask)
        {
            processTask = null;
        }

        private async void Run()
        {
            try
            {
                // Mutex/Lock to prevent service from running again until finished
                if (!running)
                {
                    running = true;
                    LogToConsole($"Start running new task ProcesssQueue");
                    
                    if(_config.RunSampleTask != null && _config.RunSampleTask)
                    {
                        // run Sample task 1
                        ServiceClass _serviceClass = new ServiceClass(_serviceCollection, _appSettingsProviders);
                        await _serviceClass.ProcessQueue();
                    }

                    // Run Follow up tasks
                    if (_config.RunAnotherSampleTask != null && _config.RunAnotherSampleTask)
                    {
                        // run Sample task 2
                        ServiceClass2 _serviceClass2 = new ServiceClass2(_serviceCollection, _appSettingsProviders);
                        _serviceClass2.sampleTask2();
                    }
                    running = false;
                }
            }
            catch (Exception ex)
            {
                running = false;
                LogToConsole($"Sample Service Error: {ex.ToString()}");
                logger.Error("Sample Service ProcessQueue threw exception: " + ex.ToString(), null, arguments: null);
                throw;
            }
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.timer.Stop();

                LogToConsole("Sample Service stopping");

                if (processTask != null)
                {
                    LogToConsole("Process still running wait to end...");
                    processTask.Wait(5000);
                }

                LogToConsole("Sample Service stopped");
                logger.Debug($"Sample Service stopped", null, arguments: null);
            }
            catch (Exception ex)
            {
                logger.Error("Error stopping Sample exception: " + ex.ToString(), ex, null, null);
                throw;
            }
            finally
            {
                this.timer.Dispose();
                this.timer = null;
            }
        }
    }
}
