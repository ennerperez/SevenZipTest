using SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SevenZipTest
{
    class Program
    {

        static void Main(string[] args)
        {
            SevenZipCompressor.SetLibraryPath(Path.Combine(IntPtr.Size == 4 ? "x86" : "x64", "7z.dll"));
            var compressor = new SevenZipCompressor();
            var target = args[0];
            var chunkSize = 0;
            if (args.Count() == 3) int.TryParse(args[2], out chunkSize);

#if DEBUG
            if (!File.Exists(target))
#endif
                compressor.CompressFiles(target, args[1]);

            var sha1 = new List<string>();

            if (File.Exists(target))
                sha1.AddRange(SplitFile(target, int.Parse(args[2])));
            else
                sha1.Add(GetHash(File.ReadAllBytes(target)));

            File.WriteAllText(target + ".sha", string.Join(Environment.NewLine, sha1));

        }

        public static string GetHash(byte[] buffer)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                byte[] hash = sha1.ComputeHash(buffer);
                StringBuilder formatted = new StringBuilder(2 * hash.Length);
                foreach (byte b in hash)
                {
                    formatted.AppendFormat("{0:X2}", b);
                }

                return formatted.ToString();
            }
        }

        public static IEnumerable<string> SplitFile(string inputFile, int chunkSize)
        {

            var result = new List<string>();

            const int BUFFER_SIZE = 20 * 1024;
            byte[] buffer = new byte[BUFFER_SIZE];

            using (Stream input = File.OpenRead(inputFile))
            {
                var parts = (int)Math.Ceiling((decimal)input.Length / chunkSize);
                int index = 0;
                while (input.Position < input.Length)
                {
                    var filename = inputFile + String.Format(".{0:D" + (parts.ToString().Length < 3 ? 3 : parts.ToString().Length) + "}", (index + 1));
                    using (Stream output = File.Create(filename))
                    {
                        int remaining = chunkSize, bytesRead;
                        while (remaining > 0 && (bytesRead = input.Read(buffer, 0,
                                Math.Min(remaining, BUFFER_SIZE))) > 0)
                        {
                            output.Write(buffer, 0, bytesRead);

                            remaining -= bytesRead;
                        }
                    }
                    result.Add(GetHash(System.IO.File.ReadAllBytes(filename)));
                    index++;
                    Thread.Sleep(500); // experimental; perhaps try it
                }
            }

#if !DEBUG
            System.IO.File.Delete(inputFile);
#endif

            return result.AsEnumerable();

        }

    }
}
