# KZ.FileHash

[![NuGet](https://img.shields.io/nuget/v/KZ.FileHash.svg)](https://www.nuget.org/packages/KZ.FileHash/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/KZ.FileHash.svg)](https://www.nuget.org/packages/KZ.FileHash/)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue)](https://dotnet.microsoft.com/)

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Benchmarks](https://img.shields.io/badge/Benchmarks-Passing-brightgreen)](https://github.com/Kareem-Zein/KZ.FileHash/actions/workflows/benchmarks.yml)

**KZ.FileHash** is a lightweight .NET library for calculating cryptographic hashes of files and streams asynchronously.

It is designed for applications that need reliable and memory-efficient file hashing without loading the entire file into memory.

The library supports multiple hashing algorithms, progress reporting, cancellation, file streams, and non-seekable streams.

---


## 📊 Performance Benchmarks
<!-- github-benchmark-action-comment -->

## Features

- Calculate hashes asynchronously.
- Calculate multiple hashes in a single read operation.
- Supports files and streams.
- Supports seekable and non-seekable streams.
- Optional progress reporting from `0` to `100`.
- Supports `CancellationToken`.
- Uses buffered asynchronous I/O.
- Uses `ArrayPool<byte>` to reduce memory allocations.
- Uses `IncrementalHash` for streaming hash calculation.
- Does not load the entire file into memory.
- Returns hexadecimal hash strings.
- Supports combining multiple algorithms using `[Flags]`.
- Compatible with modern .NET applications.
- Supports **.NET 8.0, .NET 9.0, and .NET 10.0** (multi-targeting).
- **Customizable buffer size** – You can fine-tune the internal buffer for your specific environment.
- Default buffer size **64 KB** – optimized to avoid the Large Object Heap (LOH) while delivering great performance.

---

## Supported Algorithms

KZ.FileHash currently supports the following algorithms:

| Algorithm | Hex String Length |
|-----------|------------------:|
| MD5 | 32 |
| SHA-1 | 40 |
| SHA-256 | 64 |
| SHA-384 | 96 |
| SHA-512 | 128 |
| SHA3-256 | 64 |
| SHA3-384 | 96 |
| SHA3-512 | 128 |

> **Security note:** MD5 and SHA-1 are considered cryptographically weak for security-sensitive applications. They are provided primarily for compatibility and integrity-checking scenarios where collision resistance is not a security requirement.

---

## Installation

Install the package from NuGet:

```bash
dotnet add package KZ.FileHash
```
---


## Requirements & Defaults

| Feature | Details |
| :--- | :--- |
| **Supported Runtimes** | .NET 8.0, .NET 9.0, .NET 10.0 |
| **Default Buffer Size** | **64 KB** – chosen as a power of two (2^16) for optimal system cache alignment, while staying below the 85 KB LOH threshold to minimize GC pressure. |
| **Buffer Customization** | You can set any buffer size > 0. The library trusts your expertise – no hard upper limit is enforced. |

## Quick Start

```csharp
using KZ.FileHash.Engine;
using KZ.FileHash.Enums;

var engine = new FileHashEngine(HashAlgorithmType.SHA256);

var hashes = await engine.CalculateHashAsync("example.zip");

Console.WriteLine(hashes[HashAlgorithmType.SHA256]);
```

## Multiple Algorithms

Multiple algorithms can be calculated in a single read operation:

```csharp
var engine = new FileHashEngine(
    HashAlgorithmType.MD5 |
    HashAlgorithmType.SHA256 |
    HashAlgorithmType.SHA512);

var hashes = await engine.CalculateHashAsync("example.zip");

Console.WriteLine(hashes[HashAlgorithmType.MD5]);
Console.WriteLine(hashes[HashAlgorithmType.SHA256]);
Console.WriteLine(hashes[HashAlgorithmType.SHA512]);
```

## Stream Support

KZ.FileHash also supports hashing directly from streams:

```csharp
await using var stream = File.OpenRead("example.zip");

var engine = new FileHashEngine(HashAlgorithmType.SHA256);

var hashes = await engine.CalculateHashAsync(stream);

Console.WriteLine(hashes[HashAlgorithmType.SHA256]);
```

## Progress Reporting

Progress reporting is optional:

```csharp
var progress = new Progress<double>(value =>
{
    Console.WriteLine($"{value:F2}%");
});

var engine = new FileHashEngine(HashAlgorithmType.SHA256);

var hashes = await engine.CalculateHashAsync(
    "large-file.iso",
    progress);
```

## Customize buffer size

```csharp
var progress = new Progress<double>(value =>
{
    Console.WriteLine($"{value:F2}%");
});

var engine = new FileHashEngine(HashAlgorithmType.SHA256, 1024 * 1024);

var hashes = await engine.CalculateHashAsync(
    "large-file.iso",
    progress);
```

## Cancellation

Hash calculation supports cancellation through `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource();

var engine = new FileHashEngine(HashAlgorithmType.SHA256);

var hashes = await engine.CalculateHashAsync(
    "large-file.iso",
    cancellationToken: cts.Token);
```

## AlgorithmsHelper

`AlgorithmsHelper` provides utilities for working with supported algorithms.

```csharp
using KZ.FileHash.Enums;
using KZ.FileHash.Helpers;

var length =
    AlgorithmsHelper.GetAlgorithmHexStringLength(
        HashAlgorithmType.SHA256);

var algorithms =
    AlgorithmsHelper.GetAlgorithmsByLength(64);

var name =
    AlgorithmsHelper.GetAlgorithmName(
        HashAlgorithmType.SHA256);
```

## Security Note

MD5 and SHA-1 are cryptographically weak and should not be used for security-sensitive applications.

They are included primarily for compatibility and non-security-critical integrity checking.

For modern applications, SHA-256, SHA-384, SHA-512, or SHA-3 variants are recommended.

## License

KZ.FileHash is licensed under the MIT License.

See the [LICENSE](LICENSE) file for details.

## Author

**Kareem Zein**

- GitHub: https://github.com/Kareem-Zein
- Website: https://kareem-zein.com