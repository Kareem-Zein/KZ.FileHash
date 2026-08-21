using KZ.FileHash.Engine;
using KZ.FileHash.Enums;
using System.Security.Cryptography;
using System.Text;

namespace KZ.HileHash.Tests.Engine
{
    [TestFixture]
    public class FileHashEngineTests
    {
        private sealed class TestProgress : IProgress<double>
        {
            public List<double> ReportedValues { get; } = [];

            public void Report(double value)
            {
                ReportedValues.Add(value);
            }
        }

        private sealed class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] buffer)
                : base(buffer)
            {
            }

            public override bool CanSeek => false;

            public override long Length =>
                throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override long Seek(
                long offset,
                SeekOrigin loc) =>
                throw new NotSupportedException();
        }

        [Test]
        public void Constructor_WhenAlgorithmsIsNone_ThrowsArgumentException()
        {
            Assert.That(
                () => new FileHashEngine(HashAlgorithmType.None),
                Throws.ArgumentException);
        }

        [Test]
        public void Constructor_WhenAlgorithmsAreValid_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                new FileHashEngine(
                    HashAlgorithmType.MD5 |
                    HashAlgorithmType.SHA256 |
                    HashAlgorithmType.SHA3_256));
        }

        [Test]
        public void Constructor_WhenAllAlgorithmsAreSelected_DoesNotThrow()
        {
            var algorithms =
                HashAlgorithmType.MD5 |
                HashAlgorithmType.SHA1 |
                HashAlgorithmType.SHA256 |
                HashAlgorithmType.SHA384 |
                HashAlgorithmType.SHA512 |
                HashAlgorithmType.SHA3_256 |
                HashAlgorithmType.SHA3_384 |
                HashAlgorithmType.SHA3_512;

            Assert.DoesNotThrow(() => new FileHashEngine(algorithms));
        }

        [Test]
        public void Constructor_WhenUnsupportedAlgorithmIsProvided_ThrowsArgumentException()
        {
            var unsupportedAlgorithm = (HashAlgorithmType)256;

            Assert.That(
                () => new FileHashEngine(unsupportedAlgorithm),
                Throws.ArgumentException);
        }

        [Test]
        public void Constructor_WhenSupportedAndUnsupportedAlgorithmsAreCombined_ThrowsArgumentException()
        {
            var algorithms =
                HashAlgorithmType.SHA256 |
                (HashAlgorithmType)256;

            Assert.That(
                () => new FileHashEngine(algorithms),
                Throws.ArgumentException);
        }

        private static readonly byte[] TestData = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog");

        [TestCaseSource(nameof(HashAlgorithmTestCases))]
        public async Task CalculateHashAsync_WhenUsingKnownData_ReturnsExpectedHash(HashAlgorithmType algorithm, string expectedHash)
        {
            await using var stream = new MemoryStream(TestData);

            var engine = new FileHashEngine(algorithm);

            var result = await engine.CalculateHashAsync(stream);

            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result, Contains.Key(algorithm));
                Assert.That(result[algorithm], Is.EqualTo(expectedHash));
            });
        }

        private static IEnumerable<TestCaseData> HashAlgorithmTestCases()
        {
            yield return new TestCaseData(
                HashAlgorithmType.MD5,
                Convert.ToHexString(MD5.HashData(TestData)));

            yield return new TestCaseData(
                HashAlgorithmType.SHA1,
                Convert.ToHexString(SHA1.HashData(TestData)));

            yield return new TestCaseData(
                HashAlgorithmType.SHA256,
                Convert.ToHexString(SHA256.HashData(TestData)));

            yield return new TestCaseData(
                HashAlgorithmType.SHA384,
                Convert.ToHexString(SHA384.HashData(TestData)));

            yield return new TestCaseData(
                HashAlgorithmType.SHA512,
                Convert.ToHexString(SHA512.HashData(TestData)));

            yield return new TestCaseData(
                HashAlgorithmType.SHA3_256,
                Convert.ToHexString(SHA3_256.HashData(TestData)));

            yield return new TestCaseData(
                HashAlgorithmType.SHA3_384,
                Convert.ToHexString(SHA3_384.HashData(TestData)));

            yield return new TestCaseData(
                HashAlgorithmType.SHA3_512,
                Convert.ToHexString(SHA3_512.HashData(TestData)));
        }

        [Test]
        public async Task CalculateHashAsync_WhenMultipleAlgorithmsAreSelected_ReturnsAllExpectedHashes()
        {
            var algorithms =
                HashAlgorithmType.MD5 |
                HashAlgorithmType.SHA256 |
                HashAlgorithmType.SHA3_256;

            await using var stream = new MemoryStream(TestData);

            var engine = new FileHashEngine(algorithms);

            var result = await engine.CalculateHashAsync(stream);

            var expectedHashes = new Dictionary<HashAlgorithmType, string>
            {
                [HashAlgorithmType.MD5] =
                    Convert.ToHexString(MD5.HashData(TestData)),

                [HashAlgorithmType.SHA256] =
                    Convert.ToHexString(SHA256.HashData(TestData)),

                [HashAlgorithmType.SHA3_256] =
                    Convert.ToHexString(SHA3_256.HashData(TestData))
            };

            Assert.That(result, Is.EqualTo(expectedHashes));
        }

        [Test]
        public async Task CalculateHashAsync_WhenAllAlgorithmsAreSelected_ReturnsAllHashes()
        {
            var algorithms =
                HashAlgorithmType.MD5 |
                HashAlgorithmType.SHA1 |
                HashAlgorithmType.SHA256 |
                HashAlgorithmType.SHA384 |
                HashAlgorithmType.SHA512 |
                HashAlgorithmType.SHA3_256 |
                HashAlgorithmType.SHA3_384 |
                HashAlgorithmType.SHA3_512;

            await using var stream = new MemoryStream(TestData);

            var engine = new FileHashEngine(algorithms);

            var result = await engine.CalculateHashAsync(stream);

            Assert.That(
                result.Keys,
                Is.EquivalentTo(Enum.GetValues<HashAlgorithmType>()
                    .Where(x => x != HashAlgorithmType.None)));

            Assert.That(result, Has.Count.EqualTo(8));
        }

        [Test]
        public async Task CalculateHashAsync_WhenFileExists_ReturnsExpectedHash()
        {
            var filePath = Path.GetTempFileName();

            try
            {
                await File.WriteAllBytesAsync(filePath, TestData);

                var engine = new FileHashEngine(HashAlgorithmType.SHA256);

                var result = await engine.CalculateHashAsync(filePath);

                var expectedHash =
                    Convert.ToHexString(SHA256.HashData(TestData));

                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result[HashAlgorithmType.SHA256], Is.EqualTo(expectedHash));
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Test]
        public void CalculateHashAsync_WhenFileDoesNotExist_ThrowsFileNotFoundException()
        {
            var filePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.tmp");

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            Assert.That(
                async () => await engine.CalculateHashAsync(filePath),
                Throws.TypeOf<FileNotFoundException>());
        }

        [TestCase(null)]
        public void CalculateHashAsync_WhenFilePathIsNull_ThrowsArgumentException(string? filePath)
        {
            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            Assert.That(
                async () => await engine.CalculateHashAsync(filePath!),
                Throws.ArgumentNullException);
        }

        [TestCase("")]
        [TestCase("   ")]
        public void CalculateHashAsync_WhenFilePathIsEmptyOrWhiteSpace_ThrowsArgumentException(string? filePath)
        {
            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            Assert.That(
                async () => await engine.CalculateHashAsync(filePath!),
                Throws.ArgumentException);
        }

        [Test]
        public async Task CalculateHashAsync_WhenFileIsEmpty_ReturnsExpectedHash()
        {
            var filePath = Path.GetTempFileName();

            try
            {
                var engine = new FileHashEngine(HashAlgorithmType.SHA256);

                var result = await engine.CalculateHashAsync(filePath);

                var expectedHash =
                    Convert.ToHexString(SHA256.HashData([]));

                Assert.That(
                    result[HashAlgorithmType.SHA256],
                    Is.EqualTo(expectedHash));
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Test]
        public void CalculateHashAsync_WhenStreamIsNull_ThrowsArgumentNullException()
        {
            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            Assert.That(
                async () => await engine.CalculateHashAsync(stream: null!),
                Throws.ArgumentNullException);
        }

        [Test]
        public async Task CalculateHashAsync_WhenStreamIsSeekable_ReturnsExpectedHash()
        {
            await using var stream = new MemoryStream(TestData);

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var result = await engine.CalculateHashAsync(stream);

            var expectedHash =
                Convert.ToHexString(SHA256.HashData(TestData));

            Assert.That(
                result[HashAlgorithmType.SHA256],
                Is.EqualTo(expectedHash));
        }

        [Test]
        public async Task CalculateHashAsync_WhenChangeBufferSize_ReturnsExpectedHash()
        {
            await using var stream = new MemoryStream(TestData);

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var result = await engine.CalculateHashAsync(stream);

            var expectedHash =
                Convert.ToHexString(SHA256.HashData(TestData));

            Assert.That(
                result[HashAlgorithmType.SHA256],
                Is.EqualTo(expectedHash));

            var engineWithBufferSize = new FileHashEngine(HashAlgorithmType.SHA256, 1 /*1 Byte*/);

            stream.Position = 0;
            var result2 = await engineWithBufferSize.CalculateHashAsync(stream);

            Assert.That(
               result2[HashAlgorithmType.SHA256],
               Is.EqualTo(expectedHash));
        }

        [Test]
        public async Task CalculateHashAsync_WhenStreamPositionIsNotZero_HashesFromCurrentPosition()
        {
            var data = Encoding.UTF8.GetBytes("ABCDEF");
            var expectedData = Encoding.UTF8.GetBytes("DEF");

            await using var stream = new MemoryStream(data)
            {
                Position = 3
            };

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var result = await engine.CalculateHashAsync(stream);

            var expectedHash =
                Convert.ToHexString(SHA256.HashData(expectedData));

            Assert.That(
                result[HashAlgorithmType.SHA256],
                Is.EqualTo(expectedHash));
        }

        [Test]
        public async Task CalculateHashAsync_WhenStreamIsNotSeekable_AndLengthIsProvided_ReturnsExpectedHash()
        {
            await using var stream = new NonSeekableStream(TestData);

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var result = await engine.CalculateHashAsync(
                stream,
                TestData.Length);

            var expectedHash =
                Convert.ToHexString(SHA256.HashData(TestData));

            Assert.That(
                result[HashAlgorithmType.SHA256],
                Is.EqualTo(expectedHash));
        }

        [Test]
        public void CalculateHashAsync_WhenStreamIsNotSeekable_AndProgressIsProvidedWithoutLength_ThrowsArgumentException()
        {
            var data = Encoding.UTF8.GetBytes("test");

            using var stream = new NonSeekableStream(data);

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var progress = new TestProgress();

            Assert.That(
                async () => await engine.CalculateHashAsync(
                    stream,
                    progress: progress),
                Throws.ArgumentException);
        }

        [Test]
        public async Task CalculateHashAsync_WhenStreamIsProvided_DoesNotDisposeStream()
        {
            await using var stream = new MemoryStream(
                Encoding.UTF8.GetBytes("test"));

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            await engine.CalculateHashAsync(stream);

            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public async Task CalculateHashAsync_WhenProgressIsProvided_ReportsProgressFromZeroToHundred()
        {
            var data = new byte[1024 * 1024];

            await using var stream = new MemoryStream(data);

            var progress = new TestProgress();

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            await engine.CalculateHashAsync(
                stream,
                progress: progress);

            Assert.That(progress.ReportedValues, Is.Not.Empty);
            Assert.That(progress.ReportedValues.First(), Is.EqualTo(0));
            Assert.That(progress.ReportedValues.Last(), Is.EqualTo(100));
        }

        [Test]
        public async Task CalculateHashAsync_WhenProgressIsProvided_ReportsValuesBetweenZeroAndHundred()
        {
            var data = new byte[1024 * 1024];

            await using var stream = new MemoryStream(data);

            var progress = new TestProgress();

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            await engine.CalculateHashAsync(
                stream,
                progress: progress);

            Assert.That(
                progress.ReportedValues,
                Is.All.Matches<double>(value => value >= 0 && value <= 100));
        }

        [Test]
        public async Task CalculateHashAsync_WhenProgressIsProvided_ReportsMonotonicallyIncreasingValues()
        {
            var data = new byte[1024 * 1024];

            await using var stream = new MemoryStream(data);

            var progress = new TestProgress();

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            await engine.CalculateHashAsync(
                stream,
                progress: progress);

            for (var i = 1; i < progress.ReportedValues.Count; i++)
            {
                Assert.That(
                    progress.ReportedValues[i],
                    Is.GreaterThanOrEqualTo(progress.ReportedValues[i - 1]));
            }
        }

        [Test]
        public async Task CalculateHashAsync_WhenProgressIsNotProvided_CompletesSuccessfully()
        {
            var data = new byte[1024];

            await using var stream = new MemoryStream(data);

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var result = await engine.CalculateHashAsync(stream);

            Assert.That(result, Contains.Key(HashAlgorithmType.SHA256));
        }

        [Test]
        public async Task CalculateHashAsync_WhenStreamIsEmpty_ReportsZeroAndHundredPercent()
        {
            await using var stream = new MemoryStream();

            var progress = new TestProgress();

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            await engine.CalculateHashAsync(
                stream,
                progress: progress);

            Assert.That(
                progress.ReportedValues,
                Is.EqualTo(new[] { 0d, 100d }));
        }

        [Test]
        public void CalculateHashAsync_WhenCancellationTokenIsAlreadyCanceled_ThrowsOperationCanceledException()
        {
            var data = Encoding.UTF8.GetBytes("test data");

            using var stream = new MemoryStream(data);

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            Assert.That(
                async () => await engine.CalculateHashAsync(
                    stream,
                    cancellationToken: cancellationTokenSource.Token),
                Throws.TypeOf<OperationCanceledException>());
        }

        private sealed class BlockingStream : MemoryStream
        {
            private readonly TaskCompletionSource _readStarted =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            private readonly TaskCompletionSource _allowRead =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public BlockingStream(byte[] buffer)
                : base(buffer)
            {
            }

            public Task ReadStarted => _readStarted.Task;

            public void AllowRead()
            {
                _allowRead.TrySetResult();
            }

            public override async ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                _readStarted.TrySetResult();

                await _allowRead.Task.WaitAsync(cancellationToken);

                return await base.ReadAsync(buffer, cancellationToken);
            }
        }

        [Test]
        public async Task CalculateHashAsync_WhenCancellationOccursDuringRead_ThrowsOperationCanceledException()
        {
            var data = new byte[1024 * 1024];

            await using var stream = new BlockingStream(data);

            using var cancellationTokenSource = new CancellationTokenSource();

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var hashingTask = engine.CalculateHashAsync(
                stream,
                cancellationToken: cancellationTokenSource.Token);

            await stream.ReadStarted;

            await cancellationTokenSource.CancelAsync();

            Assert.That(
                async () => await hashingTask,
                Throws.TypeOf<TaskCanceledException>());
        }

        [Test]
        public void CalculateHashAsync_WhenStreamLengthIsNegative_ThrowsArgumentOutOfRangeException()
        {
            using var stream = new NonSeekableStream(
                Encoding.UTF8.GetBytes("test"));

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            Assert.That(
                async () => await engine.CalculateHashAsync(
                    stream,
                    -1),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public async Task CalculateHashAsync_WhenNonSeekableStreamHasNoProgress_DoesNotRequireLength()
        {
            var data = Encoding.UTF8.GetBytes("test data");

            await using var stream = new NonSeekableStream(data);

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var result = await engine.CalculateHashAsync(stream);

            var expectedHash =
                Convert.ToHexString(SHA256.HashData(data));

            Assert.That(
                result[HashAlgorithmType.SHA256],
                Is.EqualTo(expectedHash));
        }

        [Test]
        public async Task CalculateHashAsync_WhenNonSeekableStreamHasProgressAndLength_ReturnsExpectedHash()
        {
            var data = Encoding.UTF8.GetBytes("test data");

            await using var stream = new NonSeekableStream(data);

            var progress = new TestProgress();

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            var result = await engine.CalculateHashAsync(
                stream,
                data.Length,
                progress);

            var expectedHash =
                Convert.ToHexString(SHA256.HashData(data));

            Assert.Multiple(() =>
            {
                Assert.That(
                    result[HashAlgorithmType.SHA256],
                    Is.EqualTo(expectedHash));

                Assert.That(progress.ReportedValues, Is.Not.Empty);
                Assert.That(progress.ReportedValues.First(), Is.EqualTo(0));
                Assert.That(progress.ReportedValues.Last(), Is.EqualTo(100));
            });
        }

        [Test]
        public async Task CalculateHashAsync_WhenCanceled_DoesNotDisposeProvidedStream()
        {
            var data = Encoding.UTF8.GetBytes("test data");

            await using var stream = new MemoryStream(data);

            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            var engine = new FileHashEngine(HashAlgorithmType.SHA256);

            Assert.That(
                async () => await engine.CalculateHashAsync(
                    stream,
                    cancellationToken: cancellationTokenSource.Token),
                Throws.InstanceOf<OperationCanceledException>());

            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public async Task CalculateHashAsync_IntegrationTest_ProducesExpectedHashesAndProgress()
        {
            var filePath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid():N}.txt");

            try
            {
                await File.WriteAllBytesAsync(filePath, TestData);

                var algorithms =
                    HashAlgorithmType.MD5 |
                    HashAlgorithmType.SHA256 |
                    HashAlgorithmType.SHA3_256;

                var progress = new TestProgress();

                var engine = new FileHashEngine(algorithms);

                var result = await engine.CalculateHashAsync(
                    filePath,
                    progress: progress);

                var expected = new Dictionary<HashAlgorithmType, string>
                {
                    [HashAlgorithmType.MD5] =
                        Convert.ToHexString(MD5.HashData(TestData)),

                    [HashAlgorithmType.SHA256] =
                        Convert.ToHexString(SHA256.HashData(TestData)),

                    [HashAlgorithmType.SHA3_256] =
                        Convert.ToHexString(SHA3_256.HashData(TestData))
                };

                Assert.Multiple(() =>
                {
                    Assert.That(result, Is.EqualTo(expected));

                    Assert.That(progress.ReportedValues, Is.Not.Empty);
                    Assert.That(progress.ReportedValues.First(), Is.EqualTo(0));
                    Assert.That(progress.ReportedValues.Last(), Is.EqualTo(100));
                });
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}
