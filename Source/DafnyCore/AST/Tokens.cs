using System.Diagnostics.Contracts;
using System.IO;
using Newtonsoft.Json;

namespace Microsoft.Dafny;

public static class TokenExtensions {


  public static DafnyRange ToDafnyRange(this IOrigin origin, bool includeTrailingWhitespace = false) {
    var startLine = origin.StartToken.line - 1;
    var startColumn = origin.StartToken.col - 1;
    var endLine = origin.EndToken.line - 1;
    int whitespaceOffset = 0;
    if (includeTrailingWhitespace) {
      string trivia = origin.EndToken.TrailingTrivia;
      // Don't want to remove newlines or comments -- just spaces and tabs
      while (whitespaceOffset < trivia.Length && (trivia[whitespaceOffset] == ' ' || trivia[whitespaceOffset] == '\t')) {
        whitespaceOffset++;
      }
    }

    var endColumn = origin.EndToken.col + (origin.InclusiveEnd ? origin.EndToken.val.Length : 0) + whitespaceOffset - 1;
    return new DafnyRange(
      new DafnyPosition(startLine, startColumn),
      new DafnyPosition(endLine, endColumn));
  }

  public static IOrigin MakeAutoGenerated(this IOrigin origin) {
    return new AutoGeneratedOrigin(origin);
  }

  public static IOrigin MakeRefined(this IOrigin origin, ModuleDefinition module) {
    return new RefinementOrigin(origin, module);
  }

  public static bool Contains(this IOrigin container, IOrigin otherToken) {
    return container.StartToken.Uri == otherToken.Uri &&
           container.StartToken.pos <= otherToken.pos &&
           (container.EndToken == null || otherToken.pos <= container.EndToken.pos);
  }

  public static bool Intersects(this IOrigin origin, IOrigin other) {
    return !(other.EndToken.pos + other.EndToken.val.Length <= origin.StartToken.pos
             || origin.EndToken.pos + origin.EndToken.val.Length <= other.StartToken.pos);
  }

  public static string PrintOriginal(this IOrigin origin) {
    return new SourceOrigin(origin.StartToken, origin.EndToken).PrintOriginal();
  }

  public static bool IsSet(this IOrigin token) => token.Uri != null;

  public static string TokenToString(this IOrigin tok, DafnyOptions options) {
    if (ReferenceEquals(tok, Token.Cli)) {
      return "CLI";
    }

    if (tok.Uri == null) {
      return $"({tok.line},{tok.col - 1})";
    }

    var currentDirectory = Directory.GetCurrentDirectory();
    string filename = tok.Uri.Scheme switch {
      "stdin" => "<stdin>",
      "transcript" => Path.GetFileName(tok.Filepath),
      _ => options.UseBaseNameForFileName
        ? Path.GetFileName(tok.Filepath)
        : (tok.Filepath.StartsWith(currentDirectory) ? Path.GetRelativePath(currentDirectory, tok.Filepath) : tok.Filepath)
    };

    return $"{filename}({tok.line},{tok.col - 1})";
  }
}

public class NestedOrigin : OriginWrapper {
  public NestedOrigin(IOrigin outer, IOrigin inner, string message = null)
    : base(outer) {
    Contract.Requires(outer != null);
    Contract.Requires(inner != null);
    Inner = inner;
    this.Message = message;
  }
  public IOrigin Outer { get { return WrappedToken; } }
  public readonly IOrigin Inner;
  public readonly string Message;
}

/// <summary>
/// A token wrapper used to produce better type checking errors
/// for quantified variables. See QuantifierVar.ExtractSingleRange()
/// </summary>
public class QuantifiedVariableDomainOrigin : OriginWrapper {
  public QuantifiedVariableDomainOrigin(IOrigin wrappedToken)
    : base(wrappedToken) {
    Contract.Requires(wrappedToken != null);
  }

  public override string val {
    get { return WrappedToken.val; }
    set { WrappedToken.val = value; }
  }
}

/// <summary>
/// A token wrapper used to produce better type checking errors
/// for quantified variables. See QuantifierVar.ExtractSingleRange()
/// </summary>
public class QuantifiedVariableRangeOrigin : OriginWrapper {
  public QuantifiedVariableRangeOrigin(IOrigin wrappedToken)
    : base(wrappedToken) {
    Contract.Requires(wrappedToken != null);
  }

  public override string val {
    get { return WrappedToken.val; }
    set { WrappedToken.val = value; }
  }
}
