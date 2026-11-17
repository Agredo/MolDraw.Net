using System.Text;
using MolDraw.Depiction;
using NCDK;
using NCDK.IO;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;

namespace MolDraw.Core.Ncdk;

/// <summary>NCDK based <see cref="IMoleculeLoader"/>. Stateless and thread-safe (NCDK parsers are created per call).</summary>
public sealed class MoleculeLoader : IMoleculeLoader
{
    private readonly MoleculeLoaderOptions _options;

    /// <summary>Creates a loader.</summary>
    /// <param name="options">Options; <see langword="null"/> uses <see cref="MoleculeLoaderOptions.Default"/>.</param>
    public MoleculeLoader(MoleculeLoaderOptions? options = null)
    {
        _options = options ?? MoleculeLoaderOptions.Default;
    }

    /// <summary>The options of this loader.</summary>
    public MoleculeLoaderOptions Options => _options;

    /// <inheritdoc />
    public IAtomContainer ParseSmiles(string smiles)
    {
        ArgumentNullException.ThrowIfNull(smiles);
        var text = smiles.Trim();
        var builder = NCDK.Silent.ChemObjectBuilder.Instance;
        if (text.Length == 0)
            return builder.NewAtomContainer();

        IAtomContainer molecule;
        try
        {
            var parser = new SmilesParser(builder, kekulise: true, strict: _options.StrictSmiles);
            molecule = parser.ParseSmiles(text);
        }
        catch (Exception ex) when (ex is InvalidSmilesException or CDKException or ArgumentException or InvalidOperationException or IndexOutOfRangeException or NullReferenceException)
        {
            throw CreateSmilesException(text, ex);
        }

        return PostProcess(molecule);
    }

    /// <inheritdoc />
    public bool TryParseSmiles(string? smiles, out IAtomContainer? molecule, out string? error)
    {
        if (smiles is null)
        {
            molecule = null;
            error = "SMILES is null.";
            return false;
        }

        try
        {
            molecule = ParseSmiles(smiles);
            error = null;
            return true;
        }
        catch (MoleculeParseException ex)
        {
            molecule = null;
            error = ex.Message;
            return false;
        }
    }

    /// <inheritdoc />
    public IAtomContainer Load(Stream stream, MoleculeFormat format)
    {
        var all = LoadCore(stream, format, firstOnly: true);
        return all.Count > 0 ? all[0] : throw new MoleculeParseException($"The {format} input contains no molecule.");
    }

    /// <inheritdoc />
    public IReadOnlyList<IAtomContainer> LoadAll(Stream stream, MoleculeFormat format) => LoadCore(stream, format, firstOnly: false);

    private List<IAtomContainer> LoadCore(Stream stream, MoleculeFormat format, bool firstOnly)
    {
        ArgumentNullException.ThrowIfNull(stream);
        string content;
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: true))
            content = reader.ReadToEnd();

        var result = new List<IAtomContainer>();
        switch (format)
        {
            case MoleculeFormat.Smiles:
                foreach (var rawLine in content.Split('\n'))
                {
                    var line = rawLine.Trim();
                    if (line.Length == 0 || line.StartsWith('#'))
                        continue;
                    var ws = line.IndexOfAny([' ', '\t']);
                    result.Add(ParseSmiles(ws < 0 ? line : line[..ws]));
                    if (firstOnly)
                        break;
                }

                break;

            case MoleculeFormat.MolFile:
                result.Add(ReadMolBlock(content, recordNumber: null));
                break;

            case MoleculeFormat.Sdf:
                var record = 0;
                foreach (var block in SplitSdf(content))
                {
                    record++;
                    result.Add(ReadMolBlock(block, record));
                    if (firstOnly)
                        break;
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported molecule format.");
        }

        return result;
    }

    private IAtomContainer ReadMolBlock(string block, int? recordNumber)
    {
        var where = recordNumber is null ? "molfile" : $"SD record {recordNumber}";
        if (string.IsNullOrWhiteSpace(block))
            throw new MoleculeParseException($"The {where} is empty.");

        var builder = NCDK.Silent.ChemObjectBuilder.Instance;
        IAtomContainer molecule;
        try
        {
            using var text = new StringReader(block);
            if (block.Contains("V3000", StringComparison.Ordinal))
            {
                using var reader = new MDLV3000Reader(text, ChemObjectReaderMode.Strict);
                molecule = reader.Read(builder.NewAtomContainer());
            }
            else
            {
                using var reader = new MDLV2000Reader(text, ChemObjectReaderMode.Relaxed);
                molecule = reader.Read(builder.NewAtomContainer());
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new MoleculeParseException($"Could not parse {where}: {ex.Message}", Excerpt(block), details: ex.Message, innerException: ex);
        }

        if (molecule is null || molecule.Atoms.Count == 0)
            throw new MoleculeParseException($"The {where} contains no atoms.", Excerpt(block));
        return PostProcess(molecule);
    }

    private IAtomContainer PostProcess(IAtomContainer molecule)
    {
        if (!_options.KeepExplicitHydrogens && molecule.Atoms.Any(a => a.AtomicNumber == 1))
            molecule = AtomContainerManipulator.SuppressHydrogens(molecule);
        return molecule;
    }

    private static IEnumerable<string> SplitSdf(string content)
    {
        var sb = new StringBuilder();
        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith("$$$$", StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(sb.ToString()))
                    yield return sb.ToString();
                sb.Clear();
                continue;
            }

            sb.Append(line).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(sb.ToString()))
            yield return sb.ToString();
    }

    private static string Excerpt(string block) => block.Length <= 200 ? block : block[..200] + "…";

    /// <summary>
    /// NCDK messages look like <c>could not parse 'CC(', Unclosed branch detected, SMILES may be truncated:\nCC(\n  ^</c>.
    /// Extracts details and the caret position.
    /// </summary>
    internal static MoleculeParseException CreateSmilesException(string smiles, Exception ex)
    {
        var raw = ex.Message ?? string.Empty;
        var lines = raw.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n');
        int? position = null;
        if (lines.Length >= 3)
        {
            var caret = lines[^1].IndexOf('^', StringComparison.Ordinal);
            if (caret >= 0 && lines[^1].Trim() == "^")
                position = caret;
        }

        var details = lines[0];
        var prefix = $"could not parse '{smiles}', ";
        if (details.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            details = details[prefix.Length..];
        details = details.TrimEnd(':').Trim();
        if (details.Length == 0)
            details = ex.GetType().Name;
        details = char.ToUpperInvariant(details[0]) + details[1..];

        var message = position is { } p
            ? $"Invalid SMILES '{smiles}' at position {p + 1}: {details}"
            : $"Invalid SMILES '{smiles}': {details}";
        return new MoleculeParseException(message, smiles, position, details, ex);
    }
}
