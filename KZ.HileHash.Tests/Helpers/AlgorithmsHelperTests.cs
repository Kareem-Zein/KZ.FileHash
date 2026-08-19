using KZ.FileHash.Enums;
using KZ.FileHash.Helpers;

namespace KZ.HileHash.Tests.Helpers
{
    [TestFixture]
    public class AlgorithmsHelperTests
    {
        #region GetAlgorithmHexStringLength
        [TestCase(HashAlgorithmType.MD5, 32)]
        [TestCase(HashAlgorithmType.SHA1, 40)]
        [TestCase(HashAlgorithmType.SHA256, 64)]
        [TestCase(HashAlgorithmType.SHA384, 96)]
        [TestCase(HashAlgorithmType.SHA512, 128)]
        [TestCase(HashAlgorithmType.SHA3_256, 64)]
        [TestCase(HashAlgorithmType.SHA3_384, 96)]
        [TestCase(HashAlgorithmType.SHA3_512, 128)]
        public void GetAlgorithmHexStringLength_WhenValidAlgorithm_ReturnsExpectedLength(
        HashAlgorithmType algorithm,
        int expectedLength)
        {
            var result = AlgorithmsHelper.GetAlgorithmHexStringLength(algorithm);

            Assert.That(result, Is.EqualTo(expectedLength));
        }

        [Test]
        public void GetAlgorithmHexStringLength_WhenAlgorithmIsNone_ReturnsZero()
        {
            var result = AlgorithmsHelper.GetAlgorithmHexStringLength(HashAlgorithmType.None);

            Assert.That(result, Is.Zero);
        }

        [Test]
        public void GetAlgorithmHexStringLength_WhenAlgorithmIsUnsupported_ReturnsZero()
        {
            var unsupportedAlgorithm = (HashAlgorithmType)256;

            var result = AlgorithmsHelper.GetAlgorithmHexStringLength(unsupportedAlgorithm);

            Assert.That(result, Is.Zero);
        }
        #endregion

        #region GetAlgorithmsByLength
        [Test]
        public void GetAlgorithmsByLength_WhenLengthIs32_ReturnsMD5()
        {
            var result = AlgorithmsHelper.GetAlgorithmsByLength(32);

            Assert.That(result, Is.EqualTo(
                new[]
                {
                HashAlgorithmType.MD5
                }));
        }

        [Test]
        public void GetAlgorithmsByLength_WhenLengthIs40_ReturnsSHA1()
        {
            var result = AlgorithmsHelper.GetAlgorithmsByLength(40);

            Assert.That(result, Is.EqualTo(
                new[]
                {
                HashAlgorithmType.SHA1
                }));
        }

        [Test]
        public void GetAlgorithmsByLength_WhenLengthIs64_ReturnsSHA256AndSHA3_256()
        {
            var result = AlgorithmsHelper.GetAlgorithmsByLength(64);

            Assert.That(
                result,
                Is.EquivalentTo(new[]
                {
                HashAlgorithmType.SHA256,
                HashAlgorithmType.SHA3_256
                }));
        }

        [Test]
        public void GetAlgorithmsByLength_WhenLengthIs96_ReturnsSHA384AndSHA3_384()
        {
            var result = AlgorithmsHelper.GetAlgorithmsByLength(96);

            Assert.That(
                result,
                Is.EquivalentTo(new[]
                {
                HashAlgorithmType.SHA384,
                HashAlgorithmType.SHA3_384
                }));
        }

        [Test]
        public void GetAlgorithmsByLength_WhenLengthIs128_ReturnsSHA512AndSHA3_512()
        {
            var result = AlgorithmsHelper.GetAlgorithmsByLength(128);

            Assert.That(
                result,
                Is.EquivalentTo(new[]
                {
                HashAlgorithmType.SHA512,
                HashAlgorithmType.SHA3_512
                }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(31)]
        [TestCase(33)]
        [TestCase(50)]
        [TestCase(65)]
        [TestCase(100)]
        [TestCase(129)]
        public void GetAlgorithmsByLength_WhenLengthIsUnsupported_ReturnsEmptyArray(
        int length)
        {
            var result = AlgorithmsHelper.GetAlgorithmsByLength(length);

            Assert.That(result, Is.Empty);
        }
        #endregion

        #region GetAlgorithmName
        [TestCase(HashAlgorithmType.MD5, "MD5")]
        [TestCase(HashAlgorithmType.SHA1, "SHA1")]
        [TestCase(HashAlgorithmType.SHA256, "SHA256")]
        [TestCase(HashAlgorithmType.SHA384, "SHA384")]
        [TestCase(HashAlgorithmType.SHA512, "SHA512")]
        [TestCase(HashAlgorithmType.SHA3_256, "SHA3-256")]
        [TestCase(HashAlgorithmType.SHA3_384, "SHA3-384")]
        [TestCase(HashAlgorithmType.SHA3_512, "SHA3-512")]
        public void GetAlgorithmName_WhenValidAlgorithm_ReturnsExpectedAlgorithmName(
        HashAlgorithmType algorithm,
        string expectedName)
        {
            var result = AlgorithmsHelper.GetAlgorithmName(algorithm);

            Assert.That(result.Name, Is.EqualTo(expectedName));
        }

        [Test]
        public void GetAlgorithmName_WhenAlgorithmIsNone_ThrowsException()
        {
            Assert.That(
                () => AlgorithmsHelper.GetAlgorithmName(HashAlgorithmType.None),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void GetAlgorithmName_WhenAlgorithmIsUnsupported_ThrowsException()
        {
            var unsupportedAlgorithm = (HashAlgorithmType)256;

            Assert.That(
                () => AlgorithmsHelper.GetAlgorithmName(unsupportedAlgorithm),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
        #endregion
    }
}
