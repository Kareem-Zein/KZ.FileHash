using BenchmarkDotNet.Attributes;
using KZ.FileHash.Engine;
using System.Security.Cryptography;

namespace KZ.FileHash.Benchmarks.FileHashBenchmarks
{
    [MemoryDiagnoser]
    public class FileHashBenchmarks
    {
        private const int KB64 = 64 * 1024;
        private const int Mega = 1024 * 1024;
        private const int Mega16 = 16 * Mega;
        private const int Mega100 = 100 * Mega;
        private const int Mega500 = 500 * Mega;

        [Params(Mega, Mega16, Mega100, Mega500)]
        public long FileSize { set; get; }

        [Params(KB64, Mega)]
        public int BufferSize { set; get; }

        private string _filePath = string.Empty;

        [GlobalSetup]
        public void Setup()
        {
            _filePath = Path.GetTempFileName();
            using var fs = File.Create(_filePath);
            fs.SetLength(FileSize);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }

        [Benchmark(Baseline = true)]
        public async Task<string> KZFileHashSingleAlgorithm()
        {
            var engine = new FileHashEngine(Enums.HashAlgorithmType.SHA512, BufferSize);
            var hashes = await engine.CalculateHashAsync(_filePath);
            return hashes[Enums.HashAlgorithmType.SHA512];
        }

        [Benchmark]
        public async Task<string> KZFileHashAllAlgorithms()
        {
            var engine = new FileHashEngine(Enums.HashAlgorithmType.MD5 | 
                Enums.HashAlgorithmType.SHA1 |
                Enums.HashAlgorithmType.SHA256 | Enums.HashAlgorithmType.SHA384 | Enums.HashAlgorithmType.SHA512 |
                Enums.HashAlgorithmType.SHA3_256 | Enums.HashAlgorithmType.SHA3_384 | Enums.HashAlgorithmType.SHA3_512
                , BufferSize);

            var hashes = await engine.CalculateHashAsync(_filePath);
            return hashes[Enums.HashAlgorithmType.SHA512];
        }

        [Benchmark]
        public async Task<string> TraditionalReadAllBytesAsync()
        {
            var bytes = await File.ReadAllBytesAsync(_filePath);
            using var sha = SHA512.Create();
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
        
        [Benchmark]
        public async Task<string[]> TraditionalReadAllBytesAsyncMultiAlgorithm()
        {
            var bytes = await File.ReadAllBytesAsync(_filePath);

            using var md5 = MD5.Create();
            using var sha1 = SHA1.Create();
            using var sha512 = SHA512.Create();
            using var sha256 = SHA256.Create();
            using var sha384 = SHA384.Create();
            using var sha3_256 = SHA3_256.Create();
            using var sha3_384 = SHA3_384.Create();
            using var sha3_512 = SHA3_512.Create();

            return
            [
                Convert.ToHexString(md5.ComputeHash(bytes)),
                Convert.ToHexString(sha1.ComputeHash(bytes)),
                Convert.ToHexString(sha512.ComputeHash(bytes)),
                Convert.ToHexString(sha256.ComputeHash(bytes)),
                Convert.ToHexString(sha384.ComputeHash(bytes)),
                Convert.ToHexString(sha3_256.ComputeHash(bytes)),
                Convert.ToHexString(sha3_384.ComputeHash(bytes)),
                Convert.ToHexString(sha3_512.ComputeHash(bytes))
            ];
        }

        [Benchmark]
        public async Task<string> TraditionalStreamWithComputeHashAsync()
        {
            using (var stream = File.OpenRead(_filePath))
            {
                using var sha = SHA512.Create();
                return Convert.ToHexString(await sha.ComputeHashAsync(stream));
            }
        }

        private async Task<string> CalculateHashAsync(HashAlgorithm algorithm)
        {
            await using var stream = File.OpenRead(_filePath);

            return Convert.ToHexString(
                await algorithm.ComputeHashAsync(stream)
            );
        }

        [Benchmark]
        public async Task<string[]> TraditionalStreamWithComputeHashAsyncMultiAlgorithm()
        {
            using var md5 = MD5.Create();
            using var sha1 = SHA1.Create();
            using var sha512 = SHA512.Create();
            using var sha256 = SHA256.Create();
            using var sha384 = SHA384.Create();
            using var sha3_256 = SHA3_256.Create();
            using var sha3_384 = SHA3_384.Create();
            using var sha3_512 = SHA3_512.Create();
            string[] result = new string[8];
            int index = 0;
            result[index++] = await CalculateHashAsync(md5);
            result[index++] = await CalculateHashAsync(sha1);
            result[index++] = await CalculateHashAsync(sha512);
            result[index++] = await CalculateHashAsync(sha256);
            result[index++] = await CalculateHashAsync(sha384);
            result[index++] = await CalculateHashAsync(sha3_256);
            result[index++] = await CalculateHashAsync(sha3_384);
            result[index++] = await CalculateHashAsync(sha3_512);

            return result;
        }
    }
}
