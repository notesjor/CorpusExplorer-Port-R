﻿using System.Collections.Generic;
using CorpusExplorer.Sdk.Action.Helper;
using CorpusExplorer.Sdk.Action.Properties;
using CorpusExplorer.Sdk.Addon;
using CorpusExplorer.Sdk.Ecosystem.Model;
using CorpusExplorer.Sdk.Helper;
using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Sdk.Utils.DataTableWriter.Abstract;
using CorpusExplorer.Sdk.Utils.Filter;
using CorpusExplorer.Sdk.Utils.Filter.Queries;

namespace CorpusExplorer.Sdk.Action
{
  public class ClusterAction : IAction
  {
    public string Action => "cluster";

    public string Description => Resources.DescCluster;

    public void Execute(Selection selection, string[] args, AbstractTableWriter writer)
    {
      if (args.Length < 2 || args[1].ToLower() == "cluster")
        return;

      var query = QueryParser.Parse(args[0]);
      if (!(query is FilterQueryUnsupportedParserFeature))
        return;

      var selections =
        UnsupportedQueryParserFeatureHelper.Handle(selection, (FilterQueryUnsupportedParserFeature) query);
      if (selections == null)
        return;

      var nargs = new List<string>(args);
      nargs.RemoveAt(0);
      var task = nargs[0];
      var action = Configuration.GetConsoleAction(task);
      if (action == null)
        return;

      nargs.RemoveAt(0);
      foreach (var s in selections) action.Execute(s, nargs.ToArray(), writer);
    }
  }
}