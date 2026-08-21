using KZ.FileHash.Enums;
using KZ.FileHash.Helpers;
using System.Buffers;
using System.Security.Cryptography;

namespace KZ.FileHash.Engine
{
    /// <summary>
    /// Provides functionality for calculating cryptographic hashes from files and streams.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The engine supports calculating multiple hash algorithms in a single read operation.
    /// </para>
    /// <para>
    /// Hash values are returned as uppercase hexadecimal strings.
    /// </para>
    /// </remarks>
    public sealed class FileHashEngine
    {
        private readonly HashAlgorithmType _algorithms;

        private readonly int _bufferSize = 64 * 1024;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHashEngine"/> class
        /// with the specified hashing algorithms.
        /// </summary>
        /// <param name="algorithms">
        /// One or more hashing algorithms to calculate.
        /// Multiple algorithms can be combined using the bitwise OR operator.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="algorithms"/> is <see cref="HashAlgorithmType.None"/>
        /// or contains unsupported flags.
        /// </exception>
        public FileHashEngine(HashAlgorithmType algorithms)
        {
            ValidateAlgorithms(algorithms);

            _algorithms = algorithms;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHashEngine"/> class
        /// with the specified hashing algorithms and buffer size.
        /// </summary>
        /// <param name="algorithms">
        /// One or more hashing algorithms to calculate.
        /// Multiple algorithms can be combined using the bitwise OR operator.
        /// </param>
        /// <param name="bufferSize">
        /// The size of the internal buffer in bytes. Must be greater than zero.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="algorithms"/> is <see cref="HashAlgorithmType.None"/>
        /// or contains unsupported flags.
        /// </exception>
        public FileHashEngine(HashAlgorithmType algorithms, int bufferSize)
            : this(algorithms)
        {
            ValidateBufferSize(bufferSize);
            _bufferSize = bufferSize;
        }

        private static void ValidateBufferSize(int bufferSize)
        {
            if (bufferSize <= 0)
                throw new ArgumentException($"The buffer size must be greater than 0.", nameof(bufferSize));
        }

        private static void ValidateAlgorithms(HashAlgorithmType algorithms)
        {
            if (algorithms == HashAlgorithmType.None)
                throw new ArgumentException("At least one hashing algorithm must be selected", nameof(algorithms));

            var supportedAlgorithms = HashAlgorithmType.None;
            
            foreach (var enumValue in Enum.GetValues<HashAlgorithmType>())
            {
                supportedAlgorithms |= enumValue;
            }

            if ((algorithms & ~supportedAlgorithms) != 0)
                throw new ArgumentException("The specified hashing algorithm contains unsupported flags.", nameof(algorithms));
        }

        /// <summary>
        /// Asynchronously calculates the selected hashes for the specified file.
        /// </summary>
        /// <param name="filePath">
        /// The path of the file to hash. Both absolute and relative paths are supported.
        /// </param>
        /// <param name="progress">
        /// An optional progress reporter that receives progress values from 0 to 100.
        /// </param>
        /// <param name="cancellationToken">
        /// A token that can be used to cancel the hashing operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous hashing operation.
        /// The result contains a read-only dictionary mapping each selected
        /// <see cref="HashAlgorithmType"/> to its uppercase hexadecimal hash string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="filePath"/> is <see langword="null"/>,
        /// empty, or consists only of white-space characters.
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown when the specified file does not exist.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when access to the specified file is denied.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown when an I/O error occurs while opening or reading the file.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
        /// </exception>
        public async Task<IReadOnlyDictionary<HashAlgorithmType, string>> CalculateHashAsync
        (
            string filePath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath);

            await using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                return await CalculateContentHashAsync(fileStream, 0, progress, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Asynchronously calculates the selected hashes for the specified stream.
        /// </summary>
        /// <param name="stream">
        /// The stream containing the data to hash.
        /// Hashing starts at the stream's current position and continues until the end
        /// of the stream.
        /// </param>
        /// <param name="streamLength">
        /// The number of bytes available to be read from the current stream position.
        /// This value is required when <paramref name="stream"/> is non-seekable
        /// and <paramref name="progress"/> is provided.
        /// It is ignored for seekable streams, where the remaining length is determined
        /// automatically.
        /// </param>
        /// <param name="progress">
        /// An optional progress reporter that receives progress values from 0 to 100.
        /// For non-seekable streams, <paramref name="streamLength"/> must be provided
        /// when progress reporting is enabled.
        /// </param>
        /// <param name="cancellationToken">
        /// A token that can be used to cancel the hashing operation.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous hashing operation.
        /// The result contains a read-only dictionary mapping each selected
        /// <see cref="HashAlgorithmType"/> to its uppercase hexadecimal hash string.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The engine does not dispose the provided <paramref name="stream"/>.
        /// The caller remains responsible for disposing the stream.
        /// </para>
        /// <para>
        /// For seekable streams, hashing begins at the current position rather than
        /// at the beginning of the stream.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when progress reporting is enabled for a non-seekable stream
        /// without providing <paramref name="streamLength"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="streamLength"/> is negative.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.
        /// </exception>
        public async Task<IReadOnlyDictionary<HashAlgorithmType, string>> CalculateHashAsync
        (
            Stream stream,
            long? streamLength = null,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (stream.CanSeek == false && progress != null && streamLength is null)
                throw new ArgumentException("A stream length must be provided when progress reporting is enabled for a non-seekable stream.", nameof(streamLength));

            return await CalculateContentHashAsync(stream, streamLength, progress, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IReadOnlyDictionary<HashAlgorithmType, string>> CalculateContentHashAsync
            (
                Stream stream,
                long? streamLength = null,
                IProgress<double>? progress = null,
                CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            var incrementalHashes = InitializeIncrementalHashes();
            long? totalLength = null;

            try
            {
                int bytesRead;
                long totalBytesRead = 0;

                if (stream.CanSeek)
                    totalLength = stream.Length - stream.Position;
                else
                {
                    if (streamLength is < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(streamLength), "Stream length cannot be negative.");
                    }

                    totalLength = streamLength;
                }

                if (progress is not null)
                    progress.Report(0);

                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    AppendData(incrementalHashes, buffer.AsSpan(0, bytesRead));
                    totalBytesRead += bytesRead;

                    if (progress is not null && totalLength.HasValue && totalLength.Value > 0)
                        ReportProgress(progress, totalBytesRead, totalLength.Value);
                }


                if (progress is not null && (totalLength.HasValue == false || totalLength.Value <= 0))
                    progress.Report(100);

                return GetHashesResults(incrementalHashes);
            }
            finally
            {
                DisposeIncrementalHashes(incrementalHashes);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static void DisposeIncrementalHashes(Dictionary<HashAlgorithmType, IncrementalHash> incrementalHashes)
        {
            foreach (var incrementalHash in incrementalHashes)
                incrementalHash.Value.Dispose();
        }

        private static void ReportProgress(IProgress<double> progress, long totalRead, long totalLength)
        {
            if (totalLength <= 0)
                return;

            progress.Report(((double)totalRead / totalLength) * 100);
        }

        private static IReadOnlyDictionary<HashAlgorithmType, string> GetHashesResults(Dictionary<HashAlgorithmType, IncrementalHash> incrementalHashes)
        {
            var result = new Dictionary<HashAlgorithmType, string>();

            foreach (var incrementalHash in incrementalHashes)
                result.Add(incrementalHash.Key, Convert.ToHexString(incrementalHash.Value.GetHashAndReset()));

            return result;
        }

        private Dictionary<HashAlgorithmType, IncrementalHash> InitializeIncrementalHashes()
        {
            Dictionary<HashAlgorithmType, IncrementalHash> incrementalHashes = [];

            foreach (var algorithm in Enum.GetValues<HashAlgorithmType>())
            {
                if (algorithm == HashAlgorithmType.None)
                    continue;

                if (_algorithms.HasFlag(algorithm))
                    incrementalHashes.TryAdd(algorithm, IncrementalHash.CreateHash(AlgorithmsHelper.GetAlgorithmName(algorithm)));
            }

            return incrementalHashes;
        }

        private static void AppendData(Dictionary<HashAlgorithmType, IncrementalHash> incrementalHashes, Span<byte> data)
        {
            foreach (var incrementalHash in incrementalHashes)
                incrementalHash.Value.AppendData(data);
        }
    }
}
