using System;
using System.Collections.Generic;
using CorpusExplorer.Terminal.Console.Action.Filter.Abstract;

namespace CorpusExplorer.Terminal.Console.Action.Filter
{
  public class ActionFilterFulltextInSentence : AbstractActionFilter
  {
    public override HashSet<string> Operator => new HashSet<string> {"insentence", "!insentence", "insen", "!insen"};
    public override string Request => "text";

    public override Selection Execute(Selection selection, string @operator, string target, string query)
    {
      return selection.CreateTemporary(
        new AbstractFilterQuery[]
        {
          new FilterQuerySingleLayerAllInOneSentence
          {
            Inverse = @operator.StartsWith("!"),
            LayerDisplayname = target,
            LayerQueries = query.Split(new[] {"#"}, StringSplitOptions.RemoveEmptyEntries)
          }
        });
    }
  }
}