﻿//------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン:4.0.30319.42000
//
//     このファイルへの変更は、以下の状況下で不正な動作の原因になったり、
//     コードが再生成されるときに損失したりします。
// </auto-generated>
//------------------------------------------------------------------------------

// 
// このソース コードは xsd によって自動生成されました。Version=4.8.3928.0 です。
// 
namespace BookViewerApp.Storages.ExtensionAdBlockerItems {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Exten" +
        "sionAdBlockerItems.xsd")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Exten" +
        "sionAdBlockerItems.xsd", IsNullable=false)]
    public partial class items {
        
        private itemsGroup[] groupField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("group")]
        public itemsGroup[] group {
            get {
                return this.groupField;
            }
            set {
                this.groupField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Exten" +
        "sionAdBlockerItems.xsd")]
    public partial class itemsGroup {
        
        private title[] titleField;
        
        private itemsGroupItem[] itemField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("title")]
        public title[] title {
            get {
                return this.titleField;
            }
            set {
                this.titleField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("item")]
        public itemsGroupItem[] item {
            get {
                return this.itemField;
            }
            set {
                this.itemField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Exten" +
        "sionAdBlockerItems.xsd")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Exten" +
        "sionAdBlockerItems.xsd", IsNullable=false)]
    public partial class title {
        
        private string languageField;
        
        private bool defaultField;
        
        private string valueField;
        
        public title() {
            this.languageField = "en";
            this.defaultField = false;
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType="language")]
        [System.ComponentModel.DefaultValueAttribute("en")]
        public string language {
            get {
                return this.languageField;
            }
            set {
                this.languageField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool @default {
            get {
                return this.defaultField;
            }
            set {
                this.defaultField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="https://github.com/kurema/BookViewerApp3/blob/master/BookViewerApp/Storages/Exten" +
        "sionAdBlockerItems.xsd")]
    public partial class itemsGroupItem {
        
        private title[] titleField;
        
        private string title1Field;
        
        private string filenameField;
        
        private string sourceField;
        
        private string project_sourceField;
        
        private string license_sourceField;
        
        private string license_summaryField;
        
        private string info_sourceField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("title")]
        public title[] title {
            get {
                return this.titleField;
            }
            set {
                this.titleField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("title")]
        public string title1 {
            get {
                return this.title1Field;
            }
            set {
                this.title1Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string filename {
            get {
                return this.filenameField;
            }
            set {
                this.filenameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType="anyURI")]
        public string source {
            get {
                return this.sourceField;
            }
            set {
                this.sourceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType="anyURI")]
        public string project_source {
            get {
                return this.project_sourceField;
            }
            set {
                this.project_sourceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType="anyURI")]
        public string license_source {
            get {
                return this.license_sourceField;
            }
            set {
                this.license_sourceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string license_summary {
            get {
                return this.license_summaryField;
            }
            set {
                this.license_summaryField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType="anyURI")]
        public string info_source {
            get {
                return this.info_sourceField;
            }
            set {
                this.info_sourceField = value;
            }
        }
    }
}