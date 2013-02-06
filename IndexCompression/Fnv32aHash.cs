using System.Text;

namespace IndexCompression
{
    //References
    //http://www.isthe.com/chongo/tech/comp/fnv/
    //http://stackoverflow.com/questions/12272296/32-bit-fast-uniform-hash-function-use-md5-sha1-and-cut-off-4-bytes

    /// <summary>
    /// Class for computing 32bit Fowler-Noll-Vo hash values.
    /// </summary>
    public class FNV32BitHash
    {
        private const uint FnvPrime32 = 16777619;
        private const uint FnvOffset32 = 2166136261;

        /// <summary>
        /// Computes the 32bit hash for the provided string
        /// </summary>
        /// <param name="value">String value to hash</param>
        /// <returns>The 32bit hash of this string</returns>
        public static uint ComputeHash(string value)
        {
            byte[] bytesToHash = Encoding.UTF8.GetBytes(value);

            uint hash = FnvOffset32;
            unchecked
            {
                foreach (var chunk in bytesToHash)
                {
                    hash ^= chunk;
                    hash *= FnvPrime32;
                }
            }

            return hash;
        }
    }
}
