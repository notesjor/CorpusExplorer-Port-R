using System.Linq;
using CorpusExplorer.Port.RProgramming.Api.Importer.Abstract;
using CorpusExplorer.Sdk.Extern.Xml.Weblicht;
using CorpusExplorer.Sdk.Model.Adapter.Corpus.Abstract;
using CorpusExplorer.Sdk.Utils.DocumentProcessing.Builder;

namespace CorpusExplorer.Port.RProgramming.Api.Importer
{
  public class ImporterWeblicht : AbstractImporter
  {
    public override bool Match(string path) { return path.ToLower().EndsWith(".xml") && !path.ToLower().EndsWith(".tcf.xml"); }

    public override AbstractCorpusAdapter Import(string path)
    {
      var importer = new WeblichtImporter {CorpusBuilder = new CorpusBuilderSingleFile()};
      return importer.Execute(new[] {path}).FirstOrDefault();
    }
  }
}