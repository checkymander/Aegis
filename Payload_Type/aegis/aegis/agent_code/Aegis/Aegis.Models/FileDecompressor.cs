using System.IO.Compression;

public static class FileDecompressor
{
    public static void DecompressStream(Stream compressedStream, Stream decompressedStream)
    {
        if (compressedStream is null || decompressedStream is null)
        {
            throw new ArgumentNullException("Streams cannot be null");
        }

        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
        {
            gzipStream.CopyTo(decompressedStream);
        }

        decompressedStream.Position = 0;
    }
}