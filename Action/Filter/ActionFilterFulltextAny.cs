using System;
using System.Collections.Generic;
using CorpusExplorer.Port.RProgramming.Api.Action.Filter.Abstract;
using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Sdk.Utils.Filter.Abstract;
using CorpusExplorer.Sdk.Utils.Filter.Queries;

namespace CorpusExplorer.Port.RProgramming.Api.Action.Filter
{
  public class ActionFilterFulltextAny : AbstractActionFilter
  {
    public override HashSet<string> Operator => new HashSet<string> {"any", "!any"};
    public override string Request => "text";

    public override Selection Execute(Selection selection, string @operator, string target, string query)
    {
      return selection.CreateTemporary(
        new AbstractFilterQuery[]
        {
          new FilterQuerySingleLayerAnyMatch
          {
            Inverse = @operator.StartsWith("!"),
            LayerDisplayname = target,
            LayerQueries = query.Split(new[] {"|", ",", ";"}, StringSplitOptions.RemoveEmptyEntries)
          }
        });
    }
  }
}