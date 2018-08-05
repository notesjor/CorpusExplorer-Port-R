﻿using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Sdk.Properties;
using CorpusExplorer.Sdk.Utils.DataTableWriter.Abstract;
using CorpusExplorer.Sdk.ViewModel;
using CorpusExplorer.Terminal.Console.Action.Abstract;

namespace CorpusExplorer.Terminal.Console.Action
{
  public class Frequency1Action : AbstractAction
  {
    public override string Action => "frequency1";
    public override string Description => "frequency1 [LAYER1] - count token frequency on [LAYER]";

    public override void Execute(Selection selection, string[] args, AbstractTableWriter writer)
    {
      var vm = new Frequency1LayerViewModel { Selection = selection };
      if (args != null && args.Length == 1)
        vm.LayerDisplayname = args[0];
      vm.Execute();

      writer.WriteTable(selection.Displayname, vm.GetNormalizedDataTable());
    }
  }
}