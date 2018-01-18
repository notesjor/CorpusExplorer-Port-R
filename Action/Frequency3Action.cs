using System.Collections.Generic;
using CorpusExplorer.Sdk.Blocks;
using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Terminal.Console.Action.Abstract;

namespace CorpusExplorer.Terminal.Console.Action
{
  public class Frequency3Action : AbstractAction
  {
    public override string Action => "frequency3";
    public override string Description => "frequency3 [LAYER1] [LAYER2] [LAYER3] - count token frequency on 3 layers";

    public override void Execute(Selection selection, string[] args)
    {
      var block = selection.CreateBlock<Frequency3LayerBlock>();
      if (args != null && args.Length == 3)
      {
        block.Layer1Displayname = args[0];
        block.Layer2Displayname = args[1];
        block.Layer3Displayname = args[2];
      }
      block.Calculate();

      WriteOutput("pos\tlemma\tterm\tfrequency\r\n");
      foreach (var x in block.Frequency)
        foreach (var y in x.Value)
          foreach (var z in y.Value)
            WriteOutput($"{x.Key}\t{y.Key}\t{z.Key}\t{z.Value}\r\n");
    }
  }
}