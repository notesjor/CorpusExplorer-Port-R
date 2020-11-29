﻿using System.Collections.Generic;
using System.Linq;
using CorpusExplorer.Terminal.Console.Xml.Model;

namespace CorpusExplorer.Terminal.Console.Xml.Extensions
{
  public static class QueriesExtension
  {
    public static IEnumerable<query> query(this queries obj)
    {
      return obj.Items.OfType<query>();
    }

    public static IEnumerable<queryGroup> queryGroup(this queries obj)
    {
      return obj.Items.OfType<queryGroup>();
    }

    public static IEnumerable<queryBuilder> queryBuilder(this queries obj)
    {
      return obj.Items.OfType<queryBuilder>();
    }

    public static string[] GetNames(this queries obj)
    {
      var res = new List<string>();
      var q0 = obj.query();
      if (q0 != null) res.AddRange(q0.Select(q => q.name));
      var q1 = obj.queryGroup();
      if (q1 != null) res.AddRange(q1.Select(q => q.name));
      var q2 = obj.queryBuilder();
      if (q2 != null) res.AddRange(q2.Select(q => q.name));
      return res.ToArray();
    }
  }
}