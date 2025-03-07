﻿using Microsoft.Dafny.LanguageServer.Language.Symbols;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Dafny.LanguageServer.Workspace;
using Microsoft.Dafny.LanguageServer.Workspace.ChangeProcessors;

namespace Microsoft.Dafny.LanguageServer.Language {
  /// <summary>
  /// Extension methods to register the dafny language services to the language server implementation.
  /// </summary>
  public static class LanguageServerExtensions {
    /// <summary>
    /// Registers all services necessary to manage the dafny language.
    /// </summary>
    /// <param name="options">The language server where the workspace services should be registered to.</param>
    /// <param name="configuration">The language server configuration.</param>
    /// <returns>The language server enriched with the dafny workspace services.</returns>
    public static LanguageServerOptions WithDafnyLanguage(this LanguageServerOptions options) {
      return options.WithServices(services => services.WithDafnyLanguage());
    }

    private static IServiceCollection WithDafnyLanguage(this IServiceCollection services) {
      return services
        .AddSingleton<IDafnyParser>(serviceProvider => new DafnyLangParser(
          serviceProvider.GetRequiredService<DafnyOptions>(),
          serviceProvider.GetRequiredService<IFileSystem>(),
          serviceProvider.GetRequiredService<ITelemetryPublisher>(),
          serviceProvider.GetRequiredService<ILogger<DafnyLangParser>>(),
          serviceProvider.GetRequiredService<ILogger<CachingParser>>()))
        .AddSingleton<ISymbolResolver, DafnyLangSymbolResolver>()
        .AddSingleton<CreateIdeStateObserver>(serviceProvider => compilation =>
          new IdeStateObserver(serviceProvider.GetRequiredService<ILogger<IdeStateObserver>>(),
            serviceProvider.GetRequiredService<ITelemetryPublisher>(),
            serviceProvider.GetRequiredService<INotificationPublisher>(),
            serviceProvider.GetRequiredService<ITextDocumentLoader>(),
            compilation))
        .AddSingleton<IVerificationProgressReporter, VerificationProgressReporter>()
        .AddSingleton(CreateVerifier)
        .AddSingleton<CreateCompilationManager>(serviceProvider => (options, engine, compilation, migratedVerificationTree) => new CompilationManager(
          serviceProvider.GetRequiredService<ILogger<CompilationManager>>(),
          serviceProvider.GetRequiredService<ITextDocumentLoader>(),
          serviceProvider.GetRequiredService<INotificationPublisher>(),
          serviceProvider.GetRequiredService<IProgramVerifier>(),
          serviceProvider.GetRequiredService<ICompilationStatusNotificationPublisher>(),
          serviceProvider.GetRequiredService<IVerificationProgressReporter>(),
          options, engine, compilation, migratedVerificationTree
          ))
        .AddSingleton<ISymbolTableFactory, SymbolTableFactory>()
        .AddSingleton<IGhostStateDiagnosticCollector, GhostStateDiagnosticCollector>();
    }

    private static IProgramVerifier CreateVerifier(IServiceProvider serviceProvider) {
      return new DafnyProgramVerifier(
        serviceProvider.GetRequiredService<ILogger<DafnyProgramVerifier>>()
      );
    }
  }
}
