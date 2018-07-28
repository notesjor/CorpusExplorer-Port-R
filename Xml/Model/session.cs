﻿namespace CorpusExplorer.Terminal.Console.Xml.Model
{
  /// <remarks/>
  [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
  [System.SerializableAttribute()]
  [System.Diagnostics.DebuggerStepThroughAttribute()]
  [System.ComponentModel.DesignerCategoryAttribute("code")]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
  public partial class session
  {

    private sources sourcesField;

    private queries queriesField;

    private tasks tasksField;

    private bool overrideField;

    private bool overrideFieldSpecified;

    /// <remarks/>
    public sources sources
    {
      get { return this.sourcesField; }
      set { this.sourcesField = value; }
    }

    /// <remarks/>
    public queries queries
    {
      get { return this.queriesField; }
      set { this.queriesField = value; }
    }

    /// <remarks/>
    public tasks tasks
    {
      get { return this.tasksField; }
      set { this.tasksField = value; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public bool @override
    {
      get { return this.overrideField; }
      set { this.overrideField = value; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool overrideSpecified
    {
      get { return this.overrideFieldSpecified; }
      set { this.overrideFieldSpecified = value; }
    }
  }
}