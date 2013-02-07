namespace IndexCompression
{
    /// <summary>
    /// Represents a specific value of a term "archetype".
    /// </summary>
    public class TermValuePair
    {
        /// <summary>
        /// Creates a new TermValuePair instance.
        /// </summary>
        public TermValuePair(string termName, string value)
        {
            TermName = termName;
            Value = value;
        }

        /// <summary>
        /// The 'archetype' of this term; a sort of higher-level
        /// categorization of terms.
        /// </summary>
        public string TermName { get; set; }

        /// <summary>
        /// The actual term value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Returns the index hash value for this instance.
        /// </summary>
        public uint ComputeHashValue()
        {
            var txtToHash = string.Format("term_{0}_value_{1}", TermName, Value);
            return FNV32BitHash.ComputeHash(txtToHash);
        }
    }
}
