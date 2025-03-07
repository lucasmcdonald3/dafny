using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Boogie;
using Microsoft.Dafny.LanguageServer.Language;
using Microsoft.Dafny.LanguageServer.Language.Symbols;
using Microsoft.Dafny.LanguageServer.Workspace.Notifications;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.Dafny.LanguageServer.Workspace;


public record IdeImplementationView(Range Range, PublishedVerificationStatus Status,
  IReadOnlyList<Diagnostic> Diagnostics);

public record IdeVerificationResult(bool WasTranslated, IReadOnlyDictionary<string, IdeImplementationView> Implementations);

/// <summary>
/// Contains information from the latest document, and from older documents if some information is missing,
/// to provide the IDE with as much information as possible.
/// </summary>
public record IdeState(
  int Version,
  Compilation Compilation,
  Node Program,
  IReadOnlyDictionary<Uri, IReadOnlyList<Diagnostic>> ResolutionDiagnostics,
  SymbolTable SymbolTable,
  LegacySignatureAndCompletionTable SignatureAndCompletionTable,
  Dictionary<Location, IdeVerificationResult> VerificationResults,
  IReadOnlyList<Counterexample> Counterexamples,
  IReadOnlyDictionary<Uri, IReadOnlyList<Range>> GhostRanges,
  IReadOnlyDictionary<Uri, VerificationTree> VerificationTrees
) {

  public ImmutableDictionary<Uri, IReadOnlyList<Diagnostic>> GetDiagnostics() {
    var resolutionDiagnostics = ResolutionDiagnostics.ToImmutableDictionary();
    var verificationDiagnostics = VerificationResults.GroupBy(kv => kv.Key.Uri).Select(kv =>
      new KeyValuePair<Uri, IReadOnlyList<Diagnostic>>(kv.Key.ToUri(), kv.SelectMany(x => x.Value.Implementations.Values.SelectMany(v => v.Diagnostics)).ToList()));
    return resolutionDiagnostics.Merge(verificationDiagnostics, Lists.Concat);
  }
}

public static class Util {
  public static Diagnostic ToLspDiagnostic(this DafnyDiagnostic dafnyDiagnostic) {
    return new Diagnostic {
      Code = dafnyDiagnostic.ErrorId,
      Severity = ToSeverity(dafnyDiagnostic.Level),
      Message = dafnyDiagnostic.Message,
      Range = dafnyDiagnostic.Token.GetLspRange(),
      Source = dafnyDiagnostic.Source.ToString(),
      RelatedInformation = dafnyDiagnostic.RelatedInformation.Select(r =>
        new DiagnosticRelatedInformation {
          Location = CreateLocation(r.Token),
          Message = r.Message
        }).ToList(),
      CodeDescription = dafnyDiagnostic.ErrorId == null
        ? null
        : new CodeDescription { Href = new Uri("https://dafny.org/dafny/HowToFAQ/Errors#" + dafnyDiagnostic.ErrorId) },
    };
  }

  private static Location CreateLocation(IToken token) {
    var uri = DocumentUri.Parse(token.Uri.AbsoluteUri);
    return new Location {
      Range = token.GetLspRange(),
      // During parsing, we store absolute paths to make reconstructing the Uri easier
      // https://github.com/dafny-lang/dafny/blob/06b498ee73c74660c61042bb752207df13930376/Source/DafnyLanguageServer/Language/DafnyLangParser.cs#L59 
      Uri = uri
    };
  }

  private static DiagnosticSeverity ToSeverity(ErrorLevel level) {
    return level switch {
      ErrorLevel.Error => DiagnosticSeverity.Error,
      ErrorLevel.Warning => DiagnosticSeverity.Warning,
      ErrorLevel.Info => DiagnosticSeverity.Hint,
      _ => throw new ArgumentException($"unknown error level {level}", nameof(level))
    };
  }
}
