﻿using System.Collections.Generic;
using CorpusExplorer.Sdk.Action.Abstract;
using CorpusExplorer.Sdk.Action.Properties;
using CorpusExplorer.Sdk.Utils.Filter.Abstract;
using CorpusExplorer.Sdk.Utils.Filter.Queries;

namespace CorpusExplorer.Sdk.Action
{
  public class KwicFirstAnyFilterAction : AbstractFilterAction
  {
    public override string Action => "kwic-first-any";

    public override string Description => Resources.DescKwicFirstAny;

    protected override AbstractFilterQuery GetQuery(string layerDisplayname, IEnumerable<string> queries)
      => new FilterQuerySingleLayerFirstAndAnyOtherMatch
      {
        LayerDisplayname = layerDisplayname,
        LayerQueries = queries
      };
  }
}