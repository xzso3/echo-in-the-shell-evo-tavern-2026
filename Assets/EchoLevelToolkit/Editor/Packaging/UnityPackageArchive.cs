using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Echo.LevelToolkit.Editor.Packaging
{
    // Reads the Unity 2021 .unitypackage gzip/tar envelope without importing any asset.
    internal static class UnityPackageArchive
    {
        internal sealed class Entry
        {
            internal string path;
            internal string sha256;
            internal byte[] smallContent;
        }

        internal sealed class Asset
        {
            internal string guid;
            internal string path;
            internal Entry content;
            internal Entry meta;
            internal bool isFolder;
        }

        internal static Dictionary<string, Asset> Read(string packagePath)
        {
            var assets = new Dictionary<string, Asset>(StringComparer.OrdinalIgnoreCase);
            using (var file = File.OpenRead(packagePath))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                var header = new byte[512];
                long count = 0;
                while (true)
                {
                    ReadExactly(gzip, header, 512);
                    if (AllZero(header)) break;
                    if (++count > 200000) throw new InvalidDataException("Too many tar entries.");
                    VerifyChecksum(header);
                    string name = Ascii(header, 0, 100);
                    string prefix = Ascii(header, 345, 155);
                    if (prefix.Length != 0) name = prefix + "/" + name;
                    if (name.StartsWith("./", StringComparison.Ordinal)) name = name.Substring(2);
                    long length = ReadOctal(header, 124, 12);
                    byte type = header[156];
                    if (type == (byte)'5') { Skip(gzip, length); SkipPadding(gzip, length); continue; }
                    if (type != 0 && type != (byte)'0') throw new InvalidDataException("Unsupported tar entry type: " + name);
                    string[] segments = name.Split('/');
                    if (segments.Length != 2 || !IsGuid(segments[0])
                        || (segments[1] != "pathname" && segments[1] != "asset"
                            && segments[1] != "asset.meta" && segments[1] != "preview.png"))
                        throw new InvalidDataException("Unexpected unitypackage entry: " + name);
                    Asset asset;
                    if (!assets.TryGetValue(segments[0], out asset))
                        assets.Add(segments[0], asset = new Asset { guid = segments[0].ToLowerInvariant() });
                    if (segments[1] == "preview.png") { Skip(gzip, length); SkipPadding(gzip, length); continue; }
                    bool keep = segments[1] == "pathname" || segments[1] == "asset.meta"
                        || (segments[1] == "asset" && length <= 2 * 1024 * 1024);
                    var entry = ReadEntry(gzip, length, keep);
                    entry.path = name;
                    SkipPadding(gzip, length);
                    if (segments[1] == "pathname")
                    {
                        if (asset.path != null) throw new InvalidDataException("Duplicate pathname: " + name);
                        if (entry.smallContent == null || entry.smallContent.Length > 4096)
                            throw new InvalidDataException("Invalid pathname: " + name);
                        asset.path = Encoding.UTF8.GetString(entry.smallContent).TrimEnd('\0', '\r', '\n');
                    }
                    else if (segments[1] == "asset.meta")
                    {
                        if (asset.meta != null) throw new InvalidDataException("Duplicate asset.meta: " + name);
                        asset.meta = entry;
                    }
                    else
                    {
                        if (asset.content != null) throw new InvalidDataException("Duplicate asset: " + name);
                        asset.content = entry;
                    }
                }
            }
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Asset asset in assets.Values)
            {
                if (asset.path == null || asset.meta == null
                    || !PackagePolicy.IsSafeAssetPath(asset.path) || !paths.Add(asset.path))
                    throw new InvalidDataException("Missing or duplicate asset parts: " + asset.guid);
                string meta = Encoding.UTF8.GetString(asset.meta.smallContent ?? Array.Empty<byte>());
                Match match = Regex.Match(meta, @"(?m)^guid: ([0-9a-fA-F]{32})\r?$");
                if (!match.Success || !string.Equals(match.Groups[1].Value, asset.guid, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Archive folder GUID does not match .meta GUID: " + asset.path);
                asset.isFolder = asset.content == null && meta.Contains("folderAsset: yes");
                if (asset.content == null && !asset.isFolder) throw new InvalidDataException("Missing asset content: " + asset.path);
            }
            return assets;
        }

        private static Entry ReadEntry(Stream stream, long length, bool keep)
        {
            if (length < 0 || length > 4L * 1024 * 1024 * 1024) throw new InvalidDataException("Entry too large.");
            if (keep && length > 2 * 1024 * 1024) throw new InvalidDataException("Metadata too large.");
            var entry = new Entry();
            using (var sha = SHA256.Create())
            using (var output = keep ? new MemoryStream((int)length) : null)
            {
                var buffer = new byte[65536];
                long remaining = length;
                while (remaining > 0)
                {
                    int take = (int)Math.Min(buffer.Length, remaining);
                    int n = stream.Read(buffer, 0, take);
                    if (n <= 0) throw new EndOfStreamException("Truncated unitypackage.");
                    sha.TransformBlock(buffer, 0, n, buffer, 0);
                    if (keep) output.Write(buffer, 0, n);
                    remaining -= n;
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                entry.sha256 = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
                if (keep) entry.smallContent = output.ToArray();
            }
            return entry;
        }

        private static void VerifyChecksum(byte[] header)
        {
            long expected = ReadOctal(header, 148, 8);
            long actual = 0;
            for (int i = 0; i < 512; i++) actual += i >= 148 && i < 156 ? 32 : header[i];
            if (expected != actual) throw new InvalidDataException("Invalid tar header checksum.");
        }

        private static bool IsGuid(string value) => Regex.IsMatch(value, "^[0-9a-fA-F]{32}$");
        private static bool AllZero(byte[] bytes) { foreach (byte b in bytes) if (b != 0) return false; return true; }
        private static string Ascii(byte[] bytes, int offset, int count) => Encoding.ASCII.GetString(bytes, offset, count).TrimEnd('\0', ' ');

        private static long ReadOctal(byte[] bytes, int offset, int count)
        {
            string value = Ascii(bytes, offset, count).Trim();
            if (value.Length == 0) return 0;
            long number = 0;
            foreach (char c in value)
            {
                if (c < '0' || c > '7') throw new InvalidDataException("Invalid tar size/checksum.");
                checked { number = number * 8 + c - '0'; }
            }
            return number;
        }

        private static void SkipPadding(Stream stream, long length) => Skip(stream, (512 - length % 512) % 512);
        private static void Skip(Stream stream, long length)
        {
            var buffer = new byte[8192];
            while (length > 0)
            {
                int n = stream.Read(buffer, 0, (int)Math.Min(length, buffer.Length));
                if (n <= 0) throw new EndOfStreamException("Truncated unitypackage.");
                length -= n;
            }
        }
        private static void ReadExactly(Stream stream, byte[] bytes, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int n = stream.Read(bytes, offset, count - offset);
                if (n <= 0) throw new EndOfStreamException("Truncated unitypackage.");
                offset += n;
            }
        }
    }
}
