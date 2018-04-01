﻿using System.Collections.Generic;
using System.Data;
using CorpusExplorer.Sdk.Model;
using CorpusExplorer.Terminal.Console.Action.Abstract;

namespace CorpusExplorer.Terminal.Console.Action
{
  public class LayerNamesAction : AbstractAction
  {
    public override string Action => "layer-names";
    public override string Description => "layer-names - all available names for [LAYER]";

    public override void Execute(Selection selection, string[] args)
    {
      var dt = new DataTable();
      dt.Columns.Add("layernames", typeof(string));

      dt.BeginLoadData();
      foreach (var x in selection.LayerDisplaynames)
      dt.Rows.Add(x);
      dt.EndLoadData();

      WriteTable(dt);
    }
  }
}