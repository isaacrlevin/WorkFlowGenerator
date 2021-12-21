using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WorkFlowGenerator.Tests;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TestClass]
public class Utility
{
    public static string TrimNewLines(string input)
    {
        //Trim off any leading or trailing new lines 
        input = input.TrimStart('\r', '\n');
        input = input.TrimEnd('\r', '\n');

        return input;
    }

}
