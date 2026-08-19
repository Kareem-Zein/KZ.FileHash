namespace KZ.FileHash.Enums;

/// <summary>
/// Specifies the cryptographic hash algorithms supported by the file hashing engine.
/// </summary>
/// <remarks>
/// Multiple algorithms can be combined using the bitwise OR operator.
/// </remarks>
[Flags]
public enum HashAlgorithmType
{
    /// <summary>
    /// Specifies that no hash algorithm is selected.
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies the MD5 hashing algorithm.
    /// </summary>
    MD5 = 1,

    /// <summary>
    /// Specifies the SHA-1 hashing algorithm.
    /// </summary>
    SHA1 = 1 << 1,

    /// <summary>
    /// Specifies the SHA-256 hashing algorithm.
    /// </summary>
    SHA256 = 1 << 2,

    /// <summary>
    /// Specifies the SHA-384 hashing algorithm.
    /// </summary>
    SHA384 = 1 << 3,

    /// <summary>
    /// Specifies the SHA-512 hashing algorithm.
    /// </summary>
    SHA512 = 1 << 4,

    /// <summary>
    /// Specifies the SHA3-256 hashing algorithm.
    /// </summary>
    SHA3_256 = 1 << 5,

    /// <summary>
    /// Specifies the SHA3-384 hashing algorithm.
    /// </summary>
    SHA3_384 = 1 << 6,

    /// <summary>
    /// Specifies the SHA3-512 hashing algorithm.
    /// </summary>
    SHA3_512 = 1 << 7,
}