﻿using System.Collections.Generic;
using System.Linq;

namespace CorpusExplorer.Terminal.Console.Helper
{
  public static class AvailableAddonHelper
  {
    public static Dictionary<string, T> GetDictionary<T>(this IEnumerable<KeyValuePair<string, T>> dic)
    {
      var dictionary = new Dictionary<string, T>();
      foreach (var pair in dic)
      {
        var key = pair.Value.GetType().Name;
        if (!dictionary.ContainsKey(key))
          dictionary.Add(key, pair.Value);
      }
      return dictionary;
    }

    public static Dictionary<string, T> GetDictionary<T>(this IEnumerable<T> enumerable)
    {
      var dictionary = new Dictionary<string, T>();
      foreach (var unknown in enumerable)
      {
        var key = unknown.GetType().Name;
        if (!dictionary.ContainsKey(key))
          dictionary.Add(key, unknown);
      }
      return dictionary;
    }

    public static Dictionary<string, string> GetDictionaryForScriptEditor<T>(this IEnumerable<KeyValuePair<string, T>> dic)
    {
      var dictionary = new Dictionary<string, string>();
      foreach (var pair in dic)
      {
        var key = pair.Value.GetType().Name;
        if (!dictionary.ContainsKey(key))
          dictionary.Add(key, pair.Key);
      }
      return dictionary;
    }
  }
}