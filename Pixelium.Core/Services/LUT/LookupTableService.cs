using System;
using System.Collections.Concurrent;

namespace Pixelium.Core.Services.LUT
{
    public class LookupTableService : ILookupTableService
    {
        private readonly ConcurrentDictionary<string, Lazy<byte[]>> _tables =
            new ConcurrentDictionary<string, Lazy<byte[]>>();

        public bool IsInitialized { get; private set; }
        public int TableCount => _tables.Count;

        public LookupTableService()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (IsInitialized) return;

            // Preload common tables
            _ = GetInvertTable();
            _ = GetGrayscaleRedTable();
            _ = GetGrayscaleGreenTable();
            _ = GetGrayscaleBlueTable();
            _ = GetBrightnessTable(1.2f);
            _ = GetBrightnessTable(0.8f);

            IsInitialized = true;
        }

        public byte[] GetInvertTable()
        {
            return GetOrCreateTable("Invert", i => (byte)(255 - i));
        }

        // Mindhárom csatornához külön LUT
        public byte[] GetGrayscaleRedTable()
        {
            return GetOrCreateTable("GrayscaleRed", i => (byte)(i * 0.299));
        }

        public byte[] GetGrayscaleGreenTable()
        {
            return GetOrCreateTable("GrayscaleGreen", i => (byte)(i * 0.587));
        }

        public byte[] GetGrayscaleBlueTable()
        {
            return GetOrCreateTable("GrayscaleBlue", i => (byte)(i * 0.114));
        }

        public byte[] GetBrightnessTable(float factor)
        {
            string key = $"Brightness_{factor}";
            return GetOrCreateTable(key, i => (byte)Math.Max(0, Math.Min(255, i * factor)));
        }

        public byte[] GetTable(string name)
        {
            return _tables.TryGetValue(name, out var lazyTable) ? lazyTable.Value :
                   throw new ArgumentException($"Table '{name}' not found");
        }

        private byte[] GetOrCreateTable(string name, Func<int, byte> calculator)
        {
            return _tables.GetOrAdd(name, key =>
                new Lazy<byte[]>(() => CreateTable(calculator)))
                .Value;
        }

        private byte[] CreateTable(Func<int, byte> calculator)
        {
            var table = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                table[i] = calculator(i);
            }
            return table;
        }

        public byte[] GetGammaTable(double gamma)
        {
            string key = $"Gamma_{gamma}";
            return GetOrCreateTable(key, i => CalculateGamma(i, gamma));
        }
        
        private byte CalculateGamma(int value, double gamma)
        {
            // Normalizálás: 0-255 -> 0.0-1.0
            double normalized = value / 255.0;

            // Gamma transzformáció
            double result = Math.Pow(normalized, 1.0 / gamma);

            // Vissza konvertálás: 0.0-1.0 -> 0-255
            return (byte)Math.Min(255, Math.Max(0, result * 255));
        }
    }
}
