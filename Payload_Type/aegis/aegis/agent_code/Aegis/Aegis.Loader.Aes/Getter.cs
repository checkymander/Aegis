using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;

public static class Getter
{
    public static bool TryGet(Stream inputStream, Stream outputStream, string key)
    {
        // Convert the key string to bytes
        byte[] keyBytes = System.Text.Encoding.UTF8.GetBytes(key);

        if (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
        {
            throw new ArgumentException("Key must be 16, 24, or 32 bytes long after encoding.");
        }

        // Read the IV (first 16 bytes) from the input stream
        byte[] iv = new byte[16];
        inputStream.Read(iv, 0, iv.Length);

        // Create AES instance
        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            // Create a decryptor
            using (ICryptoTransform decryptor = aes.CreateDecryptor())
            {
                if(decryptor is null)
                {
                    return false;
                }
                using (CryptoStream cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
                {
                    if(cryptoStream is null)
                    {
                        return false;
                    }

                    // Copy decrypted data to the output stream
                    cryptoStream.CopyTo(outputStream);
                    return true;
                }
            }
        }
    }

    // Example usage
    // DecryptFile("encrypted.bin", "decrypted.txt", System.Text.Encoding.UTF8.GetBytes("mysecretkey123456"));
}