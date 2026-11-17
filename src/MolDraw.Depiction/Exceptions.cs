namespace MolDraw.Depiction;

/// <summary>Thrown when a SMILES string or molecule file cannot be parsed.</summary>
public sealed class MoleculeParseException : Exception
{
    /// <summary>Creates a new exception.</summary>
    /// <param name="message">Human readable message (includes the details).</param>
    /// <param name="input">The offending input (SMILES string or a file excerpt), if available.</param>
    /// <param name="position">Zero-based character position of the error within <paramref name="input"/>, if known.</param>
    /// <param name="details">Short description of the problem without the input echo, e.g. <c>Unclosed branch detected</c>.</param>
    /// <param name="innerException">Underlying toolkit exception.</param>
    public MoleculeParseException(string message, string? input = null, int? position = null, string? details = null, Exception? innerException = null)
        : base(message, innerException)
    {
        Input = input;
        Position = position;
        Details = details ?? message;
    }

    /// <summary>The offending input, if available.</summary>
    public string? Input { get; }

    /// <summary>Zero-based character position of the error, if known.</summary>
    public int? Position { get; }

    /// <summary>Short description of the problem.</summary>
    public string Details { get; }
}

/// <summary>Thrown when a depiction cannot be created (e.g. 2D layout failure).</summary>
public sealed class DepictionException : Exception
{
    /// <summary>Creates a new exception.</summary>
    /// <param name="message">Message.</param>
    /// <param name="innerException">Underlying toolkit exception.</param>
    public DepictionException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
