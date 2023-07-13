using Common.Diagnostic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.IO;

namespace PlayerGeneration
{
    public static class Helpers
    {

        public static string MakeRelativePath(string path)
        {
            try
            {
                Uri file = new Uri(path);
                // Must end in a slash to indicate folder
                Uri folder = new Uri(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
                return string.Format(".{0}{1}",
                        Path.DirectorySeparatorChar,
                        Uri.UnescapeDataString(folder.MakeRelativeUri(file)
                                                .ToString()
                                                .Replace('/', Path.DirectorySeparatorChar)));
            }
            catch { }
            return path;
        }
             
        public static double GetRandomNumber(double minimum, double maximum, Random random = null)
        {
            if(random == null)
                random = new Random(Guid.NewGuid().GetHashCode());

            return random.NextDouble() * (maximum - minimum) + minimum;
        }

        public static int GetHashCodeFlds(Guid guid)
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + Thread.CurrentThread.ManagedThreadId.GetHashCode();
                hash = hash * 23 + guid.GetHashCode();           
                return hash;
            }
        }

        public static long GetLongHash(int highOrderValue)
        {
            var guidHashCode = GetHashCodeFlds(Guid.NewGuid());

            return (long)highOrderValue << 32 | (long)(uint)guidHashCode;          
        }
    }
}
