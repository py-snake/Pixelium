using Pixelium.Core.Services.LUT;

namespace Pixelium.Core.Services
{
    public interface ILutPreloadService
    {
        void Preload();
    }

    public class LutPreloadService : ILutPreloadService
    {
        private readonly ILookupTableService _lutService;

        public LutPreloadService(ILookupTableService lutService)
        {
            _lutService = lutService;
        }

        public void Preload()
        {
            // The service initializes automatically, but this ensures it's ready
            // and can be called explicitly during app startup
            System.Console.WriteLine($"LUT Service initialized with {_lutService.TableCount} tables");
        }
    }
}
