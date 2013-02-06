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
        public TermValuePair(string archetype, string value)
        {
            Archetype = archetype;
            Value = value;
        }

        /// <summary>
        /// The 'archetype' of this term; a sort of higher-level
        /// categorization of terms.
        /// </summary>
        public string Archetype { get; set; }

        /// <summary>
        /// The actual term value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Returns the index hash value for this instance.
        /// </summary>
        public uint GetIndexHashValue()
        {
            var txtToHash = string.Format("arch_{0}_value_{1}", Archetype, Value);
            return FNV32BitHash.ComputeHash(txtToHash);
        }
    }
}
