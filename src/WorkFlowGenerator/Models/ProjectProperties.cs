using System;
using System.Collections.Generic;
using System.Text;

namespace WorkFlowGenerator.Models;

// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class Project
{

    private ProjectPropertyGroup propertyGroupField;

    private string sdkField;

    /// <remarks/>
    public ProjectPropertyGroup PropertyGroup
    {
        get
        {
            return this.propertyGroupField;
        }
        set
        {
            this.propertyGroupField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string Sdk
    {
        get
        {
            return this.sdkField;
        }
        set
        {
            this.sdkField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class ProjectPropertyGroup
{

    private string targetFrameworkField;

    private string azureFunctionsVersionField;

    /// <remarks/>
    public string TargetFramework
    {
        get
        {
            return this.targetFrameworkField;
        }
        set
        {
            this.targetFrameworkField = value;
        }
    }

    /// <remarks/>
    public string AzureFunctionsVersion
    {
        get
        {
            return this.azureFunctionsVersionField;
        }
        set
        {
            this.azureFunctionsVersionField = value;
        }
    }
}
