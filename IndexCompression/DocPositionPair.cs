using System.Text;
namespace IndexCompression
{
    /// <summary>
    /// Represents a unique position within a document.
    /// </summary>
    public class DocPositionPair
    {
        /// <summary>
        /// Creates a new DocPosition instance.
        /// </summary>
        public DocPositionPair(uint identifier, uint position)
        {
            Identifier = identifier;
            Position = position;
        }

        /// <summary>
        /// The identifier of the document.
        /// </summary>
        public uint Identifier { get; set; }

        /// <summary>
        /// The position within the document.
        /// </summary>
        public uint Position { get; set; }

        /// <summary>
        /// Returns the index hash value for this instance.
        /// </summary>
        public uint ComputeHashValue()
        {
            var txtToHash = string.Format("doc_{0}_pos_{1}", Identifier, Position);
            return FNV32BitHash.ComputeHash(txtToHash);
        }
    }
}
