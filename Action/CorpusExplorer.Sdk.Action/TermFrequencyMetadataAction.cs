﻿using CorpusExplorer.Sdk.Addon;
using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Sdk.Utils.DataTableWriter.Abstract;
using CorpusExplorer.Sdk.ViewModel;

namespace CorpusExplorer.Sdk.Action
{
  public class TermFrequencyMetadataAction : IAction
  {
    public string Action => "tf-meta";
    public string Description => "tf-meta [META] {LAYER} - term frequency for [META] on {LAYER} (default: WORT)";

    public void Execute(Selection selection, string[] args, AbstractTableWriter writer)
    {
      var vm = new DocumentTermFrequencyMetadataViewModel { Selection = selection, MetadataKey = args[0] };
      if (args.Length == 2)
        vm.LayerDisplayname = args[1];
      vm.Execute();

      writer.WriteTable(selection.Displayname, vm.GetDataTable());
    }
  }
}