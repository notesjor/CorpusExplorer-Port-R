﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CorpusExplorer.Sdk.Action.Helper;
using CorpusExplorer.Sdk.Addon;
using CorpusExplorer.Sdk.Blocks;
using CorpusExplorer.Sdk.Ecosystem;
using CorpusExplorer.Sdk.Ecosystem.Model;
using CorpusExplorer.Sdk.Helper;
using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Sdk.Model.Cache;
using CorpusExplorer.Sdk.Model.Extension;
using CorpusExplorer.Sdk.Terminal;
using CorpusExplorer.Sdk.Utils.DataTableWriter.Abstract;
using CorpusExplorer.Sdk.Utils.DocumentProcessing.Cleanup;
using CorpusExplorer.Sdk.Utils.Filter;
using CorpusExplorer.Sdk.Utils.Filter.Queries;
using CorpusExplorer.Terminal.Console.Helper;
using CorpusExplorer.Terminal.Console.Properties;
using CorpusExplorer.Terminal.Console.Xml.Extensions;
using CorpusExplorer.Terminal.Console.Xml.Model;

namespace CorpusExplorer.Terminal.Console.Xml.Processor
{
  public static class XmlScriptProcessor
  {
    private static string _errorLog;

    private static readonly Dictionary<Guid, ExecuteActionItem> _executeActionList =
      new Dictionary<Guid, ExecuteActionItem>();

    private static readonly object _executeActionListLock = new object();

    private static bool _first = true;
    private static TerminalController _terminal;

    /// <summary>
    ///   Verarbeitete ein CEScript
    /// </summary>
    /// <param name="path">Pfad des CEScript</param>
    public static void Process(string path)
    {
      _terminal = CorpusExplorerEcosystem.Initialize(new CacheStrategyDisableCaching());
      _errorLog = path + ".log";

      string scriptFilename = null;
      cescript script = null;
      try
      {
        script = CeScriptHelper.LoadCeScript(path, out scriptFilename);
      }
      catch (Exception ex)
      {
        System.Console.WriteLine(Resources.XmlScriptParserError001);
        LogError(ex);
        System.Console.ReadLine();
        throw ex;
      }

      ConsoleHelper.PrintHeader();

      Parallel.ForEach(script.sessions.session,
                       !string.IsNullOrEmpty(script.sessions.mode) && script.sessions.mode.StartsWith("sync")
                         ? new ParallelOptions { MaxDegreeOfParallelism = 1 } // no prallel processing
                         : Configuration.ParallelOptions,
                       session =>
                       {
                         try
                         {
                           ExecuteSession(session, scriptFilename);
                         }
                         catch (Exception ex)
                         {
                           LogError(ex);
                         }
                       });

      ConsoleHelper.PrintHeader();
      System.Console.WriteLine(Resources.XmlScriptSuccess);
    }

    private static void ExecuteSession(session session, string scriptFilename)
    {
      try
      {
        switch (session.sources.processing)
        {
          case "loop":
            {
              foreach (var source in session.sources.Items)
              {
                switch (source)
                {
                  case annotate a:
                    using (var project = ReadSources(new[] { a }))
                      ExecuteSession(session, scriptFilename, project);
                    break;
                  case import i:
                    foreach (var item in i.Items)
                      switch (item)
                      {
                        case file _:
                          using (var project = ReadSources(new[] { i }))
                            ExecuteSession(session, scriptFilename, project);
                          break;
                        case directory d:
                          var files = Directory.GetFiles(d.Value, d.filter);
                          foreach (var file in files)
                            using (var project = ReadSources(new[]
                            {
                              new import
                              {
                                Items = new[]
                                {
                                  new file
                                  {
                                    delete = d.delete,
                                    Value = file
                                  }
                                },
                                type = i.type
                              }
                            }))
                            {
                              ExecuteSession(session, scriptFilename, project);
                            }

                          break;
                      }
                    break;
                }
              }

              break;
            }
          case "parallel-loop":
            {
              foreach (var source in session.sources.Items)
              {
                switch (source)
                {
                  case annotate a:
                    using (var project = ReadSources(new[] { a }))
                      ExecuteSession(session, scriptFilename, project);
                    break;
                  case import i:
                    foreach (var item in i.Items)
                      switch (item)
                      {
                        case file _:
                          using (var project = ReadSources(new[] { i }))
                            ExecuteSession(session, scriptFilename, project);
                          break;
                        case directory d:
                          var files = Directory.GetFiles(d.Value, d.filter);
                          Parallel.ForEach(files, Configuration.ParallelOptions, file =>
                          {
                            using (var project = ReadSources(new[]
                            {
                              new import
                              {
                                Items = new[]
                                {
                                  new file
                                  {
                                    delete = d.delete,
                                    Value = file
                                  }
                                },
                                type = i.type
                              }
                            }))
                            {
                              ExecuteSession(session, scriptFilename, project);
                            }
                            GC.Collect();
                          });

                          break;
                      }
                    break;
                }
              }

              break;
            }
          case "sub-dir-loop":
            var sdlSource = session.sources.Items.FirstOrDefault();
            switch (sdlSource)
            {
              case annotate a:
                var baseDirA = a.Items.FirstOrDefault() as directory;
                var subDirsA = Directory.GetDirectories(baseDirA.Value);
                foreach (var subDir in subDirsA)
                {
                  overrideCorpusName = subDir.Replace(Path.GetDirectoryName(subDir), "").Replace("/", "").Replace("\\", "");
                  using (var project = ReadSources(new[]
                  {
                    new annotate
                    {
                      type = a.type,
                      tagger = a.tagger,
                      language = a.language,
                      Items = Directory.GetFiles(subDir, baseDirA.filter)
                                       .Select(file => new file
                                        {
                                          delete = baseDirA.delete,
                                          Value = file
                                        }).ToArray()
                    }
                  }))
                    ExecuteSession(session, scriptFilename, project);
                }
                break;
              case import i:
                var baseDirI = i.Items.FirstOrDefault() as directory;
                var subDirsI = Directory.GetDirectories(baseDirI.Value);
                foreach (var subDir in subDirsI)
                {
                  overrideCorpusName = subDir.Replace(Path.GetDirectoryName(subDir), "").Replace("/", "").Replace("\\", "");
                  using (var project = ReadSources(new[]
                  {
                    new import
                    {
                      type = i.type,
                      Items = Directory.GetFiles(subDir, baseDirI.filter)
                                       .Select(file => new file
                                        {
                                          delete = baseDirI.delete,
                                          Value = file
                                        }).ToArray()
                    }
                  }))
                    ExecuteSession(session, scriptFilename, project);
                }
                break;
            }

            break;
          case "parallel-sub-dir-loop":
            var psdlSource = session.sources.Items.FirstOrDefault();
            switch (psdlSource)
            {
              case annotate a:
                var baseDirA = a.Items.FirstOrDefault() as directory;
                var subDirsA = Directory.GetDirectories(baseDirA.Value);
                Parallel.ForEach(subDirsA, Configuration.ParallelOptions, subDir =>
                {
                  overrideCorpusName = subDir.Replace(Path.GetDirectoryName(subDir), "").Replace("/", "")
                                             .Replace("\\", "");
                  using (var project = ReadSources(new[]
                  {
                    new annotate
                    {
                      type = a.type,
                      tagger = a.tagger,
                      language = a.language,
                      Items = Directory.GetFiles(subDir, baseDirA.filter)
                                       .Select(file => new file
                                        {
                                          delete = baseDirA.delete,
                                          Value = file
                                        }).ToArray()
                    }
                  }))
                  {
                    ExecuteSession(session, scriptFilename, project);
                  }

                  GC.Collect();
                });
                break;
              case import i:
                var baseDirI = i.Items.FirstOrDefault() as directory;
                var subDirsI = Directory.GetDirectories(baseDirI.Value);
                Parallel.ForEach(subDirsI, Configuration.ParallelOptions, subDir =>
                {
                  overrideCorpusName = subDir.Replace(Path.GetDirectoryName(subDir), "").Replace("/", "")
                                             .Replace("\\", "");
                  using (var project = ReadSources(new[]
                  {
                    new import
                    {
                      type = i.type,
                      Items = Directory.GetFiles(subDir, baseDirI.filter)
                                       .Select(file => new file
                                        {
                                          delete = baseDirI.delete,
                                          Value = file
                                        }).ToArray()
                    }
                  }))
                  {
                    ExecuteSession(session, scriptFilename, project);
                  }

                  GC.Collect();
                });
                break;
            }

            break;
          default:
            {
              using (var project = ReadSources(session.sources.Items))
                ExecuteSession(session, scriptFilename, project);
              break;
            }
        }
      }
      catch
      {
        // ignore
      }
    }

    private static void ExecuteSession(session session, string scriptFilename, Project project)
    {
      try
      {
        var selections = GenerateSelections(project, session.queries);
        var allowOverride = session.@override;

        Parallel.ForEach(session.actions.action,
                         !string.IsNullOrEmpty(session.actions.mode) && session.actions.mode.StartsWith("sync")
                           ? new ParallelOptions { MaxDegreeOfParallelism = 1 } // no prallel processing
                           : Configuration.ParallelOptions,
                         action => { ExecuteSessionAction(scriptFilename, action, selections, allowOverride); });
      }
      catch (Exception ex)
      {
        LogError(ex);
      }
    }

    private static void ExecuteSessionAction(string scriptFilename, action a, Dictionary<string, Selection[]> selections, bool allowOverride)
    {
      try
      {
        var action = Configuration.GetConsoleAction(a.type);
        if (action == null)
          return;

        var query = a.query ?? string.Empty;
        var actionSelections = new List<Selection>();
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (query == "*") // Alle Queries
        {
          actionSelections.AddRange(selections.SelectMany(x => x.Value));
        }
        else if (query == "+") // Alle Queries außer SELECTALL
        {
          var first = selections.First().Key;
          actionSelections.AddRange(selections.Where(x => x.Key != first).SelectMany(x => x.Value));
        }
        else if (query.StartsWith("*")) // Alle Queries die auf query enden
        {
          var q = query.Substring(1);
          foreach (var x in selections)
            if (x.Key.EndsWith(q))
              actionSelections.AddRange(x.Value);
        }
        else if (query.EndsWith("*")) // Alle Queries die auf query beginnen
        {
          var q = query.Substring(0, query.Length - 1);
          foreach (var x in selections)
            if (x.Key.StartsWith(q))
              actionSelections.AddRange(x.Value);
        }
        else if (!selections.ContainsKey(query)) // Wenn kein Query verfügbar breche ab
        {
          return;
        }
        else // Einzelquery
        {
          actionSelections.AddRange(selections[query]);
        }

        try
        {
          ExecuteAction(action, a, actionSelections, query, scriptFilename, allowOverride);
        }
        catch (Exception ex)
        {
          LogError(ex);
        }
      }
      catch (Exception ex)
      {
        LogError(ex);
      }
    }

    /// <summary>
    ///   Führe eine Action aus.
    /// </summary>
    /// <param name="action">Action - siehe CorpusExplorer.Terminal.Console.Action</param>
    /// <param name="a">Action (beinhaltet Information zum Ausführen und Speichern der Resulate)</param>
    /// <param name="selections">Schnappschüsse</param>
    /// <param name="query">Query-Pattern, das zur auswahl der Schnappschüsse dient</param>
    /// <param name="scriptFilename">Name des CeScripts</param>
    /// <param name="allowOverride">Erlaubt das Überschreiben von exsistierenden Ausgabedateien</param>
    private static void ExecuteAction(IAction action, action a,
                                      List<Selection> selections, string query, string scriptFilename,
                                      bool allowOverride)
    {
      var taskGuid = Guid.NewGuid();
      try
      {
        if (!string.IsNullOrEmpty(a.mode) && a.mode == "merge")
        {
          var outputPath = OutputPathBuilder(a.output.Value, scriptFilename, CorpusNameBuilder(selections), query, a.type);

          // Wurde eine Action bereits abgeschlossen? - Falls ja, breche ab.
          if (!allowOverride && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
            return;

          // Reporting für Konsole
          ExecuteActionReport(taskGuid, query, a.type, outputPath, false);

          switch (a.type)
          {
            // Ist die Action vom Typ query, dann konvertiere Abfrage
            case "query":
              {
                var exporter = Configuration.AddonExporters.GetReflectedType(a.output.format, "Exporter");
                if (exporter == null)
                  return;

                exporter.Export(selections.JoinFull(Path.GetFileNameWithoutExtension(outputPath)).CreateTemporary(new[] { QueryParser.Parse(a.query) }).ToCorpus(), outputPath);
                break;
              }
            // Ist die Action vom Typ convert, dann konvertiere direkt
            case "convert":
              {
                var exporter = Configuration.AddonExporters.GetReflectedType(a.output.format, "Exporter");
                if (exporter == null)
                  return;

                exporter.Export(selections.JoinFull(Path.GetFileNameWithoutExtension(outputPath)).ToCorpus(), outputPath);
                break;
              }
            case "cluster" when a.arguments[1] == "convert":
              {
                var exporter = Configuration.AddonExporters.GetReflectedType(a.output.format, "Exporter");
                if (exporter == null)
                  return;

                var qp = QueryParser.Parse(a.arguments[0]);
                if (!(qp is FilterQueryUnsupportedParserFeature))
                  return;

                var sel = UnsupportedQueryParserFeatureHelper.Handle(selections.JoinFull(Path.GetFileNameWithoutExtension(outputPath)), (FilterQueryUnsupportedParserFeature)qp);
                if (sel == null)
                  return;

                foreach (var s in sel)
                  exporter.Export(s.ToCorpus(), OutputPathBuilder(a.output.Value, scriptFilename, CorpusNameBuilder(selections), s.Displayname, a.type));
                break;
              }
            // Andernfalls ist format vom Typ AbstractTableWriter
            default:
              {
                var formatKey = a.output.format.StartsWith("F:") || a.output.format.StartsWith("FNT:") ? a.output.format : $"F:{a.output.format}";
                var format = Configuration.GetTableWriter(formatKey);
                if (format == null)
                  return;

                // Kopie des TableWriter, um eine parallele Verarbeitung zu ermöglichen.
                using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                using (var bs = new BufferedStream(fs))
                {
                  format = format.Clone(bs);
                  Parallel.ForEach(selections, Configuration.ParallelOptions,
                                   // ReSharper disable once AccessToDisposedClosure
                                   // ReSharper disable once ImplicitlyCapturedClosure
                                   selection => action.Execute(selection, a.arguments, format));
                  format.Destroy();
                }

                break;
              }
          }

          // Reporting für Konsole
          ExecuteActionReport(taskGuid, query, a.type, outputPath, true);
        }
        else
        {
          Parallel.ForEach(selections, Configuration.ParallelOptions, selection =>
          {
            var outputPath = OutputPathBuilder(a.output.Value, scriptFilename, CorpusNameBuilder(selections), selection.Displayname, a.type);

            // Wurde die Action bereits abgeschlossen? - Falls ja, breche ab.
            if (!allowOverride && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0)
              return;

            // Reporting für Konsole
            ExecuteActionReport(taskGuid, selection.Displayname, a.type, outputPath, false);

            switch (a.type)
            {
              // Ist die Action vom Typ query, dann konvertiere Abfrage
              case "query":
                {
                  var exporter = Configuration.AddonExporters.GetReflectedType(a.output.format, "Exporter");
                  if (exporter == null)
                    return;

                  exporter.Export(selection.CreateTemporary(new[] { QueryParser.Parse(a.query) }).ToCorpus(), outputPath);
                  break;
                }
              // Ist die Action vom Typ convert, dann konvertiere direkt
              case "convert":
                {
                  var exporter = Configuration.AddonExporters.GetReflectedType(a.output.format, "Exporter");
                  if (exporter == null)
                    return;

                  exporter.Export(selection.ToCorpus(), outputPath);
                  break;
                }
              // Ist die Action vom Typ cluster UND ist die cluster-Action vom Typ convert, dann muss format vom Typ AbstractExporter sein
              case "cluster" when a.arguments[1] == "convert":
                {
                  var exporter = Configuration.AddonExporters.GetReflectedType(a.output.format, "Exporter");
                  if (exporter == null)
                    return;

                  var qp = QueryParser.Parse(a.arguments[0]);
                  if (!(qp is FilterQueryUnsupportedParserFeature))
                    return;

                  var sel = UnsupportedQueryParserFeatureHelper.Handle(selection, (FilterQueryUnsupportedParserFeature)qp);
                  if (sel == null)
                    return;

                  foreach (var s in sel)
                    exporter.Export(s.ToCorpus(), OutputPathBuilder(a.output.Value, scriptFilename, CorpusNameBuilder(selections), s.Displayname, a.type));
                  break;
                }
              // Andernfalls ist format vom Typ AbstractTableWriter
              default:
                {
                  var formatKey = a.output.format.StartsWith("F:") || a.output.format.StartsWith("FNT:") ? a.output.format : $"F:{a.output.format}";
                  var format = Configuration.GetTableWriter(formatKey);
                  if (format == null)
                    return;

                  // Kopie des TableWriter, um eine parallele Verarbeitung zu ermöglichen.
                  using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                  using (var bs = new BufferedStream(fs))
                  {
                    format = format.Clone(bs);
                    action.Execute(selection, a.arguments, format);
                    format.Destroy();
                  }

                  break;
                }
            }

            // Reporting für Konsole
            ExecuteActionReport(taskGuid, selection.Displayname, a.type, outputPath, true);
          });
        }
      }
      catch (Exception ex)
      {
        LogError(ex, $"{query} - {action.Action} - {a.type}");
      }
    }

    private static string CorpusNameBuilder(List<Selection> selections)
    {
      var hash = new HashSet<string>(selections.SelectMany(selection => selection.CorporaDisplaynames));
      return hash.Count == 0 ? "NONE" : string.Join(";", hash);
    }

    /// <summary>
    ///   Erzeugt eine Ausgabe auf der Konsole - Damit die Nutzer*in einen Überblick behält was aktuell passiert.
    /// </summary>
    /// <param name="taskGuid">Eindeutiger GUID des Tasls</param>
    /// <param name="selectionDisplayname">Schnappschussname</param>
    /// <param name="actionType">Typ der Aufgabe</param>
    /// <param name="outputPath">Pfad der Ausgabedatei</param>
    /// <param name="done">Ist die Aufgabe erledigt?</param>
    private static void ExecuteActionReport(Guid taskGuid, string selectionDisplayname, string actionType,
                                            string outputPath, bool done)
    {
      try
      {
        var display =
          $"{(string.IsNullOrWhiteSpace(selectionDisplayname) ? "ALL" : selectionDisplayname)} > {actionType} > {Path.GetFileName(outputPath)}";
        lock (_executeActionListLock)
        {
          // Entferne bereits erledigte Aufgaben
          var keys = _executeActionList.Keys.ToArray();
          foreach (var k in keys)
            if (_executeActionList[k].Done)
              _executeActionList.Remove(k);

          // Status aktualisieren
          if (_executeActionList.ContainsKey(taskGuid))
            _executeActionList[taskGuid].Done = done;
          else
            _executeActionList.Add(taskGuid, new ExecuteActionItem { DisplayName = display, Done = false });

          if (done || _first)
          {
            _first = false;

            // Liste ausgeben
            System.Console.ForegroundColor = ConsoleColor.Gray;
            ConsoleHelper.PrintHeader();
            System.Console.WriteLine(Resources.XmlScriptCurrentActions);
            foreach (var t in _executeActionList)
            {
              System.Console.ForegroundColor = t.Value.Done ? ConsoleColor.Green : ConsoleColor.Yellow;
              System.Console.WriteLine(t.Value.DisplayName + " ... " + (t.Value.Done ? Resources.Done : Resources.Running));
            }
          }
          else
          {
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(display + " ... " + Resources.Running);
          }

          System.Console.ForegroundColor = ConsoleColor.Gray;
        }
      }
      catch (Exception ex)
      {
        LogError(ex);
      }
    }

    /// <summary>
    ///   Erzeugt Abfragen
    /// </summary>
    /// <param name="source">Projekt</param>
    /// <param name="queries">Abfragen</param>
    /// <returns>Auflistung mit allen Abfragen</returns>
    private static Dictionary<string, Selection[]> GenerateSelections(Project source, queries queries)
    {
      var all = source.SelectAll;
      if (all == null)
        return new Dictionary<string, Selection[]>();

      all.Displayname = "ALL";
      var res = new Dictionary<string, Selection[]> { { "", new[] { all } } };

      if (queries?.Items == null)
        return res;

      foreach (var item in queries.Items)
        try
        {
          switch (item)
          {
            case query q:
              if (string.IsNullOrWhiteSpace(q.parent))
                GenerateSelections_SingleQuery(q, ref res, source.SelectAll);
              else
                foreach (var selection in res[q.parent])
                  GenerateSelections_SingleQuery(q, ref res, selection, true);
              break;
            case queryBuilder b:
              if (string.IsNullOrWhiteSpace(b.parent))
                GenerateSelections_QueryBuilder(b, ref res, source.SelectAll);
              else
                foreach (var selection in res[b.parent])
                  GenerateSelections_QueryBuilder(b, ref res, selection, true);
              break;
            case queryGroup g:
              if (string.IsNullOrWhiteSpace(g.parent))
                GenerateSelections_QueryGroup(g, ref res, source.SelectAll);
              else
                foreach (var selection in res[g.parent])
                  GenerateSelections_QueryGroup(g, ref res, selection, true);
              break;
          }
        }
        catch (Exception ex)
        {
          LogError(ex);
        }

      return res;
    }

    /// <summary>
    ///   Nutzt den QueryParser des CE, um Abfragen zu bauen
    /// </summary>
    /// <param name="selection">Schnappschuss: Alle Dokumente</param>
    /// <param name="query">Abfrage</param>
    /// <param name="key">Name der Abfrage</param>
    /// <returns>Neue Schnappschüsse</returns>
    private static Selection[] GenerateSelections_Compile(Selection selection, string query, string key)
    {
      try
      {
        var filterQuery = QueryParser.Parse(query.CleanXmlValue());
        if (!(filterQuery is FilterQueryUnsupportedParserFeature))
          return new[] { selection.Create(new[] { filterQuery }, key, false) };

        var q = (FilterQueryUnsupportedParserFeature)filterQuery;
        switch (q.MetaLabel)
        {
          case "<:RANDOM:>":
            return GenerateSelections_RandomSplit(selection, q.MetaValues);
          case "<:CORPUS:>":
            return GenerateSelections_CorporaSplit(selection);
          default:
            return GenerateSelections_MetaSplit(selection, q, q.MetaValues);
        }
      }
      catch (Exception ex)
      {
        LogError(ex);
      }

      return null;
    }

    /// <summary>
    ///   Ermöglicht es auf alle Korpora zuzugreifen
    /// </summary>
    /// <param name="selection">Schnappschuss</param>
    /// <returns>Neue Schnappschüsse</returns>
    private static Selection[] GenerateSelections_CorporaSplit(Selection selection)
    {
      return
        (from csel in selection.CorporaGuids
         let corpus = selection.GetCorpus(csel)
         let dsels = new HashSet<Guid>(corpus.DocumentGuids)
         select selection.Create(new Dictionary<Guid, HashSet<Guid>> { { csel, dsels } }, corpus.CorpusDisplayname, false))
       .ToArray();
    }

    /// <summary>
    ///   Ermöglicht eine Metasplit-Abfrage (wird vom QueryParser nicht unterstützt).
    /// </summary>
    /// <param name="selection">Schnappschuss</param>
    /// <param name="q">Abfrage</param>
    /// <param name="values">Parameter</param>
    /// <returns>Neue Schnappschüsse</returns>
    private static Selection[] GenerateSelections_MetaSplit(Selection selection, FilterQueryUnsupportedParserFeature q,
                                                            IEnumerable<object> values)
    {
      var vs = values?.ToArray();
      return vs?.Length != 1 ? null : AutoSplitBlockHelper.RunAutoSplit(selection, q, vs).ToArray();
    }

    /// <summary>
    ///   Baut aus vorgegebenen Werten mehrere Einzelabfragen
    /// </summary>
    /// <param name="queryBuilder">QueryBuilder</param>
    /// <param name="res">Rückgabeliste</param>
    /// <param name="all">Schnappschuss: Alle Dokumente</param>
    /// <param name="useSelectionDisplaynameAsPrefix">Stellt dem Namen des aktuell erzeugten Query den ParentDisplayname voran.</param>
    private static void GenerateSelections_QueryBuilder(queryBuilder queryBuilder,
                                                        ref Dictionary<string, Selection[]> res, Selection all,
                                                        bool useSelectionDisplaynameAsPrefix = false)
    {
      var key = queryBuilder.name ?? string.Empty;
      if (key == "*" || key == "+")
        return;
      if (useSelectionDisplaynameAsPrefix)
        key = $"{all.Displayname}_{key}";
      foreach (var v in queryBuilder.value)
      {
        var gname = (key + v).Replace("\"", "");
        if (res.ContainsKey(gname))
          continue;
        var gquery = queryBuilder.prefix + v;

        res.Add(gname, GenerateSelections_Compile(all, gquery, gname));
      }
    }

    /// <summary>
    ///   Eine QueryGroup verknüpft mehrere Abfragen miteinander
    /// </summary>
    /// <param name="queryGroup">QueryGroup</param>
    /// <param name="res">Rückgabeliste</param>
    /// <param name="all">Schnappschuss: Alle Dokumente</param>
    /// <param name="useSelectionDisplaynameAsPrefix">Stellt dem Namen des aktuell erzeugten Query den ParentDisplayname voran.</param>
    private static void GenerateSelections_QueryGroup(queryGroup queryGroup, ref Dictionary<string, Selection[]> res,
                                                      Selection all, bool useSelectionDisplaynameAsPrefix = false)
    {
      var key = queryGroup.name ?? string.Empty;
      if (key == "" || key == "*" || key == "+")
        return;
      if (useSelectionDisplaynameAsPrefix)
        key = $"{all.Displayname}_{key}";
      if (res.ContainsKey(key))
        return;

      var prefix = string.IsNullOrEmpty(queryGroup.prefix) ? string.Empty : queryGroup.prefix;

      // Erzeuge erste Abfrage
      var qs = new List<query>(queryGroup.query);
      var selection = GenerateSelections_Compile(all, $"{prefix}{qs[0].Text.CleanXmlValue()}", qs[0].name).First()
                                                                                                          .CorporaAndDocumentGuids
                                                                                                          .ToDictionary(x => x.Key,
                                                                                                                        x =>
                                                                                                                          new
                                                                                                                            HashSet
                                                                                                                            <Guid
                                                                                                                            >(x
                                                                                                                               .Value));
      qs.RemoveAt(0); // Entferne erste Abfrage aus der Liste

      // Führe alle Folgeabfragen aus.
      foreach (var query in qs)
      {
        var temp = GenerateSelections_Compile(all, $"{prefix}{query.Text.CleanXmlValue()}", "").First()
                                                                                               .CorporaAndDocumentGuids
                                                                                               .ToDictionary(x => x.Key,
                                                                                                             x =>
                                                                                                               new
                                                                                                                 HashSet
                                                                                                                 <Guid
                                                                                                                 >(x
                                                                                                                    .Value));
        switch (queryGroup.@operator)
        {
          default:
          // ReSharper disable once RedundantCaseLabel
          case "and": // Ergebnisse müssen mit allen Abfragen übereinstimmen
            var csels = selection.Keys.ToArray();
            foreach (var csel in csels)
            {
              if (!temp.ContainsKey(csel))
              {
                selection.Remove(csel);
                continue;
              }

              var dsels = selection[csel];
              foreach (var dsel in dsels)
                if (!temp.ContainsKey(dsel))
                  selection[csel].Remove(dsel);
            }

            break;
          case "or": // Ergebnis trifft auf die erste oder eine Folgeabfrage zu
            foreach (var csel in temp)
            {
              if (!selection.ContainsKey(csel.Key))
                selection.Add(csel.Key, new HashSet<Guid>());
              foreach (var dsel in csel.Value)
                if (!selection[csel.Key].Contains(dsel))
                  selection[csel.Key].Add(dsel);
            }

            break;
        }
      }

      res.Add(key, new[] { all.Create(selection, key, false) });
    }

    /// <summary>
    ///   Ermöglicht es einen zufälligen Schnappschuss zu erstellen
    /// </summary>
    /// <param name="selection">Schnappschuss</param>
    /// <param name="values">Parameter</param>
    /// <returns>Neue Schnappschüsse</returns>
    private static Selection[] GenerateSelections_RandomSplit(Selection selection, IEnumerable<object> values)
    {
      var block = selection.CreateBlock<RandomSelectionBlock>();
      block.DocumentCount = int.Parse(values.First().ToString());
      block.Calculate();
      return new[] { block.RandomSelection, block.RandomInvertSelection };
    }

    /// <summary>
    ///   Erzeugt eine Einzelabfrage
    /// </summary>
    /// <param name="query">Einzelabfrage</param>
    /// <param name="res">Rückgabeliste</param>
    /// <param name="all">Schnappschuss: Alle Dokumente</param>
    /// <param name="useSelectionDisplaynameAsPrefix">Stellt dem Namen des aktuell erzeugten Query den ParentDisplayname voran.</param>
    private static void GenerateSelections_SingleQuery(query query, ref Dictionary<string, Selection[]> res,
                                                       Selection all, bool useSelectionDisplaynameAsPrefix = false)
    {
      var key = query.name ?? string.Empty;
      if (key == "" || key == "*" || key == "+")
        return;
      if (useSelectionDisplaynameAsPrefix)
        key = $"{all.Displayname}_{key}";
      if (res.ContainsKey(key))
        return;
      res.Add(key, GenerateSelections_Compile(all, query.Text.CleanXmlValue(), query.name));
    }

    private static void LogError(Exception ex, string additionalLine = null)
    {
      try
      {
        System.Console.WriteLine(ex.Message);
        System.Console.WriteLine(ex.StackTrace);
        if (ex.InnerException != null)
        {
          System.Console.WriteLine(ex.InnerException.Message);
          System.Console.WriteLine(ex.InnerException.StackTrace);
        }

        File.AppendAllLines(_errorLog,
                            additionalLine == null
                              ? new[] { ex.Message, ex.StackTrace, "---" }
                              : new[] { additionalLine, ex.Message, ex.StackTrace, "---" });
      }
      catch
      {
        // ignore
      }
    }

    private static string overrideCorpusName = null;

    /// <summary>
    ///   Erzeugt einen Ausgabepfad
    /// </summary>
    /// <param name="path">Pfad</param>
    /// <param name="scriptFilename">Name des CeScripts</param>
    /// <param name="corpusName">Name des Korpus (kann durch overrideCorpusName überschrieben werden)</param>
    /// <param name="selectionName">Schnappschussname</param>
    /// <param name="action">Action</param>
    /// <returns>Ausgabepfad</returns>
    private static string OutputPathBuilder(string path, string scriptFilename, string corpusName, string selectionName, string action)
    {
      corpusName = overrideCorpusName ?? corpusName;
      var res = path.Replace("{all}", "{script}_{corpus}_{selection}_{action}")
                    .Replace("{script}", scriptFilename)
                    .Replace("{corpus}", corpusName)
                    .Replace("{selection}", selectionName == "*" ? "ALL" : selectionName)
                    .Replace("{action}", action)
                    .EnsureFileName();
      var dir = Path.GetDirectoryName(res);
      if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        Directory.CreateDirectory(dir);

      return res;
    }

    private static object _newProjectLock = new object();

    /// <summary>
    ///   Liest die gewünschten Korpusquellen ein
    /// </summary>
    /// <param name="sources">Quellen</param>
    /// <returns>Project</returns>
    private static Project ReadSources(object[] sources)
    {
      Project proj;
      lock (_newProjectLock)
        proj = _terminal.ProjectNew(false);

      foreach (var source in sources)
        switch (source)
        {
          case annotate annotate:
            try
            {
              var scraper = Configuration.AddonScrapers.GetReflectedType(annotate.type, "Scraper");
              var tagger = Configuration.AddonTaggers.GetReflectedType(annotate.tagger, "Tagger");

              if (scraper == null || tagger == null)
                continue;

              // Extrahiere und bereinige die Dokumente
              scraper.Input.Enqueue(SearchFiles(annotate.Items));
              scraper.Execute();
              var cleaner1 = new StandardCleanup { Input = scraper.Output };
              cleaner1.Execute();
              var cleaner2 = new RegexXmlMarkupCleanup { Input = cleaner1.Output };
              cleaner2.Execute();

              // Annotiere das Textmaterial
              tagger.LanguageSelected = annotate.language;
              tagger.Input = cleaner2.Output;
              tagger.Execute();

              foreach (var corpus in tagger.Output)
                proj.Add(corpus);
            }
            catch (Exception ex)
            {
              LogError(ex);
            }
            break;
          case import import:
            try
            {
              var importer = Configuration.AddonImporters.GetReflectedType(import.type, "Importer");
              if (importer == null)
                continue;

              foreach (var corpus in importer.Execute(SearchFiles(import.Items)))
                proj.Add(corpus);
            }
            catch (Exception ex)
            {
              LogError(ex);
            }
            break;
        }

      return proj;
    }

    /// <summary>
    ///   Es gibt zwei mögliche Arten Quellen zu spezifizieren - als Datei oder komplette Order
    /// </summary>
    /// <param name="annotateItems">Quellen</param>
    /// <returns>Dateien</returns>
    private static IEnumerable<string> SearchFiles(object[] annotateItems)
    {
      var res = new List<string>();

      foreach (var item in annotateItems)
        try
        {
          switch (item)
          {
            case file i:
              res.Add(i.Value);
              break;
            case directory i:
              var files = Directory.GetFiles(i.Value, ValidateSearchFilter(i.filter), SearchOption.TopDirectoryOnly);
              res.AddRange(files);
              break;
          }
        }
        catch (Exception ex)
        {
          LogError(ex);
        }

      return res;
    }

    private static string ValidateSearchFilter(string iFilter)
    {
      return iFilter.StartsWith("*") ? iFilter : iFilter.StartsWith(".") ? $"*{iFilter}" : $"*.{iFilter}";
    }

    private class ExecuteActionItem
    {
      public string DisplayName { get; set; }
      public bool Done { get; set; }
    }
  }
}