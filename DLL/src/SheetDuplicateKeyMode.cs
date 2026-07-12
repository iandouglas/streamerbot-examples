namespace iandouglas736
{
    /// <summary>
    /// Controls how rows with duplicate keys are handled when reading a Google Sheet.
    /// </summary>
    public enum SheetDuplicateKeyMode
    {
        /// <summary>
        /// The last row for a given key overwrites any previous rows. This is the default.
        /// </summary>
        LastEntryWins,

        /// <summary>
        /// Rows with the same key are collected into a list.
        /// </summary>
        BuildList
    }
}
