using System.Collections.Generic;
using CorpusExplorer.Port.RProgramming.Api.Action.Abstract;
using CorpusExplorer.Sdk.Blocks;
using CorpusExplorer.Sdk.Model;

namespace CorpusExplorer.Port.RProgramming.Api.Action
{
  public class MetaAction : AbstractAction
  {
    protected override HashSet<string> MatchActionLabels => new HashSet<string> {"meta"};

    public override void Execute(Selection selection, IEnumerable<string> args)
    {
      var block = selection.CreateBlock<DocumentMetadataWeightBlock>();
      block.Calculate();

      WriteOutput("category\tlabel\ttokens\ttypes\tdocuments\r\n");
      foreach (var x in block.GetAggregatedSize())
      foreach (var y in x.Value)
        WriteOutput($"{x.Key}\t{y.Key}\t{y.Value[0]}\t{y.Value[1]}\t{y.Value[2]}\r\n");
    }
  }
}