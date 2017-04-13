namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests.TestFiles
{
    #line hidden
    using System;
    using System.Threading.Tasks;
    public class TestFiles_IntegrationTests_CodeGenerationIntegrationTest_CSharp7_DesignTime
    {
        #pragma warning disable 219
        private void __RazorDirectiveTokenHelpers__() {
        }
        #pragma warning restore 219
        private static System.Object __o = null;
        #pragma warning disable 1998
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
#line 2 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/CSharp7.cshtml"
      
        var nameLookup = new Dictionary<string, (string FirstName, string LastName, object Extra)>()
        {
            ["John Doe"] = ("John", "Doe", true)
        };

        

#line default
#line hidden
#line 8 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/CSharp7.cshtml"
                                                     

        int Sixteen = 0b0001_0000;
        long BillionsAndBillions = 100_000_000_000;
        double AvogadroConstant = 6.022_140_857_747_474e23;
        decimal GoldenRatio = 1.618_033_988_749_894_848_204_586_834_365_638_117_720_309_179M;
    

#line default
#line hidden
#line 16 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/CSharp7.cshtml"
     if (nameLookup.TryGetValue("John Doe", out var entry))
    {
        if (entry.Extra is bool alive)
        {
            // Do Something
        }
    }

#line default
#line hidden
#line 24 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/CSharp7.cshtml"
                                 __o = 1.618_033_988_749_894_848_204_586_834_365_638_117_720_309_179M;

#line default
#line hidden
#line 28 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/CSharp7.cshtml"
    __o = (First: "John", Last: "Doe").First;

#line default
#line hidden
#line 31 "TestFiles/IntegrationTests/CodeGenerationIntegrationTest/CSharp7.cshtml"
     switch (entry.Extra)
    {
        case int age:
            // Do something
            break;
        case IEnumerable<string> childrenNames:
            // Do more something
            break;
        case null:
            // Do even more of something
            break;
    }

#line default
#line hidden
        }
        #pragma warning restore 1998
    }
}
