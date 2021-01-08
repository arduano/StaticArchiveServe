using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using SharpCompress.Archives.Zip;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StaticArchiveServe
{
    public record SiteEntry(int status, string? location, string? contentType, string filename)
    {
        public Task<Stream?> FetchStream() => ArchiveContent.FetchFileStream(filename);
    }

    public static class ArchiveContent
    {
        class CloseCallbackStream : Stream, IDisposable
        {
            Stream Parent { get; }

            Action OnClose { get; }

            public CloseCallbackStream(Stream parent, Action onClose)
            {
                Parent = parent;
                OnClose = onClose;
            }

            public override bool CanRead => Parent.CanRead;
            public override bool CanSeek => Parent.CanSeek;
            public override bool CanWrite => Parent.CanWrite;
            public override long Length => Parent.Length;
            public override long Position { get => Parent.Position; set => Parent.Position = value; }

            public override void Flush() => Parent.Flush();

            public override int Read(byte[] buffer, int offset, int count) => Parent.Read(buffer, offset, count);

            public override long Seek(long offset, SeekOrigin origin) => Parent.Seek(offset, origin);

            public override void SetLength(long value) => Parent.SetLength(value);

            public override void Write(byte[] buffer, int offset, int count) => Parent.Write(buffer, offset, count);

            public override void Close()
            {
                base.Close();
                OnClose();
            }
        }

        static Dictionary<string, SiteEntry> SiteEntries { get; }

        static IArchive FileArchive { get; }
        static Dictionary<string, IArchiveEntry> FileContent { get; }

        public static byte[] Error404PageBytes { get; }

        static ArchiveContent()
        {
            var json = File.ReadAllText("./data/sitemap.json");
            SiteEntries = JsonSerializer.Deserialize<Dictionary<string, SiteEntry>>(json)!;

            var archiveName = Environment.GetEnvironmentVariable("ARCHIVENAME") ?? "files.zip";
            var archiveStream = new BufferedStream(File.OpenRead("./data/" + archiveName));

            if(archiveName.EndsWith(".rar"))
                FileArchive = RarArchive.Open(archiveStream);
            else if(archiveName.EndsWith(".rar"))
                FileArchive = SevenZipArchive.Open(archiveStream);
            else
                FileArchive = ZipArchive.Open(archiveStream);

            FileContent = FileArchive.Entries.ToDictionary(e => e.Key, e => e);

            Error404PageBytes = File.ReadAllBytes("./404.html");
        }

        public static void Load() { }

        public static SiteEntry? TryFetchEntry(string path)
        {
            if(SiteEntries.ContainsKey(path))
            {
                return SiteEntries[path];
            }
            return null;
        }

        static SemaphoreSlim archiveStreamLock = new SemaphoreSlim(1, 1);
        public static async Task<Stream?> FetchFileStream(string filename)
        {
            var key = "files/" + filename;
            if(FileContent.ContainsKey(key))
            {
                await archiveStreamLock.WaitAsync();
                return new CloseCallbackStream(FileContent[key].OpenEntryStream(), () => archiveStreamLock.Release());
                // return FileContent[key].OpenEntryStream();
            }
            return null;
        }
    }
}