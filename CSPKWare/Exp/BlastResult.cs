namespace CSPKWare.Exp
{
    public enum BlastResult
    {
        BLAST_TRUNCATED_INPUT = 2, // ran out of input before completing decompression
        BLAST_OUTPUT_ERROR = 1, // output error before completing decompression
        BLAST_SUCCESS = 0, // successful decompression
        BLAST_INVALID_LITERAL_FLAG = -1, // literal flag not zero or one
        BLAST_INVALID_DIC_SIZE = -2, // dictionary size not in 4..6
        BLAST_INVALID_OFFSET = -3, // distance is too far back
    }
}
