﻿/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
public partial class binary {
    
  private string nameField;
    
  private string typeField;
    
  private string[] textField;
    
  /// <remarks/>
  [System.Xml.Serialization.XmlAttributeAttribute(DataType="NCName")]
  public string name {
    get {
      return this.nameField;
    }
    set {
      this.nameField = value;
    }
  }
    
  /// <remarks/>
  [System.Xml.Serialization.XmlAttributeAttribute(DataType="NCName")]
  public string type {
    get {
      return this.typeField;
    }
    set {
      this.typeField = value;
    }
  }
    
  /// <remarks/>
  [System.Xml.Serialization.XmlTextAttribute()]
  public string[] Text {
    get {
      return this.textField;
    }
    set {
      this.textField = value;
    }
  }
}