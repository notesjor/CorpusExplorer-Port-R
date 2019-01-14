﻿using System;
using Newtonsoft.Json;

namespace CorpusExplorer.Terminal.Console.Web.Model.Request.WebServiceDirect
{
  public class ConvertRequest
  {
    [JsonProperty("corpusId")]
    public string CorpusId { get; set; }

    [JsonProperty("documentIds")]
    public Guid[] DocumentIds { get; set; }

    [JsonProperty("outputFormat")]
    public string OutputFormat { get; set; }
  }
}