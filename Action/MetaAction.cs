using System.Collections.Generic;
using CorpusExplorer.Sdk.Blocks;
using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Sdk.ViewModel;
using CorpusExplorer.Terminal.Console.Action.Abstract;

namespace CorpusExplorer.Terminal.Console.Action
{
  public class MetaAction : AbstractAction
  {
    public override string Action => "meta";
    public override string Description => "meta - lists all meta-categories, labels and token/type/document-count";

    public override void Execute(Selection selection, string[] args)
    {
      var vm = new CorpusWeightUnlimmitedViewModel { Selection = selection };
      vm.Analyse();
      WriteTable(vm.GetDataTable());
    }
  }
}