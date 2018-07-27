using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Sdk.Utils.DataTableWriter.Abstract;
using CorpusExplorer.Sdk.ViewModel;
using CorpusExplorer.Terminal.Console.Action.Abstract;

namespace CorpusExplorer.Terminal.Console.Action
{
  public class CooccurrenceAction : AbstractAction
  {
    public override string Action => "cooccurrence";
    public override string Description => "cooccurrence [LAYER1] - significant cooccurrences for all [LAYER] values";

    public override void Execute(Selection selection, string[] args, AbstractTableWriter writer)
    {
      var vm = new CooccurrenceViewModel {Selection = selection};
      if (args != null && args.Length == 1)
        vm.LayerDisplayname = args[0];
      vm.Execute();

      writer.WriteTable(selection.Displayname, vm.GetFullDataTable());
    }
  }
}