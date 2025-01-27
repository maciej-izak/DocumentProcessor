using System;

namespace DocumentProcessorApi.Exceptions
{
    /// <summary>
    /// Custom exception thrown when a parsing error occurs, including line number, character position, and column index.
    /// </summary>
    public class ParseException : Exception
    {
        /// <summary>
        /// The line number where the error occurred.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The exact character position in the line where the error occurred.
        /// </summary>
        public int CharPosition { get; }

        /// <summary>
        /// The column index in the CSV row where the error occurred.
        /// </summary>
        public int ColumnIndex { get; }

        /// <summary>
        /// Constructor for ParseException.
        /// </summary>
        public ParseException(string message, int lineNumber, int charPosition, int columnIndex = -1)
            : base(message)
        {
            LineNumber = lineNumber;
            CharPosition = charPosition;
            ColumnIndex = columnIndex;
        }

        /// <summary>
        /// Constructor with inner exception.
        /// </summary>
        public ParseException(string message, int lineNumber, int charPosition, int columnIndex, Exception innerException)
            : base(message, innerException)
        {
            LineNumber = lineNumber;
            CharPosition = charPosition;
            ColumnIndex = columnIndex;
        }
    }
}