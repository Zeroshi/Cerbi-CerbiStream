using CerbiStream.Encryption;
using CerbiStream.Logging.Configuration;
using static CerbiStream.Interfaces.IEncryptionTypeProvider;

namespace CerbiStream.IntegrationTests.Tests;

/// <summary>
/// Tests for encryption pathways: AES, Base64, NoOp
/// </summary>
public static class EncryptionTests
{
    public static async Task RunAllAsync(TestRunner runner)
    {
        Console.WriteLine("\n🔐 Encryption Tests");
        Console.WriteLine("   Testing encryption pathways...\n");

        runner.RunTest("NoOp encryption returns original value", () =>
        {
            var options = new CerbiStreamOptions().WithoutEncryption();
            var encryption = EncryptionFactory.GetEncryption(options);
            
            var original = "Hello, World!";
            var encrypted = encryption.Encrypt(original);
            
            Assert(encrypted == original, "NoOp should return original value");
            Assert(!encryption.IsEnabled, "NoOp should report as disabled");
        });

        runner.RunTest("Base64 encryption encodes correctly", () =>
        {
            var options = new CerbiStreamOptions().WithBase64Encryption();
            var encryption = EncryptionFactory.GetEncryption(options);
            
            var original = "Hello, World!";
            var encrypted = encryption.Encrypt(original);
            
            Assert(encrypted != original, "Base64 should change the value");
            Assert(encryption.IsEnabled, "Base64 should report as enabled");
            
            // Verify it's valid base64
            var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encrypted));
            Assert(decoded == original, "Base64 should roundtrip correctly");
        });

        runner.RunTest("AES encryption requires key and IV", () =>
        {
            var options = new CerbiStreamOptions().WithAesEncryption();
            // No key/IV set
            
            var threw = false;
            try
            {
                var encryption = EncryptionFactory.GetEncryption(options);
            }
            catch
            {
                threw = true;
            }
            
            Assert(threw, "AES without key/IV should throw");
        });

        runner.RunTest("AES encryption with key/IV works", () =>
        {
            var key = new byte[16]; // 128-bit key for AES-128
            var iv = new byte[16];  // 128-bit IV
            new Random(42).NextBytes(key);
            new Random(43).NextBytes(iv);
            
            var options = new CerbiStreamOptions()
                .WithAesEncryption()
                .WithEncryptionKey(key, iv);
            
            var encryption = EncryptionFactory.GetEncryption(options);
            
            var original = "Sensitive data to encrypt!";
            var encrypted = encryption.Encrypt(original);
            
            Assert(encrypted != original, "AES should change the value");
            Assert(encryption.IsEnabled, "AES should report as enabled");
            
            var decrypted = encryption.Decrypt(encrypted);
            Assert(decrypted == original, "AES should roundtrip correctly");
        });

        runner.RunTest("AES encryption produces different output each run", () =>
        {
            var key = new byte[16]; // 128-bit key
            var iv = new byte[16];
            new Random(42).NextBytes(key);
            new Random(43).NextBytes(iv);
            
            var options = new CerbiStreamOptions()
                .WithAesEncryption()
                .WithEncryptionKey(key, iv);
            
            var encryption = EncryptionFactory.GetEncryption(options);
            
            var original = "Test data";
            var encrypted1 = encryption.Encrypt(original);
            var encrypted2 = encryption.Encrypt(original);
            
            // Both should decrypt to original
            Assert(encryption.Decrypt(encrypted1) == original, "First encryption should decrypt");
            Assert(encryption.Decrypt(encrypted2) == original, "Second encryption should decrypt");
        });

        runner.RunTest("Encryption mode can be set via fluent API", () =>
        {
            var options1 = new CerbiStreamOptions().WithoutEncryption();
            Assert(options1.EncryptionMode == EncryptionType.None, "Should be None");
            
            var options2 = new CerbiStreamOptions().WithBase64Encryption();
            Assert(options2.EncryptionMode == EncryptionType.Base64, "Should be Base64");
            
            var options3 = new CerbiStreamOptions().WithAesEncryption();
            Assert(options3.EncryptionMode == EncryptionType.AES, "Should be AES");
        });

        await Task.CompletedTask;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception($"Assertion failed: {message}");
    }
}
