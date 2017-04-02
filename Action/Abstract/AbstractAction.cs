﻿using System;
using System.Collections.Generic;
using System.Text;
using CorpusExplorer.Sdk.Model;

namespace CorpusExplorer.Port.RProgramming.Api.Action.Abstract
{
  public abstract class AbstractAction
  {
    protected abstract HashSet<string> MatchActionLabels { get; }

    public abstract void Execute(Selection selection, IEnumerable<string> args);

    public bool Match(string actionLabel) { return MatchActionLabels.Contains(actionLabel.ToLower()); }

    protected void WriteOutput(string line)
    {
      var buffer = Encoding.UTF8.GetBytes(line.Replace("&#", "#"));
      Console.OpenStandardOutput().Write(buffer, 0, buffer.Length);
    }
  }
}