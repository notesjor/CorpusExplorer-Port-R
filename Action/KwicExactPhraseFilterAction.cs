﻿using System.Collections.Generic;
using CorpusExplorer.Sdk.Utils.Filter.Abstract;
using CorpusExplorer.Sdk.Utils.Filter.Queries;
using CorpusExplorer.Terminal.Console.Action.Abstract;

namespace CorpusExplorer.Terminal.Console.Action
{
  public class KwicExactPhraseFilterAction : AbstractFilterAction
  {
    public override string Action => "kwic-phrase";

    public override string Description =>
      "kwic-phrase [LAYER] [WORDS] - [WORDS] = space separated tokens - all token in one sentence + given order";

    protected override AbstractFilterQuery GetQuery(string layerDisplayname, IEnumerable<string> queries)
    {
      return new FilterQuerySingleLayerExactPhrase { LayerDisplayname = layerDisplayname, LayerQueries = queries };
    }
  }
}